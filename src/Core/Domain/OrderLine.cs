using System;

namespace Core.Domain;

public sealed class OrderLine
{
    public string ProductId { get; }
    public string ProductName { get; }
    public decimal Price { get; }
    public int Quantity { get; }

    private OrderLine(string productId, string productName, decimal price, int quantity)
    {
        ProductId = productId;
        ProductName = productName;
        Price = price;
        Quantity = quantity;
    }

    public static OrderLine Create(string productId, string productName, decimal price, int quantity)
    {
        if (string.IsNullOrWhiteSpace(productId))
            throw new ArgumentException("Product id cannot be empty", nameof(productId));

        if (string.IsNullOrWhiteSpace(productName))
            throw new ArgumentException("Product name cannot be empty", nameof(productName));

        if (price < 0)
            throw new ArgumentOutOfRangeException(nameof(price), price, "Price cannot be negative");

        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity), quantity, "Quantity must be greater than zero");

        return new OrderLine(productId, productName, price, quantity);
    }

    public decimal LineTotal => Price * Quantity;

    public override string ToString() =>
        $"{ProductName} x{Quantity} @ {Price:F2} = {LineTotal:F2}";
}