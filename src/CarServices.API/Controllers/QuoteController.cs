using CarServices.Application.Pricing;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace CarServices.API.Controllers;

[ApiController]
[Route("api/quote")]
public class QuoteController(
    IPriceCalculator priceCalculator,
    IValidator<QuoteRequest> validator) : ControllerBase
{
    /// <summary>
    /// Calculates the total price and total duration for a vehicle model and selected services.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(QuoteResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<QuoteResponse>> CalculateQuote(
        [FromBody] QuoteRequest request,
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

        var result = await priceCalculator.CalculateQuoteAsync(request.ModelId, request.ServiceIds, ct);
        if (!result.IsSuccess)
            return BadRequest(new { message = result.ErrorMessage });

        return Ok(result.Quote);
    }
}

