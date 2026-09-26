using System.Globalization;
using Core.Dto;

namespace Core.Import;

public static class MixedLineImporter
{
    public static MixedImportResult Load(string path)
    {
        var vehicles = new List<VehicleDto>();
        var customers = new List<CustomerDto>();
        var errors = new List<string>();

        string[] lines = File.ReadAllLines(path, System.Text.Encoding.UTF8);

        for (int i = 0; i < lines.Length; i++)
        {
            int number = i + 1;
            string line = lines[i];

            if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#'))
                continue;

            switch (ParseLine(line))
            {
                case VehicleParsed v:
                    vehicles.Add(v.Vehicle);
                    break;
                case CustomerParsed c:
                    customers.Add(c.Customer);
                    break;
                case ParseFailed f:
                    errors.Add($"line {number}: {f.Reason}");
                    break;
            }
        }

        return new MixedImportResult(vehicles, customers, errors);
    }
    private static MixedParseOutcome ParseLine(string line)
    {
        string[] parts = line.Split(';', StringSplitOptions.TrimEntries);

        return parts switch
        {
            ["V", var id, var brand, var model, var year, var price]
                when int.TryParse(year, out int y)
                  && decimal.TryParse(price, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal p)
                => new VehicleParsed(new VehicleDto(id, brand, model, y, p)),

            ["V", ..] => new ParseFailed("invalid vehicle line ('V' prefix)"),

            ["C", var id, var fullName, var phone]
                => new CustomerParsed(new CustomerDto(id, fullName, phone)),

            ["C", ..] => new ParseFailed("invalid customer line ('C' prefix)"),

            [var prefix, ..] => new ParseFailed($"unknown prefix '{prefix}'"),

            _ => new ParseFailed("empty line")
        };
    }

    private abstract record MixedParseOutcome;
    private sealed record VehicleParsed(VehicleDto Vehicle) : MixedParseOutcome;
    private sealed record CustomerParsed(CustomerDto Customer) : MixedParseOutcome;
    private sealed record ParseFailed(string Reason) : MixedParseOutcome;
}