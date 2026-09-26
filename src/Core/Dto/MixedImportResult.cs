namespace Core.Dto;

public sealed record MixedImportResult(
    IReadOnlyList<VehicleDto> Vehicles,
    IReadOnlyList<CustomerDto> Customers,
    IReadOnlyList<string> Errors);