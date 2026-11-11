namespace CarServices.Application.Pricing;

public interface IPriceCalculator
{
    Task<QuoteCalculationResult> CalculateQuoteAsync(
        int modelId,
        IReadOnlyCollection<int> serviceIds,
        CancellationToken ct = default);
}

