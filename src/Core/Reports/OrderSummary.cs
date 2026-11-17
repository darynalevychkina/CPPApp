namespace Core.Reports;

public sealed record OrderSummary(string OrderId, string CustomerId, string Status, int Lines, decimal Total);