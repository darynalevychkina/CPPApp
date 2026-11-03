using System;
using System.IO;
using Core.Abstractions;
using Core.Events;

public sealed class FileSink : INotificationSink
{
    private readonly string _path;

    public FileSink(string path)
    {
        _path = path ?? throw new ArgumentNullException(nameof(path));
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(_path))!);
    }

    public void Notify(OrderChangedEventArgs e) =>
        File.AppendAllText(_path, OrderChangeFormat.Line(e) + Environment.NewLine);
}