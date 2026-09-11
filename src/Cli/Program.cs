using Core;
using System.Text.Json;

bool jsonMode = args.Contains("--json");
EnvironmentReport report = EnvironmentInfo.Collect();

var output = new
{
    Student = "Levychkina Daryna, group FEI-34",
    report.OsDescription,
    report.FrameworkDescription,
    report.ProcessArchitecture,
    report.DetectedRid,
    report.ReportedRid,
    report.BaseDirectory,
    Domain = "Order (Vehicle Sales) - Customer, Vehicle, Order, OrderLine"
};

if (jsonMode)
{
    Console.WriteLine(JsonSerializer.Serialize(output));
}
else
{
    Console.WriteLine("CrossApp - environment information");
    Console.WriteLine($"Student: {output.Student}");
    Console.WriteLine(new string('-', 52));
    Console.WriteLine($"OS               : {report.OsDescription}");
    Console.WriteLine($"Runtime          : {report.FrameworkDescription}");
    Console.WriteLine($"Architecture     : {report.ProcessArchitecture}");
    Console.WriteLine($"RID (detected)   : {report.DetectedRid}");
    Console.WriteLine($"RID (from .NET)  : {report.ReportedRid}");
    Console.WriteLine($"Directory        : {report.BaseDirectory}");
    Console.WriteLine(new string('-', 52));
    Console.WriteLine($"Domain: {output.Domain}");
}