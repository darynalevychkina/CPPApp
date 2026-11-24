using Core.Domain;

namespace Api.Contracts;

public static class OrderMapping
{
    public static OrderResponse ToResponse(this Order order) =>
        new(order.Id,
            order.CustomerId,
            order.Status.ToString(),
            order.Lines.Select(l => new OrderLineResponse(l.ProductId, l.ProductName, l.Price, l.Quantity, l.LineTotal)).ToList(),
            order.Total);

    /// <summary>Builds a valid Draft order from an already validated request.</summary>
    public static Order ToOrder(this SaveOrderRequest request, string id)
    {
        Order order = Order.Create(id, request.CustomerId.Trim());
        foreach (OrderLineRequest line in request.Lines)
            order.AddLine(line.ProductId.Trim(), line.ProductName.Trim(), line.Price, line.Quantity);
        return order;
    }
}
