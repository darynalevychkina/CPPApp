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

        string dataPath = Path.Combine(AppContext.BaseDirectory, "data", "orders.json");

        IOrderStore store = useFile //composition root
            ? new FileOrderStore(dataPath)
            : new InMemoryOrderStore(seed);

        return useCache
            ? new CachingOrderStore(store)
            : store;
    }
}