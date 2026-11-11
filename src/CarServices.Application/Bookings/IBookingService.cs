using CarServices.Domain.Enums;

namespace CarServices.Application.Bookings;

public interface IBookingService
{
    Task<BookingOperationResult> CreateBookingAsync(
        CreateBookingRequest request,
        CancellationToken ct = default);

    Task<BookingDetailsDto?> GetBookingByReferenceAsync(
        string reference,
        CancellationToken ct = default);

    Task<BookingOperationResult> CancelBookingByReferenceAsync(
        string reference,
        CancellationToken ct = default);

    Task<BookingOperationResult> ChangeStatusAsync(
        int bookingId,
        BookingStatus newStatus,
        string? internalNotes = null,
        CancellationToken ct = default);
}

