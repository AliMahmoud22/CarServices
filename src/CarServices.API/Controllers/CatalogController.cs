using CarServices.Application.Catalog;
using Microsoft.AspNetCore.Mvc;

namespace CarServices.API.Controllers;

[ApiController]
[Route("api")]
public class CatalogController(ICatalogService catalogService) : ControllerBase
{
    /// <summary>
    /// Gets all active vehicle brands.
    /// </summary>
    [HttpGet("brands")]
    [ProducesResponseType(typeof(List<BrandDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<BrandDto>>> GetBrands(CancellationToken ct)
    {
        var brands = await catalogService.GetBrandsAsync(ct);
        return Ok(brands);
    }

    /// <summary>
    /// Gets active vehicle models for a given brand.
    /// </summary>
    [HttpGet("brands/{id:int}/models")]
    [ProducesResponseType(typeof(List<ModelDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<ModelDto>>> GetModelsByBrand(int id, CancellationToken ct)
    {
        var models = await catalogService.GetModelsByBrandAsync(id, ct);
        return Ok(models);
    }

    /// <summary>
    /// Gets available services with resolved price and duration for a given vehicle model.
    /// </summary>
    [HttpGet("models/{id:int}/services")]
    [ProducesResponseType(typeof(List<ModelServiceDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<ModelServiceDto>>> GetServicesForModel(int id, CancellationToken ct)
    {
        var services = await catalogService.GetServicesForModelAsync(id, ct);
        if (services is null)
            return NotFound(new { message = "الموديل غير موجود أو غير متاح." });

        return Ok(services);
    }
}

