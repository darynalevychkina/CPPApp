using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Core.Domain;

namespace Core.Abstractions;

public interface IOrderImporter
{
    Task<IReadOnlyList<Order>> LoadAsync(string path, CancellationToken ct = default);

    // lab 8, optional: orders are produced one by one while the file is being read
    IAsyncEnumerable<Order> StreamAsync(string path, CancellationToken ct = default);
}
