namespace Core.Reports;

public sealed record CustomerTotal(string CustomerId, int Orders, decimal Total);