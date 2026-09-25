namespace Core.Dto;

public record VehicleDto(
    string Id,
    string Brand,
    string Model,
    int Year,
    decimal Price,
    string? Note = null);