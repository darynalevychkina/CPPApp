using System;
using System.Collections.Generic;
using System.Linq;
using Core.Domain;
using Core.Dto;

namespace Core.Reports;

public static class ReportService
{
    // 1) filter + sort + projection: orders more expensive than the threshold
    public static IReadOnlyList<OrderSummary> ExpensiveOrders(IEnumerable<Order> source, decimal threshold) =>
        source.Where(o => o.Total > threshold)
            .OrderByDescending(o => o.Total).ThenBy(o => o.Id, StringComparer.Ordinal)
            .Select(o => new OrderSummary(o.Id, o.CustomerId, o.Status.ToString(), o.Lines.Count, o.Total))
            .ToList();

    // 2) top-N with a stable order: customers by the sum of confirmed orders (uses our TopBy extension)
    public static IReadOnlyList<CustomerTotal> TopCustomers(IEnumerable<Order> source, int n) =>
        source.Where(o => o.IsConfirmed)
            .GroupBy(o => o.CustomerId, StringComparer.OrdinalIgnoreCase)
            .Select(g => new CustomerTotal(g.Key, g.Count(), g.Sum(o => o.Total)))
            .TopBy(c => c.Total, n, c => c.CustomerId)
            .ToList();

    // 2b) top-N orders by total, any status (uses our TopBy extension)
    public static IReadOnlyList<OrderSummary> TopOrders(IEnumerable<Order> source, int n) =>
        source.TopBy(o => o.Total, n, o => o.Id)
            .Select(o => new OrderSummary(o.Id, o.CustomerId, o.Status.ToString(), o.Lines.Count, o.Total))
            .ToList();

    // 3) grouping with count and sum (method syntax)
    public static IReadOnlyList<StatusSummary> ByStatus(IEnumerable<Order> source) =>
        source.GroupBy(o => o.Status.ToString(), StringComparer.OrdinalIgnoreCase)
            .Select(g => new StatusSummary(g.Key, g.Count(), g.Sum(o => o.Total)))
            .OrderByDescending(s => s.Total).ThenBy(s => s.Status, StringComparer.Ordinal)
            .ToList();

    // 3b) the same report in query syntax: from ... group ... into ...
    public static IReadOnlyList<StatusSummary> ByStatusQuery(IEnumerable<Order> source)
    {
        var query =
            from o in source
            group o by o.Status.ToString() into g
            let total = g.Sum(o => o.Total)
            orderby total descending, g.Key
            select new StatusSummary(g.Key, g.Count(), total);

        return query.ToList();
    }

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

    // 5) GroupJoin: one row per customer, even when the customer has no orders (left join)
    public static IReadOnlyList<CustomerOrderSummary> CustomerOrders(IEnumerable<CustomerDto> customers, IEnumerable<Order> orders) =>
        customers.GroupJoin(
                orders,
                c => c.Id,
                o => o.CustomerId,
                (c, os) => new CustomerOrderSummary(
                    c.Id,
                    c.FullName,
                    os.Count(),
                    os.Count(o => o.IsConfirmed),
                    os.Where(o => o.IsConfirmed).Sum(o => o.Total)),
                StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(r => r.ConfirmedTotal).ThenBy(r => r.CustomerId, StringComparer.Ordinal)
            .ToList();

    // 6) Join: one row per (customer, order) pair, a customer without orders disappears (inner join)
    public static IReadOnlyList<(string CustomerId, string FullName, string OrderId, decimal Total)> CustomerOrderPairs(
        IEnumerable<CustomerDto> customers, IEnumerable<Order> orders) =>
        customers.Join(
                orders,
                c => c.Id,
                o => o.CustomerId,
                (c, o) => (CustomerId: c.Id, FullName: c.FullName, OrderId: o.Id, Total: o.Total),
                StringComparer.OrdinalIgnoreCase)
            .OrderBy(r => r.CustomerId, StringComparer.Ordinal).ThenBy(r => r.OrderId, StringComparer.Ordinal)
            .ToList();
}