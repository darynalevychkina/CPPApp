using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using Core.Abstractions;
using Core.Domain;
using Core.Dto;

namespace Core.Storage;

public sealed class FileOrderStore(string path) : IOrderStore
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,                       // files written before lab 9 (PascalCase) still load
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping     // Cyrillic stays readable, not \u0426
    };

    private readonly Dictionary<string, Order> _cache = new(StringComparer.OrdinalIgnoreCase);
    private readonly string _path = Path.GetFullPath(path);
    private bool _loaded;

    private void EnsureLoaded()
    {
        if (_loaded) return;
        if (File.Exists(_path))
        {
            List<OrderDto> dtos;
            try
            {
                dtos = JsonSerializer.Deserialize<List<OrderDto>>(File.ReadAllText(_path), Options) ?? [];
            }
            catch (JsonException ex)
            {
                // a clear message instead of an unhandled stack trace; the cause is kept
                throw new DomainException($"Data file '{_path}' is not valid JSON: {ex.Message}", ex);
            }

            foreach (var dto in dtos)
            {
                Order order;
                try
                {
                    order = Order.FromDto(dto);
                }
                catch (Exception ex) when (ex is ArgumentException or DomainException)
                {
                    throw new DomainException($"Data file '{_path}' has an invalid order '{dto.Id}': {ex.Message}", ex);
                }
                _cache[order.Id] = order;
            }
        }
        _loaded = true;
    }

    private void Flush()
    {
        var dtos = _cache.Values.Select(o => o.ToDto()).ToList();
        string tmp = _path + ".tmp";

        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        File.WriteAllText(tmp, JsonSerializer.Serialize(dtos, Options));   // 1) write the whole file next to the target
        File.Move(tmp, _path, overwrite: true);                            // 2) swap: the target is never half-written
    }

    public IReadOnlyList<Order> List()
    {
        EnsureLoaded();
        return _cache.Values.ToList();
    }

    public Order? GetById(string id)
    {
        EnsureLoaded();
        return _cache.GetValueOrDefault(id);
    }

    public IReadOnlyList<Order> Search(Func<Order, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        EnsureLoaded();
        return _cache.Values.Where(predicate).ToList();
    }

    public void Add(Order order)
    {
        ArgumentNullException.ThrowIfNull(order);
        EnsureLoaded();
        if (_cache.ContainsKey(order.Id))
            throw new DomainException($"Order with id={order.Id} already exists.");
        _cache.Add(order.Id, order);
        Flush();
    }

    public void Update(Order order)
    {
        EnsureLoaded();
        _cache[order.Id] = order;
        Flush();
    }

    public bool Remove(string id)
    {
        EnsureLoaded();
        if (!_cache.Remove(id)) return false;
        Flush();
        return true;
    }
}