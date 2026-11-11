using CarServices.Application.Bookings;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace CarServices.API.Controllers;

[ApiController]
[Route("api/bookings")]
public class BookingsController(
    IBookingService bookingService,
    IValidator<CreateBookingRequest> validator) : ControllerBase
{
    /// <summary>
    /// Creates a new booking with frozen service prices and overlap protection.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(BookingDetailsDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BookingDetailsDto>> CreateBooking(
        [FromBody] CreateBookingRequest request,
        CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            return BadRequest(new
            {
                message = validation.Errors.First().ErrorMessage,
                errors = validation.Errors.Select(e => e.ErrorMessage)
            });
        }

        var result = await bookingService.CreateBookingAsync(request, ct);
        if (!result.IsSuccess || result.Booking is null)
            return BadRequest(new { message = result.ErrorMessage });

        return CreatedAtAction(
            nameof(GetBookingByReference),
            new { reference = result.Booking.Reference },
            result.Booking);
    }

    /// <summary>
    /// Gets booking details by its reference code (e.g. CS-20261107-ABCD).
    /// </summary>
    [HttpGet("{reference}")]
    [ProducesResponseType(typeof(BookingDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BookingDetailsDto>> GetBookingByReference(
        string reference,
        CancellationToken ct)
    {
        var booking = await bookingService.GetBookingByReferenceAsync(reference, ct);
        if (booking is null)
            return NotFound(new { message = "الحجز غير موجود. يرجى التأكد من رقم الحجز." });

        return Ok(booking);
    }

    /// <summary>
    /// Cancels a booking by its reference code.
    /// </summary>
    [HttpPost("{reference}/cancel")]
    [ProducesResponseType(typeof(BookingDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BookingDetailsDto>> CancelBooking(
        string reference,
        CancellationToken ct)
    {
        var result = await bookingService.CancelBookingByReferenceAsync(reference, ct);
        if (!result.IsSuccess)
            return BadRequest(new { message = result.ErrorMessage });

        return Ok(result.Booking);
    }
}

