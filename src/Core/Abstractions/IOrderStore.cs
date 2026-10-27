using System;
using System.Collections.Generic;
using Core.Domain;

namespace Core.Abstractions;

public interface IOrderStore
{
    IReadOnlyList<Order> List(); //усі замовлення;
    Order? GetById(string id); //одне замовлення, null, якщо немає;
    IReadOnlyList<Order> Search(Func<Order, bool> predicate); //замовлення за довільною умовою;
    void Add(Order order); //зберегти нове;
    void Update(Order order); //зберегти після зміни;
    bool Remove(string id); //видалити, true, якщо було.
}