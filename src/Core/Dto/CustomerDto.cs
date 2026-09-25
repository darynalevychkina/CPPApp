namespace Core.Dto;

public record CustomerDto(
    string Id,
    string FullName,
    string Phone,
    string? Email = null);