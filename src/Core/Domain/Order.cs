using System;
using System.Collections.Generic;
using System.Linq;
using Core.Dto;

namespace Core.Domain;

public sealed class Order
{
    private readonly List<OrderLine> _lines = [];

    public string Id { get; }
    public string CustomerId { get; }
    public bool IsConfirmed { get; private set; }

    public IReadOnlyList<OrderLine> Lines => _lines.AsReadOnly();
    public decimal Total => _lines.Sum(l => l.LineTotal);

    private Order(string id, string customerId)
    {
        Id = id;
        CustomerId = customerId;
    }

    public static Order Create(string id, string customerId)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Order id cannot be empty", nameof(id));

        if (string.IsNullOrWhiteSpace(customerId))
            throw new ArgumentException("Customer id cannot be empty", nameof(customerId));

        return new Order(id, customerId);
    }

    public void AddLine(string productId, string productName, decimal price, int quantity)
    {
        if (IsConfirmed)
            throw new InvalidOperationException($"Order {Id} is already confirmed, cannot add lines");

        OrderLine line = OrderLine.Create(productId, productName, price, quantity);
        _lines.Add(line);
    }

    public void Confirm()
    {
        if (_lines.Count == 0)
            throw new InvalidOperationException($"Order {Id} has no lines, cannot confirm an empty order");

        if (IsConfirmed)
            throw new InvalidOperationException($"Order {Id} is already confirmed");

        IsConfirmed = true;
    }

    public OrderDto ToDto() =>
        new(Id, CustomerId, IsConfirmed,
            _lines.Select(l => new OrderLineDto(l.ProductId, l.ProductName, l.Price, l.Quantity)).ToList());

    public static Order FromDto(OrderDto dto)
    {
        Order order = Create(dto.Id, dto.CustomerId);

        foreach (OrderLineDto line in dto.Lines)
            order.AddLine(line.ProductId, line.ProductName, line.Price, line.Quantity);

        if (dto.IsConfirmed)
            order.Confirm();

        return order;
    }

    public override string ToString() =>
        $"Order {Id} (customer {CustomerId}) - {_lines.Count} line(s), total {Total:F2}, confirmed: {IsConfirmed}";
}