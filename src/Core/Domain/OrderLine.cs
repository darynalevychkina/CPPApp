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
        // № 3: id товару не порожній
        if (string.IsNullOrWhiteSpace(productId))
            throw new ArgumentException("Product id cannot be empty", nameof(productId));

        // № 3: назва товару не порожня
        if (string.IsNullOrWhiteSpace(productName))
            throw new ArgumentException("Product name cannot be empty", nameof(productName));

        // № 2: ціна не від'ємна
        if (price < 0)
            throw new ArgumentOutOfRangeException(nameof(price), price, "Price cannot be negative");

        // № 1: кількість більша за нуль
        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity), quantity, "Quantity must be greater than zero");

        return new OrderLine(productId, productName, price, quantity);
    }

    public decimal LineTotal => Price * Quantity;

    public override string ToString() =>
        $"{ProductName} x{Quantity} @ {Price:F2} = {LineTotal:F2}";
}