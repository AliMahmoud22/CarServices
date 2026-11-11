using CarServices.Domain.Enums;

namespace CarServices.Application.Bookings;

public record CreateBookingRequest(
    int ModelId,
    int? ModelYear,
    string? PlateNumber,
    List<int> ServiceIds,
    DateOnly Date,
    TimeOnly Time,
    string CustomerName,
    string CustomerPhone,
    string? CustomerEmail,
    string? CustomerNotes);

public record BookingItemDto(
    int ServiceId,
    string ServiceName,
    decimal Price,
    int DurationMinutes);

public record BookingDetailsDto(
    int Id,
    string Reference,
    BookingStatus Status,
    string StatusName,
    string CustomerName,
    string CustomerPhone,
    string? CustomerEmail,
    int VehicleModelId,
    string BrandName,
    string ModelName,
    string CategoryName,
    int? ModelYear,
    string? PlateNumber,
    DateOnly Date,
    TimeOnly StartTime,
    TimeOnly EndTime,
    DateTime StartAtUtc,
    DateTime EndAtUtc,
    decimal TotalPrice,
    int TotalDurationMinutes,
    string? CustomerNotes,
    DateTime CreatedAtUtc,
    List<BookingItemDto> Items,
    string WhatsAppShareUrl);

public record BookingOperationResult(
    bool IsSuccess,
    string? ErrorMessage,
    BookingDetailsDto? Booking)
{
    public static BookingOperationResult Success(BookingDetailsDto booking) =>
        new(true, null, booking);

    public static BookingOperationResult Failure(string errorMessage) =>
        new(false, errorMessage, null);
}

