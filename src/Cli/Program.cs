using Core;
using Core.Abstractions;
using Core.Domain;
using Core.Dto;
using Core.Events;
using Core.Import;
using Core.Reports;
using Core.Services;
using Core.Storage;
using System.Text.Json;

bool jsonMode = args.Contains("--json");
bool mixedMode = args.Contains("--mixed");
bool domainMode = args.Contains("--domain");
bool eventsMode = args.Contains("--events");
bool sinksMode = args.Contains("--sinks");
bool skippedMode = args.Contains("--skipped");
bool ordersMode = args.Contains("--orders") || args.Contains("--file") || args.Contains("--cache");
string? reportCommand = args.FirstOrDefault(a => a.StartsWith("report-"));

if (reportCommand is not null)
{
    RunReport(reportCommand, args);
    return;
}

if (eventsMode)
{
    RunEventsDemo(args);
    return;
}

if (sinksMode)
{
    RunSinksDemo(args);
    return;
}

if (skippedMode)
{
    RunSkippedDemo();
    return;
}

if (ordersMode)
{
    RunOrdersDemo(args);
    return;
}

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

// lab 7: reports. All the queries live in ReportService (Core); here we only call them and print.
void RunReport(string command, string[] cliArgs)
{
    IOrderStore store = StoreFactory.Create(cliArgs, SampleData.Orders());
    var service = new OrderService(store);
    var items = service.All();

    Console.WriteLine($"Store: {store.GetType().Name}, orders: {items.Count}");
    Console.WriteLine(new string('-', 52));

    switch (command)
    {
        case "report-top":
            Console.WriteLine("Top 5 customers by the sum of confirmed orders");
            PrintCustomers(ReportService.TopCustomers(items, 5));
            break;

        case "report-expensive":
            Console.WriteLine("Orders more expensive than 20000");
            PrintOrders(ReportService.ExpensiveOrders(items, 20000m));
            break;

        case "report-groups":
            Console.WriteLine("Orders by status");
            foreach (var g in ReportService.ByStatus(items))
                Console.WriteLine($"  {g.Status,-10} orders: {g.Count,3}   total: {g.Total,12:F2}");
            break;

        case "report-sum":
            var t = ReportService.Totals(items);
            Console.WriteLine($"  confirmed orders {t.Orders}, total {t.Total:F2}, average {t.Average:F2}, max {t.Max:F2}");
            break;

        case "report-lazy":
            RunLazyDemo(items);
            break;

        case "report-toporders":
            Console.WriteLine("Top 3 orders by total (any status)");
            PrintOrders(ReportService.TopOrders(items, 3));
            break;

        case "report-query":
        {
            Console.WriteLine("Orders by status (query syntax: from ... group ... into ...)");
            var byQuery = ReportService.ByStatusQuery(items);
            foreach (var row in byQuery)
                Console.WriteLine($"  {row.Status,-10} orders: {row.Count,3}   total: {row.Total,12:F2}");

            Console.WriteLine($"Same result as method syntax: {byQuery.SequenceEqual(ReportService.ByStatus(items))}");
            break;
        }

        case "report-join":
        {
            var customers = SampleData.Customers();

            Console.WriteLine("GroupJoin: every customer with the orders (left join)");
            foreach (var r in ReportService.CustomerOrders(customers, items))
                Console.WriteLine($"  {r.CustomerId,-6} {r.FullName,-18} orders: {r.Orders,2}  confirmed: {r.Confirmed,2}  total: {r.ConfirmedTotal,12:F2}");

            var pairs = ReportService.CustomerOrderPairs(customers, items);
            Console.WriteLine();
            Console.WriteLine($"Join: (customer, order) pairs: {pairs.Count}, customers present: {pairs.Select(p => p.CustomerId).Distinct().Count()} of {customers.Count}");
            break;
        }

        default:
            Console.WriteLine("report-top | report-expensive | report-groups | report-sum | report-lazy | report-join | report-toporders | report-query");
            break;
    }
}

void PrintOrders(IReadOnlyList<OrderSummary> rows)
{
    foreach (var row in rows)
        Console.WriteLine($"  {row.OrderId,-8} {row.CustomerId,-6} {row.Status,-10} {row.Lines,2} line(s) {row.Total,12:F2}");
}

void PrintCustomers(IReadOnlyList<CustomerTotal> rows)
{
    foreach (var row in rows)
        Console.WriteLine($"  {row.CustomerId,-6} orders: {row.Orders,2}   total: {row.Total,12:F2}");
}

// deferred execution: the query is built once and executed every time it is enumerated
void RunLazyDemo(IReadOnlyList<Order> items)
{
    var source = items.ToList();                          // a local list: service.All() returns a snapshot
    var query = source.Where(o => o.Total > 20000m);      // the query is built, not executed
    Console.WriteLine($"Expensive before changes: {query.Count()}");

    Order extra = Order.Create("O-TMP", "CU-99");
    extra.AddLine("V-999", "Demo item", 25000.00m, 1);
    source.Add(extra);
    Console.WriteLine($"Expensive after changes : {query.Count()}");   // a different number

    var snapshot = query.ToList();                        // the result is fixed
    source.Clear();
    Console.WriteLine($"Snapshot still holds    : {snapshot.Count}"); // does not change
    Console.WriteLine($"Query now sees          : {query.Count()}");  // the source is empty now
}

void RunEventsDemo(string[] cliArgs)
{
    string logPath = Path.Combine(AppContext.BaseDirectory, "logs", "app.log");
    Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);
    File.Delete(logPath);                                   // every run starts with a fresh log

    IOrderStore store = StoreFactory.Create(cliArgs);
    var service = new OrderService(store);

    Console.WriteLine("CrossApp - domain events demo");
    Console.WriteLine($"Store: {store.GetType().Name}");
    Console.WriteLine(new string('-', 52));

    // subscriber 1: a local function (has a name, so we can unsubscribe it later)
    void OnOrderChanged(object? sender, OrderChangedEventArgs e)
    {
        string line = $"{e.At:HH:mm:ss} {e.Kind,-14} {e.OrderId} lines={e.LinesCount} total={e.Total:F2}";
        Console.WriteLine(line);
        File.AppendAllText(logPath, line + Environment.NewLine);
    }

    service.Changed += OnOrderChanged;

    // subscriber 2: a lambda with a closure over the local variable 'changes'
    int changes = 0;
    service.Changed += (_, _) =>
    {
        changes++;
        Console.WriteLine($"  counter -> {changes}");
    };

    // successful operations: each one raises exactly one event
    Order order = service.CreateOrder("CU-01");
    service.AddLine(order.Id, "V-001", "Toyota Corolla", 14500.00m, 1);
    service.AddLine(order.Id, "V-005", "Yamaha MT-07", 7600.00m, 2);
    service.Confirm(order.Id);
    Console.WriteLine($"Events received: {changes}");
    Console.WriteLine();

    // failure scenario: an exception is shown, but there is no event line
    int before = changes;
    TryDo("Failure 1: add a line to an order that does not exist", () =>
        service.AddLine("O-missing", "V-001", "Toyota Corolla", 14500.00m, 1));
    TryDo("Failure 2: add a line to an already confirmed order", () =>
        service.AddLine(order.Id, "V-002", "BMW X5", 52300.00m, 1));
    Console.WriteLine($"Events during failures: {changes - before}");
    Console.WriteLine();

    // a delegate instead of an event: we need a value back, so Func
    Console.WriteLine("Search with a predicate (Func<Order, bool>): total above 20000");
    foreach (Order found in service.Search(o => o.Total > 20000m))
        Console.WriteLine($"  {found}");
    Console.WriteLine();

    // unsubscribe: the operation still works, but the console/log handler is silent
    service.Changed -= OnOrderChanged;
    Order second = service.CreateOrder("CU-02");
    service.AddLine(second.Id, "V-003", "Honda CBR650R", 9800.50m, 1);
    Console.WriteLine($"Lines in the second order: {service.Find(second.Id)!.Lines.Count} (data changed, no log lines)");
    Console.WriteLine($"Events received: {changes}");
    Console.WriteLine($"Log file: {logPath}");
}

// optional task 1 of lab 6: the same notifications through INotificationSink passed to the constructor
void RunSinksDemo(string[] cliArgs)
{
    string logPath = Path.Combine(AppContext.BaseDirectory, "logs", "sinks.log");
    Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);
    File.Delete(logPath);

    IOrderStore store = StoreFactory.Create(cliArgs);

    // composition root: Cli decides which sinks the service gets (manual DI, as in week 5)
    var service = new OrderService(store, [new ConsoleSink(), new FileSink(logPath)]);

    Console.WriteLine("CrossApp - notification sinks demo");
    Console.WriteLine($"Store: {store.GetType().Name}");
    Console.WriteLine(new string('-', 52));

    Order order = service.CreateOrder("CU-01");
    service.AddLine(order.Id, "V-001", "Toyota Corolla", 14500.00m, 1);
    service.AddLine(order.Id, "V-005", "Yamaha MT-07", 7600.00m, 2);
    service.Confirm(order.Id);
    Console.WriteLine();

    TryDo("Failure: add a line to an already confirmed order", () =>
        service.AddLine(order.Id, "V-002", "BMW X5", 52300.00m, 1));

    Console.WriteLine("Sinks are fixed at construction: there is no way to unsubscribe.");
    Console.WriteLine($"Log file: {logPath}");
}

// optional task 2 of lab 6: Action<string> as a parameter of the import method
void RunSkippedDemo()
{
    string csvPath = Path.Combine("data", "sample.csv");
    if (!File.Exists(csvPath))
    {
        Console.WriteLine($"File not found: {Path.GetFullPath(csvPath)}");
        return;
    }

    string reportPath = Path.Combine(AppContext.BaseDirectory, "logs", "skipped.log");
    Directory.CreateDirectory(Path.GetDirectoryName(reportPath)!);
    File.Delete(reportPath);

    Console.WriteLine("CrossApp - skipped-lines report via Action<string>");
    Console.WriteLine(new string('-', 52));

    int skipped = 0;
    ImportResult<VehicleDto> imported = VehicleCsvImporter.Load(csvPath, reason =>
    {
        skipped++;
        Console.WriteLine($"  skipped: {reason}");
        File.AppendAllText(reportPath, reason + Environment.NewLine);
    });

    Console.WriteLine($"Loaded: {imported.Items.Count}, reported through the callback: {skipped}, in Errors: {imported.Errors.Count}");
    Console.WriteLine($"Report file: {reportPath}");
}

void RunOrdersDemo(string[] cliArgs)
{
    // composition root: the factory is the only place that knows concrete store classes
    IOrderStore store = StoreFactory.Create(cliArgs);
    bool cached = store is CachingOrderStore;

    var service = new OrderService(store);

    Console.WriteLine($"Store: {store.GetType().Name}");
    Console.WriteLine(new string('-', 52));

    // order 1: CU-01, confirmed
    Order created = service.CreateOrder("CU-01");
    Console.WriteLine($"Created order {created.Id} for CU-01");
    service.AddLine(created.Id, "V-001", "Toyota Corolla", 14500.00m, 1);
    service.AddLine(created.Id, "V-005", "Yamaha MT-07", 7600.00m, 2);
    Console.WriteLine("Added 2 lines");
    service.Confirm(created.Id);
    Console.WriteLine("Confirmed");

    // order 2: another customer, confirmed
    Order bmw = service.CreateOrder("CU-02");
    Console.WriteLine($"Created order {bmw.Id} for CU-02");
    service.AddLine(bmw.Id, "V-002", "BMW X5", 52300.00m, 1);
    service.Confirm(bmw.Id);
    Console.WriteLine("Added 1 line, confirmed");

    // order 3: CU-01 again, left as a draft
    Order draft = service.CreateOrder("CU-01");
    Console.WriteLine($"Created order {draft.Id} for CU-01 (draft)");
    service.AddLine(draft.Id, "V-003", "Honda CBR650R", 9800.50m, 1);
    Console.WriteLine("Added 1 line, left as draft");
    Console.WriteLine();

    Console.WriteLine($"Find by id: {service.Find(created.Id)}");
    foreach (OrderLine line in service.Find(created.Id)!.Lines)
        Console.WriteLine($"  {line}");
    if (cached)
        Console.WriteLine($"Find again: {service.Find(created.Id)}");   // second read comes from the cache

    Console.WriteLine("All orders:");
    foreach (Order o in service.All())
        Console.WriteLine($"  {o}");
    if (cached)
        service.All();                                                  // second read comes from the cache

    Console.WriteLine("Search: orders of CU-01");
    foreach (Order found in service.Search(o => o.CustomerId == "CU-01"))
        Console.WriteLine($"  {found}");

    Console.WriteLine("Search: confirmed orders with total above 20000");
    foreach (Order found in service.Search(o => o.IsConfirmed && o.Total > 20000m))
        Console.WriteLine($"  {found}");

    Console.WriteLine();
    TryDo("Failure 1: add a line to an order that does not exist", () =>
        service.AddLine("O-c90caa77", "V-001", "Toyota Corolla", 14500.00m, 1));

    TryDo("Failure 2: add an order with a duplicate id", () =>
        store.Add(created));

    if (store is CachingOrderStore cache)
        Console.WriteLine($"Cache hits: {cache.Hits}, misses: {cache.Misses}");
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
            Status: "Draft",
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

    TryDo("Scenario 8: cross-entity rule in OrderService (max 3 confirmed orders per customer)", () =>
    {
        var service = new OrderService(new InMemoryOrderStore());

        for (int i = 1; i <= 3; i++)
        {
            Order o = service.CreateOrder("CU-10");
            service.AddLine(o.Id, "V-001", "Toyota Corolla", 14500.00m, 1);
            service.Confirm(o.Id);
            Console.WriteLine($"Confirmed {o.Id} for CU-10");
        }

        Order other = service.CreateOrder("CU-20");
        service.AddLine(other.Id, "V-002", "BMW X5", 52300.00m, 1);
        service.Confirm(other.Id);
        Console.WriteLine($"Confirmed {other.Id} for CU-20 (different customer, no limit hit)");

        Order fourth = service.CreateOrder("CU-10");
        service.AddLine(fourth.Id, "V-003", "Honda CBR650R", 9800.50m, 1);
        service.Confirm(fourth.Id);
    });

    TryDo("Scenario 9: valid status transitions (Draft -> Confirmed -> Cancelled)", () =>
    {
        Order order = Order.Create("O-009", "CU-09");
        order.AddLine("V-008", "Audi A4", 24500.00m, 1);
        Console.WriteLine($"Start        : {order.Status}");

        order.Confirm();
        Console.WriteLine($"After Confirm: {order.Status}");

        order.Cancel();
        Console.WriteLine($"After Cancel : {order.Status}");
    });

    TryDo("Scenario 10: forbidden transition (Cancelled -> Confirmed)", () =>
    {
        Order order = Order.Create("O-010", "CU-10");
        order.AddLine("V-009", "Suzuki GSX-R750", 8700.00m, 1);
        order.Cancel();
        order.Confirm();
    });

    TryDo("Scenario 11: FromDto rejects an unknown status", () =>
    {
        var dto = new OrderDto("O-011", "CU-11", "Shipped", []);
        Order.FromDto(dto);
    });

    TryDo("Scenario 12: state is unchanged after a failed operation", () =>
    {
        Order order = Order.Create("O-012", "CU-12");
        order.AddLine("V-001", "Toyota Corolla", 14500.00m, 1);
        Console.WriteLine($"Before: {order}");

        try { order.AddLine("V-002", "BMW X5", 52300.00m, 0); }
        catch (Exception ex) { Console.WriteLine($"Rejected: {ex.GetType().Name}"); }

        Console.WriteLine($"After : {order}");
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