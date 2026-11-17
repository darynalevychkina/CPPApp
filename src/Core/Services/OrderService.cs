using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Core.Abstractions;
using Core.Domain;
using Core.Dto;
using Core.Events;

namespace Core.Services;

public sealed class OrderService(IOrderStore store, IEnumerable<INotificationSink>? sinks = null) //тип параметра є інтерфейсом
{
    public const int MaxConfirmedOrdersPerCustomer = 3;

    private readonly IOrderStore _store = store ?? throw new ArgumentNullException(nameof(store));
    private readonly INotificationSink[] _sinks = sinks?.ToArray() ?? [];

    public event EventHandler<OrderChangedEventArgs>? Changed; //оголошення події

    private void OnChanged(Order order, OrderChangeKind kind) 
    {
        var args = new OrderChangedEventArgs(order.Id, kind, order.Lines.Count, order.Total);
        Changed?.Invoke(this, args);          // викликає по черзі всі ці методи, передаючи їм два аргументи: хто підняв подію (this, тобто сервіс) і дані події (args: id замовлення, вид зміни, кількість рядків, сума).
        foreach (INotificationSink sink in _sinks)
            sink.Notify(args);                // way 2: sinks, fixed at construction
    }

    public Order CreateOrder(string customerId)
    {
        var order = Order.Create($"O-{Guid.NewGuid().ToString("N")[..8]}", customerId);
        _store.Add(order);
        OnChanged(order, OrderChangeKind.OrderCreated);   // after the change, not before
        return order;
    }

    public void AddLine(string orderId, string productId, string productName, decimal price, int quantity)
    {
        var order = _store.GetById(orderId)
            ?? throw new DomainException($"No order with id={orderId}.");
        order.AddLine(productId, productName, price, quantity);   // if an invariant throws, no notification
        _store.Update(order);
        OnChanged(order, OrderChangeKind.LineAdded);
    }

    public void Confirm(string orderId)
    {
        var order = _store.GetById(orderId)
            ?? throw new DomainException($"No order with id={orderId}.");

        // the cross-entity rule is itself a search by predicate
        int confirmed = _store
            .Search(o => o.CustomerId == order.CustomerId && o.IsConfirmed && o.Id != order.Id)
            .Count;

        if (confirmed >= MaxConfirmedOrdersPerCustomer)
            throw new DomainException(
                $"Customer {order.CustomerId} already has {confirmed} confirmed orders (limit is {MaxConfirmedOrdersPerCustomer})");

        order.Confirm();
        _store.Update(order);
        OnChanged(order, OrderChangeKind.OrderConfirmed);
    }

    // lab 8: async import; the token is checked inside the importer and between records
    public async Task<int> ImportAsync(IOrderImporter importer, string path, CancellationToken ct = default,
        IProgress<ImportProgress>? progress = null)
    {
        ArgumentNullException.ThrowIfNull(importer);

        IReadOnlyList<Order> loaded = await importer.LoadAsync(path, ct);

        int total = loaded.Count;
        int step = Math.Max(1, total / 100);               // at most ~100 reports, not one per order
        progress?.Report(new ImportProgress(0, total));

        int added = 0;
        int processed = 0;
        foreach (Order order in loaded)
        {
            ct.ThrowIfCancellationRequested();
            processed++;

            if (_store.GetById(order.Id) is null)           // already there: skip, not an error
            {
                _store.Add(order);
                OnChanged(order, OrderChangeKind.OrderCreated);
                added++;
            }

            if (processed % step == 0 || processed == total)
                progress?.Report(new ImportProgress(processed, total));
        }

        return added;
    }

    // lab 8, optional: streaming import. Orders are saved as they arrive, so on cancellation or a bad record
    // the ones already processed stay in the store (unlike ImportAsync, which is all-or-nothing).
    public async Task<int> ImportStreamAsync(IOrderImporter importer, string path, CancellationToken ct = default,
        IProgress<ImportProgress>? progress = null)
    {
        ArgumentNullException.ThrowIfNull(importer);

        int added = 0;
        int processed = 0;
        await foreach (Order order in importer.StreamAsync(path, ct).WithCancellation(ct))
        {
            processed++;
            if (_store.GetById(order.Id) is null)
            {
                _store.Add(order);
                OnChanged(order, OrderChangeKind.OrderCreated);
                added++;
            }

            if (processed % 100 == 0)
                progress?.Report(new ImportProgress(processed, null));   // total unknown while streaming
        }

        progress?.Report(new ImportProgress(processed, processed));       // the end: now the total is known
        return added;
    }

    // lab 8, optional: user token + timeout token, linked into one. Whoever fires first cancels the import,
    // and we can still tell the two apart: a timeout becomes TimeoutException, a user cancel stays OperationCanceledException.
    public async Task<int> ImportWithTimeoutAsync(IOrderImporter importer, string path, TimeSpan timeout,
        CancellationToken userToken = default, bool streaming = false, IProgress<ImportProgress>? progress = null)
    {
        using var timeoutCts = new CancellationTokenSource(timeout);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(userToken, timeoutCts.Token);

        try
        {
            return streaming
                ? await ImportStreamAsync(importer, path, linked.Token, progress)
                : await ImportAsync(importer, path, linked.Token, progress);
        }
        catch (OperationCanceledException ex) when (timeoutCts.IsCancellationRequested && !userToken.IsCancellationRequested)
        {
            throw new TimeoutException($"Import did not finish within {timeout.TotalMilliseconds:F0} ms", ex);
        }
    }

    public IReadOnlyList<Order> All() => _store.List();

    public Order? Find(string id) => _store.GetById(id);

    // the criterion is passed by the caller: Func, not an event, because we need a value back
    public IReadOnlyList<Order> Search(Func<Order, bool> predicate) => _store.Search(predicate);
}