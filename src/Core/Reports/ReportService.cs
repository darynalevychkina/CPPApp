using System;
using System.Collections.Generic;
using System.Linq;
using Core.Domain;

namespace Core.Reports;

public static class ReportService
{
    // 1) filter + sort + projection: orders more expensive than the threshold
    public static IReadOnlyList<OrderSummary> ExpensiveOrders(IEnumerable<Order> source, decimal threshold) =>
        source.Where(o => o.Total > threshold)
            .OrderByDescending(o => o.Total).ThenBy(o => o.Id, StringComparer.Ordinal)
            .Select(o => new OrderSummary(o.Id, o.CustomerId, o.Status.ToString(), o.Lines.Count, o.Total))
            .ToList();

    // 2) top-N with a stable order: customers by the sum of confirmed orders
    public static IReadOnlyList<CustomerTotal> TopCustomers(IEnumerable<Order> source, int n) =>
        source.Where(o => o.IsConfirmed)
            .GroupBy(o => o.CustomerId, StringComparer.OrdinalIgnoreCase)
            .Select(g => new CustomerTotal(g.Key, g.Count(), g.Sum(o => o.Total)))
            .OrderByDescending(c => c.Total).ThenBy(c => c.CustomerId, StringComparer.Ordinal)
            .Take(n)
            .ToList();

    // 3) grouping with count and sum
    public static IReadOnlyList<StatusSummary> ByStatus(IEnumerable<Order> source) =>
        source.GroupBy(o => o.Status.ToString(), StringComparer.OrdinalIgnoreCase)
            .Select(g => new StatusSummary(g.Key, g.Count(), g.Sum(o => o.Total)))
            .OrderByDescending(s => s.Total).ThenBy(s => s.Status, StringComparer.Ordinal)
            .ToList();

    // 4) aggregates over confirmed orders, empty sequence handled
    public static (int Orders, decimal Total, decimal Average, decimal Max) Totals(IEnumerable<Order> source)
    {
        var confirmed = source.Where(o => o.IsConfirmed).ToList();
        return confirmed.Count == 0
            ? (0, 0m, 0m, 0m)
            : (confirmed.Count,
               confirmed.Sum(o => o.Total),
               confirmed.Average(o => o.Total),
               confirmed.Max(o => o.Total));
    }
}