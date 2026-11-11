namespace CarServices.Application.Catalog;

public record BrandDto(
    int Id,
    string Name);

public record ModelDto(
    int Id,
    int BrandId,
    int CategoryId,
    string CategoryName,
    string Name,
    int? YearFrom,
    int? YearTo);

public record ModelServiceDto(
    int ServiceId,
    string Name,
    string? Description,
    decimal Price,
    int DurationMinutes);

