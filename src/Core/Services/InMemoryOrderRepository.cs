using System.Collections.Generic;
using System.Linq;
using Core.Domain;

namespace Core.Services;

public sealed class InMemoryOrderRepository : IOrderRepository
{
    private readonly List<Order> _orders = [];

    public void Add(Order order) => _orders.Add(order);

    public int CountConfirmedByCustomer(string customerId) =>
        _orders.Count(o => o.CustomerId == customerId && o.IsConfirmed);
}