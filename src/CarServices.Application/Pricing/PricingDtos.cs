namespace CarServices.Application.Pricing;

public record QuoteRequest(
    int ModelId,
    List<int> ServiceIds);

public record QuoteItemDto(
    int ServiceId,
    string ServiceName,
    decimal Price,
    int DurationMinutes);

public record QuoteResponse(
    int ModelId,
    List<QuoteItemDto> Items,
    decimal TotalPrice,
    int TotalDurationMinutes);

public record QuoteCalculationResult(
    bool IsSuccess,
    string? ErrorMessage,
    QuoteResponse? Quote)
{
    public static QuoteCalculationResult Success(QuoteResponse quote) =>
        new(true, null, quote);

    public static QuoteCalculationResult Failure(string errorMessage) =>
        new(false, errorMessage, null);
}

