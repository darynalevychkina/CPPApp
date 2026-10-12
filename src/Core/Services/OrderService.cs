using System;
using Core.Domain;

namespace Core.Services;

public sealed class OrderService
{
    public const int MaxConfirmedOrdersPerCustomer = 3;

    private readonly IOrderRepository _orders;

    public OrderService(IOrderRepository orders)
    {
        _orders = orders;
    }

    public void ConfirmAndSave(Order order)
    {
        int confirmed = _orders.CountConfirmedByCustomer(order.CustomerId);

        if (confirmed >= MaxConfirmedOrdersPerCustomer)
            throw new InvalidOperationException(
                $"Customer {order.CustomerId} already has {confirmed} confirmed orders (limit is {MaxConfirmedOrdersPerCustomer})");

        order.Confirm();
        _orders.Add(order);
    }
}