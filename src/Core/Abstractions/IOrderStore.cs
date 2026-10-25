using System;
using System.Collections.Generic;
using Core.Domain;

namespace Core.Abstractions;

public interface IOrderStore
{
    IReadOnlyList<Order> List();
    Order? GetById(string id);
    IReadOnlyList<Order> Search(Func<Order, bool> predicate);
    void Add(Order order);
    void Update(Order order);
    bool Remove(string id);
}