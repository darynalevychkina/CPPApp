using System;
using System.Collections.Generic;
using Core.Abstractions;
using Core.Domain;

namespace Core.Services;

public sealed class OrderService(IOrderStore store)
{
    public const int MaxConfirmedOrdersPerCustomer = 3;

    private readonly IOrderStore _store = store ?? throw new ArgumentNullException(nameof(store));

    public Order CreateOrder(string customerId)
    {
        var order = Order.Create($"O-{Guid.NewGuid().ToString("N")[..8]}", customerId);
        _store.Add(order);
        return order;
    }

    public void AddLine(string orderId, string productId, string productName, decimal price, int quantity)
    {
        var order = _store.GetById(orderId)
            ?? throw new InvalidOperationException($"No order with id={orderId}.");
        order.AddLine(productId, productName, price, quantity);
        _store.Update(order);
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
    }

    public IReadOnlyList<Order> All() => _store.List();

    public Order? Find(string id) => _store.GetById(id);

    public IReadOnlyList<Order> Search(Func<Order, bool> predicate) => _store.Search(predicate);
}