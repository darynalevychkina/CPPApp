using System;
using System.IO;
using System.Linq;
using Core.Abstractions;
using Core.Storage;

public static class StoreFactory
{
    public static IOrderStore Create(string[] args)
    {
        ArgumentNullException.ThrowIfNull(args);

        bool useFile = args.Contains("--file");
        bool useCache = args.Contains("--cache");

        string dataPath = Path.Combine(AppContext.BaseDirectory, "data", "orders.json");

        IOrderStore store = useFile
            ? new FileOrderStore(dataPath)
            : new InMemoryOrderStore();

        return useCache
            ? new CachingOrderStore(store)
            : store;
    }
}