using CarServices.Application.Scheduling;
using Microsoft.AspNetCore.Mvc;

namespace CarServices.API.Controllers;

[ApiController]
[Route("api/slots")]
public class SlotsController(ISchedulingService schedulingService) : ControllerBase
{
    /// <summary>
    /// Gets available booking time slots for a given date, vehicle model, and selected services.
    /// </summary>
    /// <param name="date">Booking date (YYYY-MM-DD)</param>
    /// <param name="modelId">Selected vehicle model ID</param>
    /// <param name="serviceIds">Selected service IDs (e.g. ?serviceIds=1&amp;serviceIds=2)</param>
    /// <param name="ct">Cancellation token</param>
    [HttpGet]
    [ProducesResponseType(typeof(AvailableSlotsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AvailableSlotsResponse>> GetAvailableSlots(
        [FromQuery] DateOnly date,
        [FromQuery] int modelId,
        [FromQuery] List<int> serviceIds,
        CancellationToken ct)
    {
        if (date == default)
            return BadRequest(new { message = "يجب تحديد تاريخ الحجز." });

        if (modelId <= 0)
            return BadRequest(new { message = "يجب اختيار موديل السيارة." });

        if (serviceIds is null || serviceIds.Count == 0)
            return BadRequest(new { message = "يجب اختيار خدمة واحدة على الأقل." });

        var result = await schedulingService.GetAvailableSlotsAsync(date, modelId, serviceIds, ct);
        if (!result.IsSuccess)
            return BadRequest(new { message = result.ErrorMessage });

        return Ok(result.Response);
    }
}

