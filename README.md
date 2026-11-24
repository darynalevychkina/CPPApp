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

Domain model (Order / OrderLine)

dotnet run --project src/Cli -- --domain

Order and OrderLine are encapsulated domain classes in src/Core/Domain. They have no public constructor — the only way to create one is through a static Create factory method that validates all input before the object exists. Internal state (the list of lines, the confirmed flag) is private; nothing outside the class can put an Order into an invalid state.

Invariants enforced by Order / OrderLine:

* Line quantity must be greater than zero

* Line price cannot be negative

* Product id and product name cannot be empty
  
* Cannot add a line to an already confirmed order
  
* Cannot confirm an order with no lines
  
* Cannot confirm an order that is already confirmed

Order.ToDto() / Order.FromDto() connect the domain model to the week-3 DTO layer (Core/Dto/OrderDto.cs). FromDto rebuilds an Order by calling Create and AddLine for every line, so a corrupted or hand-built DTO goes through the exact same validation as a normal Order — it cannot be used to bypass the invariants.

The domain classes (Core/Domain) do not depend on Console or File I/O, or on the Cli project — they can be reused as-is in any other application.

The Cli demo (--domain) wraps every scenario in a local TryDo(title, action) function that catches the exception and prints only its type name and message, never the full stack trace.

* Order status is an explicit enum (Draft, Confirmed, Cancelled); only these transitions are allowed: Draft -> Confirmed, Draft -> Cancelled, Confirmed -> Cancelled. Cancelled is final. Transitions are validated with a switch expression inside Order.

Publish

dotnet publish src/Cli -c Release -r win-x64 --self-contained true

dotnet publish src/Cli -c Release -r win-x64 --self-contained false

dotnet publish src/Cli -c Release -r linux-x64 --self-contained true

Run without dotnet run (from publish folder)

./src/Cli/bin/Release/net10.0/win-x64/publish/Cli.exe

Publish comparison

* win-x64, self-contained — 78 MB, 197 files, runtime required: no
* win-x64, framework-dependent — 237 KB, 10 files, runtime required: yes (.NET 10)
* linux-x64, self-contained — 80 MB, 197 files, runtime required: no
* linux-x64, framework-dependent — 153 KB, 10 files, runtime required: yes (.NET 10)

Order.Create("X", "Y").Status = OrderStatus.Confirmed;

Service layer (IOrderStore)

dotnet run --project src/Cli -- --orders (2)

dotnet run --project src/Cli -- --file (3)

OrderService depends only on the IOrderStore interface (Core/Abstractions). Two implementations: InMemoryOrderStore (data lives until the process exits) and FileOrderStore (JSON file data/orders.json next to the executable, written through OrderDto). The implementation is chosen once, in Program.cs (composition root), via the --file flag. No DI container is used.

dotnet run --project src/Cli -- --orders --cache

CachingOrderStore is a decorator: it implements IOrderStore and wraps another IOrderStore, caching reads and invalidating the cache on every write. It is enabled with --cache.

Stores accept a search predicate: IOrderStore.Search(Func<Order, bool>). The caller supplies the criterion as a lambda, so a store does not need a separate method for each query. This prepares the ground for LINQ in weeks 6-7.

## Domain events, Action / Func, lambdas (lab 6)

```
dotnet run --project src/Cli -- --events
dotnet run --project src/Cli -- --sinks
dotnet run --project src/Cli -- --skipped
```

`OrderService` raises a `Changed` event (`EventHandler<OrderChangedEventArgs>`) after every successful change: `OrderCreated`, `LineAdded`, `OrderConfirmed`. The event is raised only after the change is done and saved, so a failed operation (for example, adding a line to a confirmed order) raises nothing. `Core` does not write to the console or to files: `Cli` subscribes with `+=` using a local function (console + `logs/app.log`) and a lambda that counts events (a closure over a local variable), and then unsubscribes the local function with `-=`. Read operations (`All`, `Find`, `Search`) do not raise events.

Delegates in this lab: `Func<Order, bool>` is the search criterion of `Search` (a delegate that returns a value), `Action<string>` is an optional callback of `VehicleCsvImporter.Load` that is called for every skipped line (`--skipped`, log in `logs/skipped.log`), and `EventHandler` is the event.

Optional task 1: `INotificationSink` (in `Core/Abstractions`) with `ConsoleSink` and `FileSink` (in `Cli/Sinks`). The sinks are passed to the `OrderService` constructor in the composition root (`--sinks`, log in `logs/sinks.log`). Event or sink: an event lets any code subscribe and unsubscribe at run time, but the service does not know who listens; a sink is an explicit dependency fixed at construction, so it is visible in the constructor, easy to replace with a test double and cannot be unsubscribed.

Optional task 2: `VehicleCsvImporter.Load(path, onSkipped)`; the importer reports every skipped line through the callback, and the caller decides what to do with it (print, write to a file, count).

The log files are created next to the executable (`src/Cli/bin/.../logs/`) and are cleared at the start of every run.

## LINQ reports (lab 7)

```
- `dotnet run --project src/Cli -- report-groups` (number of orders and total sum for each status: Confirmed, Cancelled, Draft)
- `dotnet run --project src/Cli -- report-sum` (count, total, average and maximum of the confirmed orders; zeros if there are none)
- `dotnet run --project src/Cli -- report-top` (top 5 customers by the sum of their confirmed orders)
- `dotnet run --project src/Cli -- report-lazy` (demo of deferred execution: the same query gives 9, then 10 after the source changes, and ToList keeps a snapshot)
- `dotnet run --project src/Cli -- report-join` (every customer with the number and total of their orders, including a customer without orders; GroupJoin compared with Join)
- `dotnet run --project src/Cli -- report-toporders` (top 3 orders by total, any status, using our own TopBy extension method)
- `dotnet run --project src/Cli -- report-query` (the report "orders by status" written in query syntax and checked against the method syntax)
- `dotnet run -c Release --project src/Cli -- report-bench` (timing of the top customers report on 100000 generated orders, with and without ToList in the middle)
```

All the queries live in `Core` (`ReportService`) as pure functions over `IEnumerable<Order>`; they return read-only DTO records (`OrderSummary`, `CustomerTotal`, `StatusSummary`), not entities, and know nothing about the console. `Program.cs` only calls them and prints the result. The data comes from `SampleData` (18 orders, 7 customers) loaded into the in-memory store; the reports do not write anything to disk. Orders have no date, so the grouping report is by status, and the customer ranking and the average count confirmed orders only.

- `report-top`: top 5 customers by the sum of confirmed orders (`Where`, `GroupBy`, `Select`, `TopBy`). Customers CU-04 and CU-06 have equal totals, so a second sort key (`ThenBy` by id) keeps the order the same on every run.
- `report-expensive`: orders more expensive than 20000, most expensive first.
- `report-groups`: orders by status with count and total.
- `report-sum`: count, total, average and maximum of confirmed orders; an empty source gives zeros, because `Average` and `Max` throw on an empty sequence.

Deferred execution: a LINQ query such as `source.Where(o => o.Total > 20000m)` is only described when it is built; it runs every time it is enumerated (`Count()`, `foreach`, `ToList()`). In `report-lazy` the two `Count()` calls gave 9 and 10, because an order was added to the source between them. `ToList()` fixes the result, so the snapshot keeps 10 even after the source is cleared (the query itself then sees 0). That is why the report methods return ready lists.

Optional tasks:

- `report-join`: `GroupJoin` of customers and orders returns one row per customer, even a customer without orders (CU-07 appears with zeros), like a left join; `Join` returns (customer, order) pairs, so such a customer disappears (18 pairs, 6 customers of 7).
- `report-toporders`: our own extension method `TopBy(keySelector, n, tieBreaker)` (in `EnumerableExtensions`) replaces the manual `OrderByDescending` + `ThenBy` + `Take`; it is used in two reports, `TopCustomers` and `TopOrders`.
- `report-query`: the report "orders by status" written in query syntax (`from ... group ... into ...`) next to the method syntax; the command checks that both give the same result. Query syntax reads almost like SQL and is clearer when a query has several steps and an intermediate value (`let total = ...`), while method syntax is shorter for simple chains and has the operators and comparers that query syntax lacks (`Take`, `Distinct`, `StringComparer.OrdinalIgnoreCase`). Both compile to the same method calls, so the speed is the same.
- `report-bench`: Stopwatch timing on 100000 generated orders (3 warm-up runs, a forced garbage collection, the average of 5 runs, Release build). Averages of three runs (the first run after start was a cold-start outlier and is not counted): one chain with `ToList` only at the end 41.5 ms, `ToList` in the middle of the chain 32.8 ms, a lazy query enumerated twice 54.9 ms, `ToList` once and reuse 27.3 ms. `ToList` in the middle of one chain gave only a small gain and costs extra memory, but when the same query is used twice, materializing it once makes it about 2 times faster. The numbers depend on the computer.

| Variant | Average, ms |

| A: one chain, `ToList` only at the end | 41.5 |

| B: `ToList` in the middle of the chain | 32.8 |

| C: lazy query enumerated twice | 54.9 |

| D: `ToList` once, then reuse the list | 27.3 |

`ToList` in the middle of a single chain gave only a small gain (about 20%) and costs two extra lists in memory, so it is not worth adding by default. When the same query is used twice, a lazy query runs again on every enumeration, so materializing it once with `ToList` makes it about 2 times faster. The numbers depend on the computer and should be repeated several times.

Async import, cancellation, DomainException and tests (lab 8)

dotnet run --project src/Cli -- --import

dotnet run --project src/Cli -- --import data/orders_import_bad.json

dotnet run --project src/Cli -- --import-stream data/orders_import.json

dotnet run --project src/Cli -- --import-gen 300000 data/orders_big.json

dotnet run --project src/Cli -- --import data/orders_big.json --timeout 100 --progress

dotnet test

dotnet test --collect:"XPlat Code Coverage"

Orders are imported from a JSON file asynchronously (IOrderImporter / JsonOrderImporter in Core). The file is read with FileStream(useAsync: true) and JsonSerializer.DeserializeAsync, so no thread is blocked while the disk is read, and Task.Run is not used. Nothing is saved to the store until the whole file has been read and validated, so an error or a cancellation leaves the store unchanged. Orders that already exist are skipped, so a repeated import does not create duplicates. Default file: data/orders_import.json.

DomainException (src/Core/DomainException.cs) is thrown when a business rule is broken: adding a line to a non-draft order, confirming an empty order, a forbidden status transition, an unknown order id, the limit of confirmed orders per customer, a duplicate order id. A wrong argument (empty id, negative price) is still ArgumentException, and technical failures (missing file) are not wrapped. A bad record in an import file becomes DomainException("Record N: ...") with the original error kept as InnerException.

Cancellation: Ctrl+C or --timeout <ms> cancels a running import through a CancellationToken; the code checks the token between records. In Cli the catch order is OperationCanceledException (not an error), DomainException (data rejected), FileNotFoundException (file not found).

Optional tasks:

* --import-stream: streaming import with IAsyncEnumerable<Order> and await foreach; [EnumeratorCancellation] on the token parameter of the iterator. Orders are saved as they are read, so after a cancellation the already processed orders stay in the store (the regular import is all-or-nothing).
* --timeout: the user token and the timeout token are linked with CancellationTokenSource.CreateLinkedTokenSource. A timeout is reported as TimeoutException, a user cancellation stays OperationCanceledException; a test proves that it was the timeout that fired.
* --progress: IProgress<ImportProgress> reports the progress; the Cli subscriber draws the percent on one console line. A synchronous IProgress implementation is used, because Progress<T> posts callbacks to the thread pool in a console app.
* Coverage: dotnet test --collect:"XPlat Code Coverage" (coverlet.collector). Result for Core: 43.35 % of lines, 40.52 % of branches. The code added in this lab (DomainException, importer, OrderService, Order) is covered 88–100 %; the lower total comes from earlier labs' code that is checked by manual runs (file and cache stores, vehicle importers, some reports).

Unit tests (tests/Core.Tests, xUnit)

The test project references only Core, not Cli. Tests use InMemoryOrderStore and fixed data (SampleData), temporary files are deleted in finally. 31 tests: Order invariants and DomainException, OrderService events and limit rule, reports ([Theory] with [InlineData], tie-breaker, empty sequence), async import, cancellation, streaming, timeout, progress.

Configuration, paths and secrets (lab 9)

dotnet run --project src/Cli -- --export

Settings are read from src/Cli/appsettings.json (no secrets in it) and appsettings.{Environment}.json. The profile is chosen with CROSSAPP_ENVIRONMENT (default Development: log level Debug; Production: Warning and data folder data-prod). Later sources override earlier ones: appsettings.json, profile file, user-secrets, CROSSAPP_* environment variables, command line.

| Key | Default | Environment variable |
|---|---|---|
| Storage:DataPath | data | CROSSAPP_Storage__DataPath or CROSSAPP_DATA_PATH |
| Storage:FileName | orders.json | CROSSAPP_Storage__FileName |
| Orders:Currency | UAH | CROSSAPP_Orders__Currency |
| Logging:MinimumLevel | Information | CROSSAPP_Logging__MinimumLevel |
| Integration:ApiKey | not in the file | CROSSAPP_Integration__ApiKey |

A relative DataPath is resolved against the application folder (AppContext.BaseDirectory), not the current folder, so the data file is the same wherever the program is started from. Paths are built with Path.Combine, there are no absolute paths in the code.

cd C:\dev\CrossApp
dotnet run --project src/Cli -- --export
cd src\Cli
dotnet run -- --export

dotnet user-secrets set "Integration:ApiKey" "local-dev-key" --project src/Cli
git grep "local-dev-key"
git grep -n "ApiKey" -- "*.json"

Override the data folder:

$env:CROSSAPP_DATA_PATH = "$env:USERPROFILE\data-test"          # PowerShell
CROSSAPP_DATA_PATH=/tmp/data-test dotnet run --project src/Cli -- --export          # bash

Secret (never stored in the repository, only UserSecretsId is):

dotnet user-secrets set "Integration:ApiKey" "local-dev-key" --project src/Cli

It is stored in %APPDATA%\Microsoft\UserSecrets\<UserSecretsId>\secrets.json (Linux/macOS: ~/.microsoft/usersecrets/<UserSecretsId>/secrets.json). --export prints only whether the key is present and its length.

--export saves all orders as JSON to the data file; --import <file> reads the same format. The file is written atomically (orders.json.tmp, then File.Move), JSON keys are camelCase, Cyrillic stays readable, a broken file gives a clear error message.

## Lab 10 — Minimal API (src/Api)

Run (port 5080 is fixed in `launchSettings.json`):

```powershell
dotnet run --project src/Api
```

API docs (Development only): Scalar — http://localhost:5080/scalar/v1, OpenAPI — http://localhost:5080/openapi/v1.json

| Method | Path | Codes |
|---|---|---|
| GET | /api/orders?page=1&pageSize=20&search=... → `{ items, total }` | 200, 400 |
| GET | /api/orders/{id} | 200, 404 |
| POST | /api/orders | 201, 400 |
| PUT | /api/orders/{id} | 200, 400, 404, 409 |
| DELETE | /api/orders/{id} | 204, 404 |
| GET | /api/orders/{id}/total | 200, 404 |

Validation: FluentValidation via an endpoint filter. Errors: ProblemDetails (`DomainException` → 409).

```powershell
curl.exe -i -X POST http://localhost:5080/api/orders -H "Content-Type: application/json" -d "{\"customerId\":\"CU-07\",\"lines\":[{\"productId\":\"V-1\",\"productName\":\"Corolla\",\"price\":14500,\"quantity\":2}]}"
curl.exe "http://localhost:5080/api/orders?page=1&pageSize=5&search=yamaha"
```
