using Core;
using Core.Dto;
using Core.Import;
using System.Text.Json;

bool jsonMode = args.Contains("--json");
string[] positional = args.Where(a => !a.StartsWith("--")).ToArray();
string path = positional.Length > 0 ? positional[0] : Path.Combine("data", "sample.csv");

EnvironmentReport report = EnvironmentInfo.Collect();

if (!File.Exists(path))
{
    Console.WriteLine($"File not found: {Path.GetFullPath(path)}");
    return;
}

ImportResult<VehicleDto> result = VehicleCsvImporter.Load(path);

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
    Errors = result.Errors
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
}