using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Core.Abstractions;
using Core.Domain;
using Core.Dto;

namespace Core.Storage;

public sealed class JsonOrderImporter : IOrderImporter
{
    private static readonly JsonSerializerOptions Options = new() { PropertyNameCaseInsensitive = true };

    public async Task<IReadOnlyList<Order>> LoadAsync(string path, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        // true async I/O: useAsync: true, no thread is blocked while the OS reads the file
        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read,
            bufferSize: 4096, useAsync: true);

        List<OrderDto> dtos;
        try
        {
            dtos = await JsonSerializer.DeserializeAsync<List<OrderDto>>(stream, Options, ct)
                   ?? throw new DomainException("Import file is empty");
        }
        catch (JsonException ex)
        {
            throw new DomainException($"Import file is not valid JSON: {ex.Message}", ex);
        }

        var result = new List<Order>(dtos.Count);
        for (int i = 0; i < dtos.Count; i++)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                result.Add(Order.FromDto(dtos[i]));
            }
            catch (Exception ex) when (ex is ArgumentException or DomainException)
            {
                throw new DomainException($"Record {i + 1}: {ex.Message}", ex);   // keep the cause as inner exception
            }
        }

        return result;
    }

    // streaming variant: DeserializeAsyncEnumerable reads the array element by element,
    // so the whole file never has to sit in memory. [EnumeratorCancellation] makes the token
    // passed to WithCancellation(...) flow into this parameter as well.
    public async IAsyncEnumerable<Order> StreamAsync(string path,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read,
            bufferSize: 4096, useAsync: true);

        int index = 0;
        await foreach (OrderDto? dto in JsonSerializer.DeserializeAsyncEnumerable<OrderDto>(stream, Options, ct))
        {
            ct.ThrowIfCancellationRequested();
            index++;

            Order order;
            try
            {
                order = Order.FromDto(dto ?? throw new DomainException("Record is null"));
            }
            catch (Exception ex) when (ex is ArgumentException or DomainException)
            {
                throw new DomainException($"Record {index}: {ex.Message}", ex);
            }

            yield return order;
        }
    }
}
