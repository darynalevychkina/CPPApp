# CrossApp
End-to-end project for the Cross-Platform Programming course.

Domain: Order (Vehicle Sales).
Entities: Customer, Vehicle, Order, OrderLine.
Purpose: managing orders for the purchase of cars and motorcycles, calculating order totals.

## Environment
.NET SDK 10.0, Windows x64

## Publish

dotnet publish src/Cli -c Release -r win-x64 --self-contained true

dotnet publish src/Cli -c Release -r win-x64 --self-contained false

dotnet publish src/Cli -c Release -r linux-x64 --self-contained true

## Run without dotnet run (from publish folder)
./src/Cli/bin/Release/net10.0/win-x64/publish/Cli.exe

## Run
dotnet build
dotnet run --project src/Cli
dotnet run --project src/Cli -- --json

## Publish comparison
**win-x64 — self-contained**
Size: 78 MB · Runtime required: no
**win-x64 — framework-dependent**
Size: 237 KB · Runtime required: yes (.NET 10)
**linux-x64 — self-contained**
Size: 80 MB · Runtime required: no
