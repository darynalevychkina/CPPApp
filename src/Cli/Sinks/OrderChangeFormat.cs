using Core.Events;

public static class OrderChangeFormat
{
    public static string Line(OrderChangedEventArgs e) =>
        $"{e.At:HH:mm:ss} {e.Kind,-14} {e.OrderId} lines={e.LinesCount} total={e.Total:F2}";
}