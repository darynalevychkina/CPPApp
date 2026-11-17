using System;
using System.Collections.Generic;
using Core.Domain;
using Core.Dto;

namespace Core;

public static class SampleData
{
    private enum Fate { Draft, Confirmed, Cancelled }

    public static IReadOnlyList<Order> Orders() =>
    [
        // CU-01
        Build("O-001", "CU-01", Fate.Confirmed, ("V-001", "Toyota Corolla", 14500.00m, 1), ("V-005", "Yamaha MT-07", 7600.00m, 2)),
        Build("O-002", "CU-01", Fate.Confirmed, ("V-003", "Honda CBR650R", 9800.50m, 1)),
        Build("O-003", "CU-01", Fate.Draft,     ("V-004", "Ford Focus", 11200.00m, 1)),

        // CU-02
        Build("O-004", "CU-02", Fate.Confirmed, ("V-002", "BMW X5", 52300.00m, 1)),
        Build("O-005", "CU-02", Fate.Cancelled, ("V-008", "Audi A4", 24500.00m, 1)),
        Build("O-006", "CU-02", Fate.Draft,     ("V-005", "Yamaha MT-07", 7600.00m, 1)),

        // CU-03
        Build("O-007", "CU-03", Fate.Confirmed, ("V-006", "Volkswagen Golf", 10500.00m, 2)),
        Build("O-008", "CU-03", Fate.Confirmed, ("V-010", "Skoda Octavia", 19800.00m, 1)),
        Build("O-009", "CU-03", Fate.Confirmed, ("V-009", "Suzuki GSX-R750", 8700.00m, 1)),

        // CU-04 (confirmed sum equals CU-06: shows why ThenBy is needed)
        Build("O-010", "CU-04", Fate.Confirmed, ("V-011", "Mazda CX-5", 31200.00m, 1)),
        Build("O-011", "CU-04", Fate.Draft,     ("V-012", "Toyota Camry", 21500.00m, 1)),
        Build("O-012", "CU-04", Fate.Cancelled, ("V-009", "Suzuki GSX-R750", 8700.00m, 1)),
        Build("O-018", "CU-04", Fate.Confirmed, ("V-014", "Kawasaki Ninja400", 5900.00m, 1)),

        // CU-05
        Build("O-013", "CU-05", Fate.Confirmed, ("V-012", "Toyota Camry", 21500.00m, 1), ("V-013", "Honda Civic", 16800.00m, 1)),
        Build("O-014", "CU-05", Fate.Draft,     ("V-014", "Kawasaki Ninja400", 5900.00m, 1)),

        // CU-06
        Build("O-015", "CU-06", Fate.Confirmed, ("V-015", "Renault Duster", 14300.00m, 1)),
        Build("O-016", "CU-06", Fate.Confirmed, ("V-005", "Yamaha MT-07", 7600.00m, 3)),
        Build("O-017", "CU-06", Fate.Cancelled, ("V-016", "BMW M3", 61500.00m, 1)),
    ];

    public static IReadOnlyList<CustomerDto> Customers() =>
    [
        new("CU-01", "Olena Kovalenko", "+380501110001", "olena@example.com"),
        new("CU-02", "Andrii Shevchenko", "+380501110002"),
        new("CU-03", "Iryna Bondar", "+380501110003", "iryna@example.com"),
        new("CU-04", "Taras Melnyk", "+380501110004"),
        new("CU-05", "Maria Tkachenko", "+380501110005", "maria@example.com"),
        new("CU-06", "Oleh Savchenko", "+380501110006"),
        new("CU-07", "Sofiia Lysenko", "+380501110007"),   // has no orders: shows the GroupJoin / Join difference
    ];

    // lab 7, optional task 4: many orders for timing. The fixed seed makes every run produce the same data.
    public static IReadOnlyList<Order> Generate(int count, int seed = 42)
    {
        var random = new Random(seed);
        var result = new List<Order>(count);

        for (int i = 1; i <= count; i++)
        {
            Order order = Order.Create($"G-{i:D6}", $"CU-{random.Next(1, 1001):D4}");

            int lines = random.Next(1, 4);
            for (int j = 0; j < lines; j++)
                order.AddLine($"V-{random.Next(1, 200):D3}", "Generated item", random.Next(1000, 60000), random.Next(1, 4));

            int roll = random.Next(100);
            if (roll < 60)
            {
                order.Confirm();                       // 60% confirmed
            }
            else if (roll < 75)
            {
                order.Confirm();                       // 15% cancelled
                order.Cancel();
            }                                          // 25% stay drafts

            result.Add(order);
        }

        return result;
    }

    private static Order Build(string id, string customerId, Fate fate,
        params (string ProductId, string Name, decimal Price, int Quantity)[] lines)
    {
        Order order = Order.Create(id, customerId);
        foreach (var l in lines)
            order.AddLine(l.ProductId, l.Name, l.Price, l.Quantity);

        if (fate is Fate.Confirmed or Fate.Cancelled)
            order.Confirm();
        if (fate is Fate.Cancelled)
            order.Cancel();

        return order;
    }
}