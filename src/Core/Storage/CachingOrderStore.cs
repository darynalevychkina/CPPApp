using System;
using System.Collections.Generic;
using Core.Abstractions;
using Core.Domain;

namespace Core.Storage;

public sealed class CachingOrderStore(IOrderStore inner) : IOrderStore
{
    private readonly IOrderStore _inner = inner ?? throw new ArgumentNullException(nameof(inner));
    private readonly Dictionary<string, Order?> _byId = new(StringComparer.OrdinalIgnoreCase);
    private IReadOnlyList<Order>? _list;

    public int Hits { get; private set; }
    public int Misses { get; private set; }

    public IReadOnlyList<Order> List()
    {
        if (_list is not null)
        {
            Hits++;
            return _list;
        }

        Misses++;
        _list = _inner.List();
        return _list;
    }

    public Order? GetById(string id)
    {
        if (_byId.TryGetValue(id, out var cached))
        {
            Hits++;
            return cached;
        }

        Misses++;
        var order = _inner.GetById(id);
        _byId[id] = order;
        return order;
    }

    public void Add(Order order)
    {
        _inner.Add(order);
        Invalidate();
    }

    public void Update(Order order)
    {
        _inner.Update(order);
        Invalidate();
    }

    public bool Remove(string id)
    {
        bool removed = _inner.Remove(id);
        Invalidate();
        return removed;
    }

    private void Invalidate()
    {
        _list = null;
        _byId.Clear();
    }
}