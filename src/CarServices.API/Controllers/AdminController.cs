using CarServices.Application.Admin;
using CarServices.Application.Bookings;
using CarServices.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarServices.API.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin")]
public class AdminController(IAdminService adminService) : ControllerBase
{
    // =========================================================================
    // Dashboard & Bookings
    // =========================================================================

    [HttpGet("dashboard")]
    [ProducesResponseType(typeof(DashboardSummaryDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<DashboardSummaryDto>> GetDashboard(CancellationToken ct) =>
        Ok(await adminService.GetDashboardSummaryAsync(ct));

    [HttpGet("bookings")]
    [ProducesResponseType(typeof(List<BookingDetailsDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<BookingDetailsDto>>> GetBookings(
        [FromQuery] DateOnly? dateFrom,
        [FromQuery] DateOnly? dateTo,
        [FromQuery] BookingStatus? status,
        [FromQuery] string? search,
        CancellationToken ct)
    {
        var filter = new AdminBookingFilterRequest(dateFrom, dateTo, status, search);
        var bookings = await adminService.GetBookingsAsync(filter, ct);
        return Ok(bookings);
    }

    [HttpPatch("bookings/{id:int}/status")]
    [ProducesResponseType(typeof(BookingDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BookingDetailsDto>> ChangeBookingStatus(
        int id,
        [FromBody] ChangeBookingStatusRequest request,
        CancellationToken ct)
    {
        var result = await adminService.ChangeBookingStatusAsync(id, request, ct);
        if (!result.IsSuccess)
            return BadRequest(new { message = result.ErrorMessage });

        return Ok(result.Booking);
    }

    // =========================================================================
    // Categories
    // =========================================================================

    [HttpGet("categories")]
    [ProducesResponseType(typeof(List<AdminCategoryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<AdminCategoryDto>>> GetCategories(CancellationToken ct) =>
        Ok(await adminService.GetCategoriesAsync(ct));

    [HttpPost("categories")]
    [ProducesResponseType(typeof(AdminCategoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AdminCategoryDto>> CreateCategory(
        [FromBody] UpsertCategoryRequest request,
        CancellationToken ct)
    {
        var result = await adminService.CreateCategoryAsync(request, ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { message = result.ErrorMessage });
    }

    [HttpPut("categories/{id:int}")]
    [ProducesResponseType(typeof(AdminCategoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AdminCategoryDto>> UpdateCategory(
        int id,
        [FromBody] UpsertCategoryRequest request,
        CancellationToken ct)
    {
        var result = await adminService.UpdateCategoryAsync(id, request, ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { message = result.ErrorMessage });
    }

    [HttpDelete("categories/{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeleteCategory(int id, CancellationToken ct)
    {
        var result = await adminService.DeleteCategoryAsync(id, ct);
        return result.IsSuccess ? NoContent() : BadRequest(new { message = result.ErrorMessage });
    }

    // =========================================================================
    // Brands
    // =========================================================================

    [HttpGet("brands")]
    [ProducesResponseType(typeof(List<AdminBrandDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<AdminBrandDto>>> GetBrands(CancellationToken ct) =>
        Ok(await adminService.GetBrandsAsync(ct));

    [HttpPost("brands")]
    [ProducesResponseType(typeof(AdminBrandDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AdminBrandDto>> CreateBrand(
        [FromBody] UpsertBrandRequest request,
        CancellationToken ct)
    {
        var result = await adminService.CreateBrandAsync(request, ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { message = result.ErrorMessage });
    }

    [HttpPut("brands/{id:int}")]
    [ProducesResponseType(typeof(AdminBrandDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AdminBrandDto>> UpdateBrand(
        int id,
        [FromBody] UpsertBrandRequest request,
        CancellationToken ct)
    {
        var result = await adminService.UpdateBrandAsync(id, request, ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { message = result.ErrorMessage });
    }

    [HttpDelete("brands/{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeleteBrand(int id, CancellationToken ct)
    {
        var result = await adminService.DeleteBrandAsync(id, ct);
        return result.IsSuccess ? NoContent() : BadRequest(new { message = result.ErrorMessage });
    }

    // =========================================================================
    // Models
    // =========================================================================

    [HttpGet("models")]
    [ProducesResponseType(typeof(List<AdminModelDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<AdminModelDto>>> GetModels(
        [FromQuery] int? brandId,
        [FromQuery] int? categoryId,
        CancellationToken ct) =>
        Ok(await adminService.GetModelsAsync(brandId, categoryId, ct));

    [HttpPost("models")]
    [ProducesResponseType(typeof(AdminModelDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AdminModelDto>> CreateModel(
        [FromBody] UpsertModelRequest request,
        CancellationToken ct)
    {
        var result = await adminService.CreateModelAsync(request, ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { message = result.ErrorMessage });
    }

    [HttpPut("models/{id:int}")]
    [ProducesResponseType(typeof(AdminModelDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AdminModelDto>> UpdateModel(
        int id,
        [FromBody] UpsertModelRequest request,
        CancellationToken ct)
    {
        var result = await adminService.UpdateModelAsync(id, request, ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { message = result.ErrorMessage });
    }

    [HttpDelete("models/{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeleteModel(int id, CancellationToken ct)
    {
        var result = await adminService.DeleteModelAsync(id, ct);
        return result.IsSuccess ? NoContent() : BadRequest(new { message = result.ErrorMessage });
    }

    // =========================================================================
    // Services
    // =========================================================================

    [HttpGet("services")]
    [ProducesResponseType(typeof(List<AdminServiceDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<AdminServiceDto>>> GetServices(CancellationToken ct) =>
        Ok(await adminService.GetServicesAsync(ct));

    [HttpPost("services")]
    [ProducesResponseType(typeof(AdminServiceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AdminServiceDto>> CreateService(
        [FromBody] UpsertServiceRequest request,
        CancellationToken ct)
    {
        var result = await adminService.CreateServiceAsync(request, ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { message = result.ErrorMessage });
    }

    [HttpPut("services/{id:int}")]
    [ProducesResponseType(typeof(AdminServiceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AdminServiceDto>> UpdateService(
        int id,
        [FromBody] UpsertServiceRequest request,
        CancellationToken ct)
    {
        var result = await adminService.UpdateServiceAsync(id, request, ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { message = result.ErrorMessage });
    }

    [HttpDelete("services/{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeleteService(int id, CancellationToken ct)
    {
        var result = await adminService.DeleteServiceAsync(id, ct);
        return result.IsSuccess ? NoContent() : BadRequest(new { message = result.ErrorMessage });
    }

    // =========================================================================
    // Service Prices & Bulk Adjustment
    // =========================================================================

    [HttpGet("prices")]
    [ProducesResponseType(typeof(List<AdminServicePriceDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<AdminServicePriceDto>>> GetServicePrices(
        [FromQuery] int? serviceId,
        [FromQuery] int? categoryId,
        CancellationToken ct) =>
        Ok(await adminService.GetServicePricesAsync(serviceId, categoryId, ct));

    [HttpPut("prices")]
    [ProducesResponseType(typeof(AdminServicePriceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AdminServicePriceDto>> UpsertServicePrice(
        [FromBody] UpsertServicePriceRequest request,
        CancellationToken ct)
    {
        var result = await adminService.UpsertServicePriceAsync(request, ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { message = result.ErrorMessage });
    }

    [HttpDelete("prices/{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeleteServicePrice(int id, CancellationToken ct)
    {
        var result = await adminService.DeleteServicePriceAsync(id, ct);
        return result.IsSuccess ? NoContent() : BadRequest(new { message = result.ErrorMessage });
    }

    [HttpPost("prices/bulk-adjust")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> BulkAdjustPrices(
        [FromBody] BulkAdjustPricesRequest request,
        CancellationToken ct)
    {
        var result = await adminService.BulkAdjustPricesAsync(request, ct);
        if (!result.IsSuccess)
            return BadRequest(new { message = result.ErrorMessage });

        return Ok(new { updatedCount = result.Data, message = $"تم تعديل {result.Data} تسعيرة بنجاح." });
    }

    // =========================================================================
    // Model Price Overrides
    // =========================================================================

    [HttpGet("overrides")]
    [ProducesResponseType(typeof(List<AdminModelPriceOverrideDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<AdminModelPriceOverrideDto>>> GetModelOverrides(
        [FromQuery] int? modelId,
        CancellationToken ct) =>
        Ok(await adminService.GetModelOverridesAsync(modelId, ct));

    [HttpPut("overrides")]
    [ProducesResponseType(typeof(AdminModelPriceOverrideDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AdminModelPriceOverrideDto>> UpsertModelOverride(
        [FromBody] UpsertModelPriceOverrideRequest request,
        CancellationToken ct)
    {
        var result = await adminService.UpsertModelOverrideAsync(request, ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { message = result.ErrorMessage });
    }

    [HttpDelete("overrides/{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeleteModelOverride(int id, CancellationToken ct)
    {
        var result = await adminService.DeleteModelOverrideAsync(id, ct);
        return result.IsSuccess ? NoContent() : BadRequest(new { message = result.ErrorMessage });
    }

    // =========================================================================
    // Working Hours
    // =========================================================================

    [HttpGet("working-hours")]
    [ProducesResponseType(typeof(List<AdminWorkingHourDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<AdminWorkingHourDto>>> GetWorkingHours(CancellationToken ct) =>
        Ok(await adminService.GetWorkingHoursAsync(ct));

    [HttpPut("working-hours")]
    [ProducesResponseType(typeof(List<AdminWorkingHourDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<List<AdminWorkingHourDto>>> UpdateWorkingHours(
        [FromBody] List<UpdateWorkingHourItemRequest> requests,
        CancellationToken ct)
    {
        var result = await adminService.UpdateWorkingHoursAsync(requests, ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { message = result.ErrorMessage });
    }

    // =========================================================================
    // Holidays
    // =========================================================================

    [HttpGet("holidays")]
    [ProducesResponseType(typeof(List<AdminHolidayDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<AdminHolidayDto>>> GetHolidays(CancellationToken ct) =>
        Ok(await adminService.GetHolidaysAsync(ct));

    [HttpPost("holidays")]
    [ProducesResponseType(typeof(AdminHolidayDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AdminHolidayDto>> CreateHoliday(
        [FromBody] CreateHolidayRequest request,
        CancellationToken ct)
    {
        var result = await adminService.CreateHolidayAsync(request, ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { message = result.ErrorMessage });
    }

    [HttpDelete("holidays/{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeleteHoliday(int id, CancellationToken ct)
    {
        var result = await adminService.DeleteHolidayAsync(id, ct);
        return result.IsSuccess ? NoContent() : BadRequest(new { message = result.ErrorMessage });
    }

    // =========================================================================
    // Shop Settings
    // =========================================================================

    [HttpGet("settings")]
    [ProducesResponseType(typeof(AdminShopSettingDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<AdminShopSettingDto>> GetSettings(CancellationToken ct) =>
        Ok(await adminService.GetShopSettingsAsync(ct));

    [HttpPut("settings")]
    [ProducesResponseType(typeof(AdminShopSettingDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AdminShopSettingDto>> UpdateSettings(
        [FromBody] UpdateShopSettingRequest request,
        CancellationToken ct)
    {
        var result = await adminService.UpdateShopSettingsAsync(request, ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { message = result.ErrorMessage });
    }
}

