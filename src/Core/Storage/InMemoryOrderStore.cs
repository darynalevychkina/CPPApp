using System;
using System.Collections.Generic;
using System.Linq;
using Core.Abstractions;
using Core.Domain;

namespace Core.Storage;

// Thread-safe: in the Api the store is a Singleton used by concurrent HTTP requests.
// A lock (not ConcurrentDictionary) keeps the insertion order of List().
public sealed class InMemoryOrderStore(IEnumerable<Order>? seed = null) : IOrderStore
{
    private readonly object _gate = new();

    private readonly Dictionary<string, Order> _items =
        (seed ?? []).ToDictionary(o => o.Id, StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<Order> List()
    {
        lock (_gate) return _items.Values.ToList();
    }

    public Order? GetById(string id)
    {
        lock (_gate) return _items.GetValueOrDefault(id);
    }

    public IReadOnlyList<Order> Search(Func<Order, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        lock (_gate) return _items.Values.Where(predicate).ToList();
    }

    public void Update(Order order)
    {
        ArgumentNullException.ThrowIfNull(order);
        lock (_gate) _items[order.Id] = order;
    }

    public bool Remove(string id)
    {
        lock (_gate) return _items.Remove(id);
    }

    public void Add(Order order)
    {
        ArgumentNullException.ThrowIfNull(order);
        lock (_gate)
        {
            if (_items.ContainsKey(order.Id))
                throw new DomainException($"Order with id={order.Id} already exists.");
            _items.Add(order.Id, order);
        }
    }
}
