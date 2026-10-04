namespace Core.Dto;

public sealed record OrderLineDto(string ProductId, string ProductName, decimal Price, int Quantity);

public sealed record OrderDto(string Id, string CustomerId, bool IsConfirmed, IReadOnlyList<OrderLineDto> Lines);