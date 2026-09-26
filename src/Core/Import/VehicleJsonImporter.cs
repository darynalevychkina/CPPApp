using System.Text.Json;
using Core.Dto;

namespace Core.Import;

public static class VehicleJsonImporter
{
    public static ImportResult<VehicleDto> Load(string path)
    {
        string json = File.ReadAllText(path);

        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        try
        {
            var items = JsonSerializer.Deserialize<List<VehicleDto>>(json, options) ?? [];
            return new ImportResult<VehicleDto>(items, []);
        }
        catch (JsonException ex)
        {
            return new ImportResult<VehicleDto>([], [$"invalid JSON: {ex.Message}"]);
        }
    }
}