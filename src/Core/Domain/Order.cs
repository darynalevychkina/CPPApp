using System;
using System.Collections.Generic;
using System.Linq;
using Core.Dto;

namespace Core.Domain;

public sealed class Order
{
    private readonly List<OrderLine> _lines = []; // приватне поле

    public string Id { get; }
    public string CustomerId { get; }
    public OrderStatus Status { get; private set; } = OrderStatus.Draft; // властивість без публічного set

    public bool IsConfirmed => Status == OrderStatus.Confirmed;
    public IReadOnlyList<OrderLine> Lines => _lines.AsReadOnly();
    public decimal Total => _lines.Sum(l => l.LineTotal);

    private Order(string id, string customerId)   // приватний конструктор
    {
        Id = id;
        CustomerId = customerId;
    }

    public static Order Create(string id, string customerId)
    {
        // № 4: id замовлення не порожній
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Order id cannot be empty", nameof(id));

        if (string.IsNullOrWhiteSpace(customerId))
            throw new ArgumentException("Customer id cannot be empty", nameof(customerId));

        return new Order(id, customerId);
    }

    public void AddLine(string productId, string productName, decimal price, int quantity)
    {
        // № 5: рядки додаються тільки до Draft
        if (Status != OrderStatus.Draft)
            throw new InvalidOperationException($"Order {Id} is {Status}, cannot add lines");

        OrderLine line = OrderLine.Create(productId, productName, price, quantity);
        _lines.Add(line);
    }

    public void Confirm()
    {
        // № 6: порожнє замовлення не підтверджується
        if (_lines.Count == 0)
            throw new InvalidOperationException($"Order {Id} has no lines, cannot confirm an empty order");

        ChangeStatus(OrderStatus.Confirmed);
    }

    public void Cancel() => ChangeStatus(OrderStatus.Cancelled);

    private void ChangeStatus(OrderStatus target)
    {
        // № 7: перехід має бути дозволений
        if (!CanTransition(Status, target))
            throw new InvalidOperationException(
                $"Order {Id}: transition {Status} -> {target} is not allowed");

        Status = target;
    }

// № 7: список дозволених переходів
    private static bool CanTransition(OrderStatus from, OrderStatus to) => (from, to) switch
    {
        (OrderStatus.Draft, OrderStatus.Confirmed) => true,
        (OrderStatus.Draft, OrderStatus.Cancelled) => true,
        (OrderStatus.Confirmed, OrderStatus.Cancelled) => true,
        _ => false
    };

    public OrderDto ToDto() =>
        new(Id, CustomerId, Status.ToString(),
            _lines.Select(l => new OrderLineDto(l.ProductId, l.ProductName, l.Price, l.Quantity)).ToList());

    public static Order FromDto(OrderDto dto)
    {
        if (!Enum.TryParse(dto.Status, ignoreCase: true, out OrderStatus status)
            || !Enum.IsDefined(status))
            // № 8: статус із DTO має бути відомим
            throw new ArgumentException($"Unknown order status '{dto.Status}'", nameof(dto));

        Order order = Create(dto.Id, dto.CustomerId);

        foreach (OrderLineDto line in dto.Lines)
            order.AddLine(line.ProductId, line.ProductName, line.Price, line.Quantity);

        switch (status)
        {
            case OrderStatus.Confirmed:
                order.Confirm();
                break;
            case OrderStatus.Cancelled:
                order.Cancel();
                break;
        }

        return order;
    }

    public override string ToString() =>
        $"Order {Id} (customer {CustomerId}) - {_lines.Count} line(s), total {Total:F2}, status: {Status}";
}