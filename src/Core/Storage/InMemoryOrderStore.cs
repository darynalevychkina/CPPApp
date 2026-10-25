using System;
using System.Collections.Generic;
using System.Linq;
using Core.Abstractions;
using Core.Domain;

namespace Core.Storage;

public sealed class InMemoryOrderStore(IEnumerable<Order>? seed = null) : IOrderStore
{
    private readonly Dictionary<string, Order> _items =
        (seed ?? []).ToDictionary(o => o.Id, StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<Order> List() => _items.Values.ToList();

    public Order? GetById(string id) => _items.GetValueOrDefault(id);

    public IReadOnlyList<Order> Search(Func<Order, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        return _items.Values.Where(predicate).ToList();
    }

    public void Update(Order order) => _items[order.Id] = order;

    public bool Remove(string id) => _items.Remove(id);

    public void Add(Order order)
    {
        ArgumentNullException.ThrowIfNull(order);
        if (_items.ContainsKey(order.Id))
            throw new InvalidOperationException($"Order with id={order.Id} already exists.");
        _items.Add(order.Id, order);
    }
}