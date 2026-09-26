using Core;
using Core.Dto;
using Core.Import;
using System.Text.Json;

bool jsonMode = args.Contains("--json");
bool mixedMode = args.Contains("--mixed");
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