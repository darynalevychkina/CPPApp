namespace Core.Reports;

public sealed record CustomerOrderSummary(string CustomerId, string FullName, int Orders, int Confirmed, decimal ConfirmedTotal);