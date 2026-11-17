using Xunit;
using Core;
using Core.Services;
using Core.Storage;

namespace Core.Tests;

public class ImportTests
{
    private const string GoodJson = """
        [ { "id": "T-1", "customerId": "CU-01", "status": "Confirmed",
            "lines": [ { "productId": "V-1", "productName": "Car", "price": 100, "quantity": 1 } ] } ]
        """;

    private const string BadJson = """
        [ { "id": "T-1", "customerId": "CU-01", "status": "Draft",
            "lines": [ { "productId": "V-1", "productName": "Car", "price": 100, "quantity": 1 } ] },
          { "id": "T-2", "customerId": "CU-01", "status": "Confirmed", "lines": [] } ]
        """;

    private static async Task<string> WithTempFile(string content, Func<string, Task> body)
    {
        string path = Path.Combine(Path.GetTempPath(), $"orders-{Guid.NewGuid():N}.json");
        await File.WriteAllTextAsync(path, content);
        try { await body(path); }
        finally { File.Delete(path); }
        return path;
    }

    [Fact]
    public async Task ImportAsync_ValidFile_AddsOrders()
    {
        var service = new OrderService(new InMemoryOrderStore());

        await WithTempFile(GoodJson, async path =>
        {
            int added = await service.ImportAsync(new JsonOrderImporter(), path);
            Assert.Equal(1, added);
        });

        Assert.Single(service.All());
    }

    [Fact]
    public async Task ImportAsync_RunTwice_SkipsExistingOrders()
    {
        var service = new OrderService(new InMemoryOrderStore());

        await WithTempFile(GoodJson, async path =>
        {
            await service.ImportAsync(new JsonOrderImporter(), path);
            int second = await service.ImportAsync(new JsonOrderImporter(), path);
            Assert.Equal(0, second);
        });
    }

    [Fact]
    public async Task ImportAsync_CancelledToken_ThrowsOperationCanceled_AndSavesNothing()
    {
        var service = new OrderService(new InMemoryOrderStore());
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await WithTempFile(GoodJson, async path =>
        {
            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => service.ImportAsync(new JsonOrderImporter(), path, cts.Token));
        });

        Assert.Empty(service.All());
    }

    [Fact]
    public async Task ImportAsync_BadRecord_ThrowsDomainExceptionWithRecordNumber_AndSavesNothing()
    {
        var service = new OrderService(new InMemoryOrderStore());

        await WithTempFile(BadJson, async path =>
        {
            var ex = await Assert.ThrowsAsync<DomainException>(
                () => service.ImportAsync(new JsonOrderImporter(), path));

            Assert.Contains("Record 2", ex.Message);
            Assert.NotNull(ex.InnerException);
        });

        Assert.Empty(service.All());
    }

    [Fact]
    public async Task ImportAsync_NotJson_ThrowsDomainException()
    {
        var service = new OrderService(new InMemoryOrderStore());

        await WithTempFile("this is not json", async path =>
        {
            await Assert.ThrowsAsync<DomainException>(
                () => service.ImportAsync(new JsonOrderImporter(), path));
        });
    }

    [Fact]
    public async Task ImportAsync_MissingFile_ThrowsFileNotFound()
    {
        var service = new OrderService(new InMemoryOrderStore());

        await Assert.ThrowsAsync<FileNotFoundException>(
            () => service.ImportAsync(new JsonOrderImporter(), Path.Combine(Path.GetTempPath(), "no-such-file.json")));
    }

    [Fact]
    public async Task ImportStreamAsync_ValidFile_AddsOrders()
    {
        var service = new OrderService(new InMemoryOrderStore());

        await WithTempFile(GoodJson, async path =>
        {
            int added = await service.ImportStreamAsync(new JsonOrderImporter(), path);
            Assert.Equal(1, added);
        });
    }

    [Fact]
    public async Task ImportStreamAsync_CancelledToken_ThrowsOperationCanceled()
    {
        var service = new OrderService(new InMemoryOrderStore());
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await WithTempFile(GoodJson, async path =>
        {
            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => service.ImportStreamAsync(new JsonOrderImporter(), path, cts.Token));
        });

        Assert.Empty(service.All());
    }

    [Fact]
    public async Task ImportStreamAsync_BadSecondRecord_KeepsFirstOrder_AndReportsRecordNumber()
    {
        var service = new OrderService(new InMemoryOrderStore());

        await WithTempFile(BadJson, async path =>
        {
            var ex = await Assert.ThrowsAsync<DomainException>(
                () => service.ImportStreamAsync(new JsonOrderImporter(), path));
            Assert.Contains("Record 2", ex.Message);
        });

        // streaming is not all-or-nothing: the record read before the failure is already saved
        Assert.Single(service.All());
    }

    [Fact]
    public async Task StreamAsync_WithCancellation_TokenFlowsIntoIterator()
    {
        // [EnumeratorCancellation]: the token given to WithCancellation reaches the iterator's parameter
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await WithTempFile(GoodJson, async path =>
        {
            await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            {
                await foreach (var _ in new JsonOrderImporter().StreamAsync(path).WithCancellation(cts.Token))
                {
                }
            });
        });
    }

    // an importer that never finishes by itself: it only ends when its token is cancelled
    private sealed class HangingImporter : Core.Abstractions.IOrderImporter
    {
        public async Task<IReadOnlyList<Core.Domain.Order>> LoadAsync(string path, CancellationToken ct = default)
        {
            await Task.Delay(Timeout.Infinite, ct);
            return [];
        }

        public async IAsyncEnumerable<Core.Domain.Order> StreamAsync(string path,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
        {
            await Task.Delay(Timeout.Infinite, ct);
            yield break;
        }
    }

    [Fact]
    public async Task ImportWithTimeoutAsync_TimeoutFires_ThrowsTimeoutException_NotUserCancel()
    {
        var service = new OrderService(new InMemoryOrderStore());

        var ex = await Assert.ThrowsAsync<TimeoutException>(() =>
            service.ImportWithTimeoutAsync(new HangingImporter(), "ignored", TimeSpan.FromMilliseconds(50)));

        // the cancellation that stopped the import is kept as the cause
        Assert.IsAssignableFrom<OperationCanceledException>(ex.InnerException);
    }

    [Fact]
    public async Task ImportWithTimeoutAsync_UserCancels_ThrowsOperationCanceled_NotTimeout()
    {
        var service = new OrderService(new InMemoryOrderStore());
        using var userCts = new CancellationTokenSource();
        userCts.CancelAfter(50);                       // the user cancels first, the timeout is far away

        var ex = await Record.ExceptionAsync(() =>
            service.ImportWithTimeoutAsync(new HangingImporter(), "ignored", TimeSpan.FromMinutes(10), userCts.Token));

        Assert.IsAssignableFrom<OperationCanceledException>(ex);
        Assert.IsNotType<TimeoutException>(ex);
    }

    [Fact]
    public async Task ImportWithTimeoutAsync_FinishesInTime_ReturnsCount()
    {
        var service = new OrderService(new InMemoryOrderStore());

        await WithTempFile(GoodJson, async path =>
        {
            int added = await service.ImportWithTimeoutAsync(new JsonOrderImporter(), path, TimeSpan.FromMinutes(1));
            Assert.Equal(1, added);
        });
    }

    // synchronous recorder: Progress<T> would post to the thread pool and the order of reports would not be guaranteed
    private sealed class RecordingProgress : IProgress<Core.Dto.ImportProgress>
    {
        public List<Core.Dto.ImportProgress> Reports { get; } = [];
        public void Report(Core.Dto.ImportProgress value) => Reports.Add(value);
    }

    private const string ThreeJson = """
        [ { "id": "P-1", "customerId": "CU-01", "status": "Draft", "lines": [] },
          { "id": "P-2", "customerId": "CU-02", "status": "Draft", "lines": [] },
          { "id": "P-3", "customerId": "CU-03", "status": "Draft", "lines": [] } ]
        """;

    [Fact]
    public async Task ImportAsync_ReportsProgress_FromZeroToHundredPercent()
    {
        var service = new OrderService(new InMemoryOrderStore());
        var progress = new RecordingProgress();

        await WithTempFile(ThreeJson, async path =>
            await service.ImportAsync(new JsonOrderImporter(), path, default, progress));

        Assert.Equal(0, progress.Reports.First().Percent);
        Assert.Equal(100, progress.Reports.Last().Percent);
        Assert.Equal(3, progress.Reports.Last().Processed);
        // processed never goes backwards
        Assert.Equal(progress.Reports.OrderBy(r => r.Processed).Select(r => r.Processed),
                     progress.Reports.Select(r => r.Processed));
    }

    [Fact]
    public async Task ImportStreamAsync_ReportsFinalProgress_WithKnownTotal()
    {
        var service = new OrderService(new InMemoryOrderStore());
        var progress = new RecordingProgress();

        await WithTempFile(ThreeJson, async path =>
            await service.ImportStreamAsync(new JsonOrderImporter(), path, default, progress));

        var last = progress.Reports.Last();
        Assert.Equal(3, last.Processed);
        Assert.Equal(3, last.Total);
    }

    [Fact]
    public async Task ImportAsync_WithoutProgress_Works()
    {
        var service = new OrderService(new InMemoryOrderStore());

        await WithTempFile(ThreeJson, async path =>
            Assert.Equal(3, await service.ImportAsync(new JsonOrderImporter(), path)));
    }
}
