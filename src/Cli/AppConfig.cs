using System;
using System.Linq;
using System.Reflection;
using Core.Configuration;
using Microsoft.Extensions.Configuration;

/// <summary>Everything the application needs from configuration, already resolved.</summary>
public sealed record AppSettings(
    string EnvironmentName,
    AppOptions Options,
    StorageOptions Storage,
    string DataFilePath,
    string? ApiKey);

/// <summary>
/// Builds the configuration. Later sources override earlier ones:
/// appsettings.json -> appsettings.{Environment}.json -> user-secrets -> CROSSAPP_* variables -> command line.
/// </summary>
public static class AppConfig
{
    private static AppSettings? _current;

    // built lazily, so modes that do not need configuration do not touch it
    public static AppSettings Current => _current ??= Load(Environment.GetCommandLineArgs().Skip(1).ToArray());

    public static AppSettings Load(string[] args)
    {
        string envName = Environment.GetEnvironmentVariable("CROSSAPP_ENVIRONMENT") ?? "Development";

        // only "--Section:Key=value" arguments are configuration overrides; flags like --file are ignored here
        string[] overrides = args.Where(a => a.StartsWith("--") && a.Contains(':') && a.Contains('=')).ToArray();

        IConfiguration config = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)                                  // not the current directory
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .AddJsonFile($"appsettings.{envName}.json", optional: true)
            .AddUserSecrets(Assembly.GetExecutingAssembly(), optional: true)        // secrets.json outside the repository
            .AddEnvironmentVariables(prefix: "CROSSAPP_")                           // CROSSAPP_Storage__DataPath
            .AddCommandLine(overrides)
            .Build();

        AppOptions options = config.Get<AppOptions>() ?? new AppOptions();

        // a simple variable CROSSAPP_DATA_PATH overrides the Storage section
        StorageOptions storage = options.Storage.WithDataPath(Environment.GetEnvironmentVariable("CROSSAPP_DATA_PATH"));

        return new AppSettings(
            envName,
            options,
            storage,
            storage.ResolveFullPath(AppContext.BaseDirectory),
            config["Integration:ApiKey"]);                                          // the secret: never printed, only its presence
    }
}
