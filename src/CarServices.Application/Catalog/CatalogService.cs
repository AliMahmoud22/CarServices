using CarServices.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CarServices.Application.Catalog;

public class CatalogService(IAppDbContext db) : ICatalogService
{
    public Task<List<BrandDto>> GetBrandsAsync(CancellationToken ct = default) =>
        db.Brands
            .AsNoTracking()
            .Where(b => b.IsActive)
            .OrderBy(b => b.Name)
            .Select(b => new BrandDto(b.Id, b.Name))
            .ToListAsync(ct);

    public Task<List<ModelDto>> GetModelsByBrandAsync(int brandId, CancellationToken ct = default) =>
        db.Models
            .AsNoTracking()
            .Where(m => m.BrandId == brandId && m.IsActive && m.Brand.IsActive)
            .OrderBy(m => m.Name)
            .Select(m => new ModelDto(
                m.Id,
                m.BrandId,
                m.CategoryId,
                m.Category.Name,
                m.Name,
                m.YearFrom,
                m.YearTo))
            .ToListAsync(ct);

    public async Task<List<ModelServiceDto>?> GetServicesForModelAsync(int modelId, CancellationToken ct = default)
    {
        var model = await db.Models
            .AsNoTracking()
            .Where(m => m.Id == modelId && m.IsActive && m.Brand.IsActive)
            .Select(m => new { m.Id, m.CategoryId })
            .FirstOrDefaultAsync(ct);

        if (model is null)
            return null;

        var categoryPrices = await db.ServicePrices
            .AsNoTracking()
            .Where(sp => sp.CategoryId == model.CategoryId && sp.Service.IsActive)
            .Select(sp => new
            {
                sp.ServiceId,
                sp.Service.Name,
                sp.Service.Description,
                sp.Service.SortOrder,
                sp.Price,
                sp.DurationMinutes
            })
            .ToListAsync(ct);

        var overrides = await db.ModelPriceOverrides
            .AsNoTracking()
            .Where(o => o.VehicleModelId == model.Id)
            .ToDictionaryAsync(o => o.ServiceId, ct);

        var result = new List<ModelServiceDto>();

        foreach (var cp in categoryPrices.OrderBy(x => x.SortOrder).ThenBy(x => x.Name))
        {
            if (overrides.TryGetValue(cp.ServiceId, out var ov))
            {
                if (!ov.IsAvailable)
                    continue;

                result.Add(new ModelServiceDto(
                    cp.ServiceId,
                    cp.Name,
                    cp.Description,
                    ov.Price,
                    ov.DurationMinutes));
            }
            else
            {
                result.Add(new ModelServiceDto(
                    cp.ServiceId,
                    cp.Name,
                    cp.Description,
                    cp.Price,
                    cp.DurationMinutes));
            }
        }

        return result;
    }
}

