using Core.Dto;

namespace Core.Import;

public static class VehicleCsvImporter
{
    private const char Separator = ';';

    public static ImportResult<VehicleDto> Load(string path)
    {
        var items = new List<VehicleDto>();
        var errors = new List<string>();

        string[] lines = File.ReadAllLines(path, System.Text.Encoding.UTF8);

        for (int i = 0; i < lines.Length; i++)
        {
            int number = i + 1;
            string line = lines[i];

            if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#'))
                continue;

            if (number == 1 && line.StartsWith("id", StringComparison.OrdinalIgnoreCase))
                continue; // header row

            switch (ParseLine(line))
            {
                case ParseOk ok:
                    items.Add(ok.Value);
                    break;
                case ParseFailed failed:
                    errors.Add($"line {number}: {failed.Reason}");
                    break;
            }
        }

        return new ImportResult<VehicleDto>(items, errors);
    }

    private static ParseOutcome ParseLine(string line)
    {
        string[] parts = line.Split(Separator, StringSplitOptions.TrimEntries);

        return parts switch
        {
            { Length: < 5 } => new ParseFailed($"expected 5 columns, got {parts.Length}"),

            [_, "", _, _, _] or [_, _, "", _, _]
                => new ParseFailed("brand or model is empty"),

            [_, _, _, var year, _] when !int.TryParse(year, out int y) || y < 1900 || y > DateTime.Now.Year + 1
                => new ParseFailed($"year '{year}' is out of range"),

            [_, _, _, _, var price] when !decimal.TryParse(
                price,
                System.Globalization.NumberStyles.Number,
                System.Globalization.CultureInfo.InvariantCulture,
                out decimal p) || p < 0
                => new ParseFailed($"invalid price '{price}'"),

            [var id, var brand, var model, var year, var price]
                => new ParseOk(new VehicleDto(
                    id,
                    brand,
                    model,
                    int.Parse(year),
                    decimal.Parse(price, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture))),

            _ => new ParseFailed($"too many columns: {parts.Length}")
        };
    }

    private abstract record ParseOutcome;
    private sealed record ParseOk(VehicleDto Value) : ParseOutcome;
    private sealed record ParseFailed(string Reason) : ParseOutcome;
}