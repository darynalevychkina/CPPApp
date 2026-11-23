using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Core.Abstractions;
using Core.Domain;
using Core.Storage;

public static class StoreFactory
{
    public static IOrderStore Create(string[] args, IEnumerable<Order>? seed = null)
    {
        ArgumentNullException.ThrowIfNull(args);

        bool useFile = args.Contains("--file");
        bool useCache = args.Contains("--cache");

        // the path comes from configuration (appsettings.json / CROSSAPP_* variables), not from a literal
        string dataPath = AppConfig.Current.DataFilePath;

        IOrderStore store = useFile //composition root
            ? new FileOrderStore(dataPath)
            : new InMemoryOrderStore(seed);

        return useCache
            ? new CachingOrderStore(store)
            : store;
    }
}