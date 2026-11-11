namespace CarServices.Application.Catalog;

public interface ICatalogService
{
    Task<List<BrandDto>> GetBrandsAsync(CancellationToken ct = default);
    Task<List<ModelDto>> GetModelsByBrandAsync(int brandId, CancellationToken ct = default);
    Task<List<ModelServiceDto>?> GetServicesForModelAsync(int modelId, CancellationToken ct = default);
}

