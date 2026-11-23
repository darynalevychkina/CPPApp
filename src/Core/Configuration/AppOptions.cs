using System;
using System.IO;

namespace Core.Configuration;

/// <summary>Application settings. Bound from appsettings.json (+ environment overrides) in the Cli project.</summary>
public sealed class AppOptions
{
    public StorageOptions Storage { get; init; } = new();
    public OrdersOptions Orders { get; init; } = new();
    public LoggingOptions Logging { get; init; } = new();
    public string EnvironmentName { get; init; } = "Development";
}

public sealed class StorageOptions
{
    public string DataPath { get; init; } = "data";
    public string FileName { get; init; } = "orders.json";

    /// <summary>
    /// A relative DataPath is resolved against baseDir (not against the current directory),
    /// an absolute one is used as is. The folder is created if it is missing.
    /// </summary>
    public string ResolveFullPath(string baseDir)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(baseDir);

        string root = Path.IsPathRooted(DataPath) ? DataPath : Path.Combine(baseDir, DataPath);
        Directory.CreateDirectory(root);
        return Path.GetFullPath(Path.Combine(root, FileName));
    }

    /// <summary>A simple override (CROSSAPP_DATA_PATH) replaces the folder and keeps the file name.</summary>
    public StorageOptions WithDataPath(string? overridePath) =>
        string.IsNullOrWhiteSpace(overridePath)
            ? this
            : new StorageOptions { DataPath = overridePath, FileName = FileName };
}

public sealed class OrdersOptions
{
    public string Currency { get; init; } = "UAH";
}

public sealed class LoggingOptions
{
    public string MinimumLevel { get; init; } = "Information";
}
