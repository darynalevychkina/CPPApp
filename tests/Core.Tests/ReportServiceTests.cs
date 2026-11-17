using Xunit;
using Core.Reports;

namespace Core.Tests;

public class ReportServiceTests
{
    private static readonly IReadOnlyList<Core.Domain.Order> Data = SampleData.Orders();

    [Theory]
    [InlineData(10000, 12)]
    [InlineData(30000, 4)]
    [InlineData(1_000_000, 0)]
    public void ExpensiveOrders_FiltersByThreshold(decimal threshold, int expectedCount)
    {
        var rows = ReportService.ExpensiveOrders(Data, threshold);

        Assert.Equal(expectedCount, rows.Count);
        Assert.All(rows, r => Assert.True(r.Total > threshold));
    }

    [Fact]
    public void ExpensiveOrders_AreSortedByTotalDescending()
    {
        var rows = ReportService.ExpensiveOrders(Data, 0m);

        Assert.Equal(rows.OrderByDescending(r => r.Total).Select(r => r.Total), rows.Select(r => r.Total));
    }

    [Fact]
    public void TopCustomers_EqualTotals_AreOrderedByCustomerId()
    {
        // CU-04 and CU-06 have the same confirmed sum: ThenBy makes the order deterministic
        var rows = ReportService.TopCustomers(Data, 10);

        int i04 = rows.ToList().FindIndex(r => r.CustomerId == "CU-04");
        int i06 = rows.ToList().FindIndex(r => r.CustomerId == "CU-06");

        Assert.Equal(rows[i04].Total, rows[i06].Total);
        Assert.True(i04 < i06);
    }

    [Fact]
    public void Totals_EmptySequence_ReturnsZeros()
    {
        var t = ReportService.Totals([]);

        Assert.Equal((0, 0m, 0m, 0m), t);
    }
}
