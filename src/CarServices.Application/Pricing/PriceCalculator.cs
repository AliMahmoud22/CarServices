using CarServices.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CarServices.Application.Pricing;

public class PriceCalculator(IAppDbContext db) : IPriceCalculator
{
    public async Task<QuoteCalculationResult> CalculateQuoteAsync(
        int modelId,
        IReadOnlyCollection<int> serviceIds,
        CancellationToken ct = default)
    {
        if (serviceIds is null || serviceIds.Count == 0)
            return QuoteCalculationResult.Failure("يجب اختيار خدمة واحدة على الأقل.");

        var distinctServiceIds = serviceIds.Distinct().ToList();

        var model = await db.Models
            .AsNoTracking()
            .Where(m => m.Id == modelId && m.IsActive && m.Brand.IsActive)
            .Select(m => new { m.Id, m.CategoryId })
            .FirstOrDefaultAsync(ct);

        if (model is null)
            return QuoteCalculationResult.Failure("موديل السيارة غير موجود أو غير متاح.");

        var categoryPrices = await db.ServicePrices
            .AsNoTracking()
            .Where(sp => sp.CategoryId == model.CategoryId
                         && distinctServiceIds.Contains(sp.ServiceId)
                         && sp.Service.IsActive)
            .Select(sp => new
            {
                sp.ServiceId,
                sp.Service.Name,
                sp.Service.SortOrder,
                sp.Price,
                sp.DurationMinutes
            })
            .ToListAsync(ct);

        var overrides = await db.ModelPriceOverrides
            .AsNoTracking()
            .Where(o => o.VehicleModelId == model.Id && distinctServiceIds.Contains(o.ServiceId))
            .ToDictionaryAsync(o => o.ServiceId, ct);

        var items = new List<QuoteItemDto>();

        foreach (var cp in categoryPrices.OrderBy(x => x.SortOrder).ThenBy(x => x.Name))
        {
            if (overrides.TryGetValue(cp.ServiceId, out var ov))
            {
                if (!ov.IsAvailable)
                    return QuoteCalculationResult.Failure($"الخدمة '{cp.Name}' غير متاحة لهذا الموديل.");

                items.Add(new QuoteItemDto(
                    cp.ServiceId,
                    cp.Name,
                    ov.Price,
                    ov.DurationMinutes));
            }
            else
            {
                items.Add(new QuoteItemDto(
                    cp.ServiceId,
                    cp.Name,
                    cp.Price,
                    cp.DurationMinutes));
            }
        }

        if (items.Count != distinctServiceIds.Count)
            return QuoteCalculationResult.Failure("إحدى الخدمات المختارة غير متاحة لهذا الموديل.");

        var totalPrice = items.Sum(x => x.Price);
        var totalDuration = items.Sum(x => x.DurationMinutes);

        return QuoteCalculationResult.Success(new QuoteResponse(
            model.Id,
            items,
            totalPrice,
            totalDuration));
    }
}

