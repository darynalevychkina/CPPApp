using System;
using Core.Abstractions;
using Core.Events;

public sealed class ConsoleSink : INotificationSink
{
    public void Notify(OrderChangedEventArgs e) =>
        Console.WriteLine(OrderChangeFormat.Line(e));
}