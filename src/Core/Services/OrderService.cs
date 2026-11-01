using System;
using System.Collections.Generic;
using System.Linq;
using Core.Abstractions;
using Core.Domain;
using Core.Events;

namespace Core.Services;

public sealed class OrderService(IOrderStore store, IEnumerable<INotificationSink>? sinks = null) //тип параметра є інтерфейсом
{
    public const int MaxConfirmedOrdersPerCustomer = 3;

    private readonly IOrderStore _store = store ?? throw new ArgumentNullException(nameof(store));
    private readonly INotificationSink[] _sinks = sinks?.ToArray() ?? [];

    public event EventHandler<OrderChangedEventArgs>? Changed;

    private void OnChanged(Order order, OrderChangeKind kind)
    {
        var args = new OrderChangedEventArgs(order.Id, kind, order.Lines.Count, order.Total);
        Changed?.Invoke(this, args);          // way 1: event, anyone can subscribe later
        foreach (INotificationSink sink in _sinks)
            sink.Notify(args);                // way 2: sinks, fixed at construction
    }

    public Order CreateOrder(string customerId)
    {
        var order = Order.Create($"O-{Guid.NewGuid().ToString("N")[..8]}", customerId);
        _store.Add(order);
        OnChanged(order, OrderChangeKind.OrderCreated);   // after the change, not before
        return order;
    }

    public void AddLine(string orderId, string productId, string productName, decimal price, int quantity)
    {
        var order = _store.GetById(orderId)
            ?? throw new InvalidOperationException($"No order with id={orderId}.");
        order.AddLine(productId, productName, price, quantity);   // if an invariant throws, no notification
        _store.Update(order);
        OnChanged(order, OrderChangeKind.LineAdded);
    }

    public void Confirm(string orderId)
    {
        var order = _store.GetById(orderId)
            ?? throw new InvalidOperationException($"No order with id={orderId}.");

        // the cross-entity rule is itself a search by predicate
        int confirmed = _store
            .Search(o => o.CustomerId == order.CustomerId && o.IsConfirmed && o.Id != order.Id)
            .Count;

        if (confirmed >= MaxConfirmedOrdersPerCustomer)
            throw new InvalidOperationException(
                $"Customer {order.CustomerId} already has {confirmed} confirmed orders (limit is {MaxConfirmedOrdersPerCustomer})");

        order.Confirm();
        _store.Update(order);
        OnChanged(order, OrderChangeKind.OrderConfirmed);
    }

    public IReadOnlyList<Order> All() => _store.List();

    public Order? Find(string id) => _store.GetById(id);

    // the criterion is passed by the caller: Func, not an event, because we need a value back
    public IReadOnlyList<Order> Search(Func<Order, bool> predicate) => _store.Search(predicate);
}