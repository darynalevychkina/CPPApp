CrossApp
End-to-end project for the Cross-Platform Programming course.

Domain: Order (Vehicle Sales). Entities: Customer, Vehicle, Order, OrderLine. Purpose: managing orders for the purchase of cars and motorcycles, calculating order totals.

Environment
.NET SDK 10.0, Windows x64

Run

dotnet build

dotnet run --project src/Cli

dotnet run --project src/Cli -- --json

Import

dotnet run --project src/Cli -- data/sample.csv

dotnet run --project src/Cli -- data/sample.json

Loads vehicle records from a CSV or JSON file. The importer is selected automatically by file extension (.csv / .json). Prints the total count, the first 5 records, a list of skipped lines with reasons, and a one-line import summary (total / accepted / skipped / error rate). If no path is given, defaults to data/sample.csv. A missing file prints a clear message instead of throwing an unhandled exception.

Mixed-line import

dotnet run --project src/Cli -- --mixed data/mixed.csv

Reads a file where each line starts with a type prefix: "V;..." for vehicles, "C;..." for customers. A single switch expression distinguishes the two record types by prefix and routes each line into its own result list.

Publish

dotnet publish src/Cli -c Release -r win-x64 --self-contained true

dotnet publish src/Cli -c Release -r win-x64 --self-contained false

dotnet publish src/Cli -c Release -r linux-x64 --self-contained true

Run without dotnet run (from publish folder)

./src/Cli/bin/Release/net10.0/win-x64/publish/Cli.exe

Publish comparison

- win-x64, self-contained — 78 MB, 197 files, runtime required: no
- win-x64, framework-dependent — 237 KB, 10 files, runtime required: yes (.NET 10)
- linux-x64, self-contained — 80 MB, 197 files, runtime required: no
- linux-x64, framework-dependent — 153 KB, 10 files, runtime required: yes (.NET 10)
