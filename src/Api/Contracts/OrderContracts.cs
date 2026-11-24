namespace Api.Contracts;

// ---- output: what the client receives (domain entities are never serialized) ----

public sealed record OrderLineResponse(string ProductId, string ProductName, decimal Price, int Quantity, decimal LineTotal);

public sealed record OrderResponse(
    string Id,
    string CustomerId,
    string Status,
    IReadOnlyList<OrderLineResponse> Lines,
    decimal Total);

public sealed record OrderTotalResponse(string OrderId, int LineCount, decimal Total);

// ---- input: what the client sends (POST and PUT use the same shape) ----

public sealed record OrderLineRequest(string ProductId, string ProductName, decimal Price, int Quantity);

public sealed record SaveOrderRequest(string CustomerId, List<OrderLineRequest> Lines)
{
    public const int MaxLines = 100;

    /// <summary>Field names in <paramref name="errors"/> are the property names of the request (CustomerId, Lines[0].Quantity).</summary>
    public bool TryValidate(out Dictionary<string, string[]> errors)
    {
        var found = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(CustomerId) || CustomerId.Length > 50)
            found[nameof(CustomerId)] = ["Customer id is required, up to 50 characters."];

        if (Lines is null || Lines.Count == 0)
        {
            found[nameof(Lines)] = ["An order needs at least one line."];
        }
        else if (Lines.Count > MaxLines)
        {
            found[nameof(Lines)] = [$"An order can have at most {MaxLines} lines."];
        }
        else
        {
            for (int i = 0; i < Lines.Count; i++)
            {
                OrderLineRequest? line = Lines[i];
                string prefix = $"{nameof(Lines)}[{i}]";

                if (line is null)
                {
                    found[prefix] = ["The line must not be null."];
                    continue;
                }

                if (string.IsNullOrWhiteSpace(line.ProductId) || line.ProductId.Length > 50)
                    found[$"{prefix}.{nameof(line.ProductId)}"] = ["Product id is required, up to 50 characters."];

                if (string.IsNullOrWhiteSpace(line.ProductName) || line.ProductName.Length > 200)
                    found[$"{prefix}.{nameof(line.ProductName)}"] = ["Product name is required, up to 200 characters."];

                if (line.Price < 0)
                    found[$"{prefix}.{nameof(line.Price)}"] = ["Price cannot be negative."];

                if (line.Quantity <= 0)
                    found[$"{prefix}.{nameof(line.Quantity)}"] = ["Quantity must be greater than zero."];
            }
        }

        errors = found;
        return found.Count == 0;
    }
}
