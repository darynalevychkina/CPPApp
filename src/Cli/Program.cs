using System.Runtime.InteropServices;
using System.Text.Json;

bool jsonMode = args.Contains("--json");

var info = new
{
    OSDescription = RuntimeInformation.OSDescription,
    OSVersion = Environment.OSVersion.ToString(),
    Architecture = RuntimeInformation.ProcessArchitecture.ToString(),
    DotnetVersion = Environment.Version.ToString(),
    Runtime = RuntimeInformation.FrameworkDescription,
    AppDirectory = AppContext.BaseDirectory,
    CurrentDirectory = Environment.CurrentDirectory,
    Domain = "Order (Vehicle Sales) - Customer, Vehicle, Order, OrderLine"
};

if (jsonMode)
{
    Console.WriteLine(JsonSerializer.Serialize(info));
}
else
{
    Console.WriteLine("CrossApp - Cross-Platform Programming Practicum");
    Console.WriteLine("Student: Levychkina Daryna, group FEI-34");
    Console.WriteLine(new string('-', 52));
    Console.WriteLine($"OS (OSDescription)      : {info.OSDescription}");
    Console.WriteLine($"OS (Environment)        : {info.OSVersion}");
    Console.WriteLine($"Process Architecture    : {info.Architecture}");
    Console.WriteLine($".NET Version (CLR)      : {info.DotnetVersion}");
    Console.WriteLine($"Runtime                 : {info.Runtime}");
    Console.WriteLine($"Application Directory   : {info.AppDirectory}");
    Console.WriteLine($"Current Directory       : {info.CurrentDirectory}");
    Console.WriteLine(new string('-', 52));
    Console.WriteLine($"Domain: {info.Domain}");
}