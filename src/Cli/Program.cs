using Core;
using Core.Domain;
using Core.Dto;
using Core.Import;
using System.Text.Json;

bool jsonMode = args.Contains("--json");
bool mixedMode = args.Contains("--mixed");
bool domainMode = args.Contains("--domain");

if (domainMode)
{
    RunDomainDemo();
    return;
}

string[] positional = args.Where(a => !a.StartsWith("--")).ToArray();
string path = positional.Length > 0 ? positional[0] : Path.Combine("data", mixedMode ? "mixed.csv" : "sample.csv");

if (!File.Exists(path))
{
    Console.WriteLine($"File not found: {Path.GetFullPath(path)}");
    return;
}

if (mixedMode)
{
    MixedImportResult mixed = MixedLineImporter.Load(path);

    Console.WriteLine("CrossApp - mixed-line import (V = Vehicle, C = Customer)");
    Console.WriteLine(new string('-', 52));
    Console.WriteLine($"File: {path}");
    Console.WriteLine($"Vehicles loaded : {mixed.Vehicles.Count}");
    foreach (VehicleDto v in mixed.Vehicles)
        Console.WriteLine($"  V {v.Id,-6} {v.Brand,-12} {v.Model,-14} {v.Year,6} {v.Price,10:F2}");

    Console.WriteLine($"Customers loaded: {mixed.Customers.Count}");
    foreach (CustomerDto c in mixed.Customers)
        Console.WriteLine($"  C {c.Id,-6} {c.FullName,-20} {c.Phone}");

    if (mixed.Errors.Count > 0)
    {
        Console.WriteLine($"Skipped lines: {mixed.Errors.Count}");
        foreach (string e in mixed.Errors)
            Console.WriteLine($"  ! {e}");
    }
    return;
}

EnvironmentReport report = EnvironmentInfo.Collect();

ImportResult<VehicleDto> result = Path.GetExtension(path).ToLowerInvariant() switch
{
    ".csv" => VehicleCsvImporter.Load(path),
    ".json" => VehicleJsonImporter.Load(path),
    var ext => new ImportResult<VehicleDto>([], [$"unsupported file extension '{ext}'"])
};

ImportStats stats = result.GetStats();

var output = new
{
    Student = "Levychkina Daryna, group FEI-34",
    report.OsDescription,
    report.FrameworkDescription,
    report.ProcessArchitecture,
    report.DetectedRid,
    report.ReportedRid,
    report.BaseDirectory,
    report.BuildNote,
    Domain = "Order (Vehicle Sales) - Customer, Vehicle, Order, OrderLine",
    ImportedCount = result.Items.Count,
    Items = result.Items,
    Errors = result.Errors,
    Stats = stats
};

if (jsonMode)
{
    Console.WriteLine(JsonSerializer.Serialize(output));
}
else
{
    Console.WriteLine("CrossApp - environment information & vehicle import");
    Console.WriteLine($"Student: {output.Student}");
    Console.WriteLine(new string('-', 52));
    Console.WriteLine($"OS               : {report.OsDescription}");
    Console.WriteLine($"Runtime          : {report.FrameworkDescription}");
    Console.WriteLine($"Architecture     : {report.ProcessArchitecture}");
    Console.WriteLine($"RID (detected)   : {report.DetectedRid}");
    Console.WriteLine($"RID (from .NET)  : {report.ReportedRid}");
    Console.WriteLine($"Directory        : {report.BaseDirectory}");
    Console.WriteLine($"Build Note       : {report.BuildNote}");
    Console.WriteLine(new string('-', 52));
    Console.WriteLine($"Domain: {output.Domain}");
    Console.WriteLine(new string('-', 52));
    Console.WriteLine($"File: {path}");
    Console.WriteLine($"Loaded records: {result.Items.Count}");
    Console.WriteLine();

    foreach (VehicleDto v in result.Items.Take(5))
        Console.WriteLine($"  {v.Id,-6} {v.Brand,-12} {v.Model,-14} {v.Year,6} {v.Price,10:F2}");

    if (result.Errors.Count > 0)
    {
        Console.WriteLine();
        Console.WriteLine($"Skipped records: {result.Errors.Count}");
        foreach (string e in result.Errors)
            Console.WriteLine($"  ! {e}");
    }

    Console.WriteLine();
    Console.WriteLine(stats.ToString());
}

void RunDomainDemo()
{
    Console.WriteLine("CrossApp - Order domain model demo");
    Console.WriteLine(new string('-', 52));

    TryDo("Scenario 1: create order and add valid lines", () =>
    {
        Order order = Order.Create("O-001", "CU-01");
        order.AddLine("V-001", "Toyota Corolla", 14500.00m, 1);
        order.AddLine("V-005", "Yamaha MT-07", 7600.00m, 2);
        order.Confirm();
        Console.WriteLine(order);
    });

    TryDo("Scenario 2: quantity must be greater than zero", () =>
    {
        Order order = Order.Create("O-002", "CU-02");
        order.AddLine("V-002", "BMW X5", 52300.00m, 0);
    });

    TryDo("Scenario 3: cannot add a line to a confirmed order", () =>
    {
        Order order = Order.Create("O-003", "CU-03");
        order.AddLine("V-003", "Honda CBR650R", 9800.50m, 1);
        order.Confirm();
        order.AddLine("V-004", "Ford Focus", 11200.00m, 1);
    });

    TryDo("Scenario 4: cannot confirm an empty order", () =>
    {
        Order order = Order.Create("O-004", "CU-04");
        order.Confirm();
    });

    TryDo("Scenario 5: round-trip through ToDto / FromDto", () =>
    {
        Order original = Order.Create("O-005", "CU-05");
        original.AddLine("V-006", "Volkswagen Golf", 10500.00m, 1);
        original.Confirm();

        OrderDto dto = original.ToDto();
        Order restored = Order.FromDto(dto);

        Console.WriteLine($"Original: {original}");
        Console.WriteLine($"Restored: {restored}");
    });

    TryDo("Scenario 6: FromDto re-validates invariants on a corrupted DTO", () =>
    {
        var brokenDto = new OrderDto(
            "O-006",
            "CU-06",
            IsConfirmed: false,
            Lines: [new OrderLineDto("V-007", "Kawasaki Ninja400", -100.00m, 1)]);

        Order.FromDto(brokenDto);
    });

    TryDo("Scenario 7: build OrderLine entities from a week-3 ImportResult<VehicleDto>", () =>
    {
        ImportResult<VehicleDto> imported = VehicleJsonImporter.Load(
            Path.Combine("data", "vehicles_for_orders.json"));

        ImportResult<OrderLine> lines = OrderLineFactory.FromVehicles(imported);

        Console.WriteLine($"Lines created: {lines.Items.Count}");
        foreach (OrderLine line in lines.Items)
            Console.WriteLine($"  {line}");

        Console.WriteLine($"Rejected: {lines.Errors.Count}");
        foreach (string e in lines.Errors)
            Console.WriteLine($"  ! {e}");
    });
}

void TryDo(string title, Action action)
{
    Console.WriteLine($"--- {title} ---");
    try
    {
        action();
        Console.WriteLine("OK");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"FAILED: {ex.GetType().Name}: {ex.Message}");
    }
    Console.WriteLine();
}