using System;

namespace Core.Events;

public sealed class OrderChangedEventArgs(
    string orderId, OrderChangeKind kind, int linesCount, decimal total) : EventArgs
{
    public string OrderId { get; } = orderId;
    public OrderChangeKind Kind { get; } = kind;
    public int LinesCount { get; } = linesCount;     // number of lines after the change
    public decimal Total { get; } = total;           // order total after the change
    public DateTimeOffset At { get; } = DateTimeOffset.Now;
}