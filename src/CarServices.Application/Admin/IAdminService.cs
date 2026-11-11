using CarServices.Application.Bookings;

namespace CarServices.Application.Admin;

public interface IAdminService
{
    // Dashboard & Bookings
    Task<DashboardSummaryDto> GetDashboardSummaryAsync(CancellationToken ct = default);
    Task<List<BookingDetailsDto>> GetBookingsAsync(AdminBookingFilterRequest filter, CancellationToken ct = default);
    Task<BookingOperationResult> ChangeBookingStatusAsync(int bookingId, ChangeBookingStatusRequest request, CancellationToken ct = default);

    // Categories
    Task<List<AdminCategoryDto>> GetCategoriesAsync(CancellationToken ct = default);
    Task<AdminResult<AdminCategoryDto>> CreateCategoryAsync(UpsertCategoryRequest request, CancellationToken ct = default);
    Task<AdminResult<AdminCategoryDto>> UpdateCategoryAsync(int id, UpsertCategoryRequest request, CancellationToken ct = default);
    Task<AdminResult<bool>> DeleteCategoryAsync(int id, CancellationToken ct = default);

    // Brands
    Task<List<AdminBrandDto>> GetBrandsAsync(CancellationToken ct = default);
    Task<AdminResult<AdminBrandDto>> CreateBrandAsync(UpsertBrandRequest request, CancellationToken ct = default);
    Task<AdminResult<AdminBrandDto>> UpdateBrandAsync(int id, UpsertBrandRequest request, CancellationToken ct = default);
    Task<AdminResult<bool>> DeleteBrandAsync(int id, CancellationToken ct = default);

    // Models
    Task<List<AdminModelDto>> GetModelsAsync(int? brandId = null, int? categoryId = null, CancellationToken ct = default);
    Task<AdminResult<AdminModelDto>> CreateModelAsync(UpsertModelRequest request, CancellationToken ct = default);
    Task<AdminResult<AdminModelDto>> UpdateModelAsync(int id, UpsertModelRequest request, CancellationToken ct = default);
    Task<AdminResult<bool>> DeleteModelAsync(int id, CancellationToken ct = default);

    // Services
    Task<List<AdminServiceDto>> GetServicesAsync(CancellationToken ct = default);
    Task<AdminResult<AdminServiceDto>> CreateServiceAsync(UpsertServiceRequest request, CancellationToken ct = default);
    Task<AdminResult<AdminServiceDto>> UpdateServiceAsync(int id, UpsertServiceRequest request, CancellationToken ct = default);
    Task<AdminResult<bool>> DeleteServiceAsync(int id, CancellationToken ct = default);

    // Service Prices & Bulk Adjustment
    Task<List<AdminServicePriceDto>> GetServicePricesAsync(int? serviceId = null, int? categoryId = null, CancellationToken ct = default);
    Task<AdminResult<AdminServicePriceDto>> UpsertServicePriceAsync(UpsertServicePriceRequest request, CancellationToken ct = default);
    Task<AdminResult<bool>> DeleteServicePriceAsync(int id, CancellationToken ct = default);
    Task<AdminResult<int>> BulkAdjustPricesAsync(BulkAdjustPricesRequest request, CancellationToken ct = default);

    // Model Price Overrides
    Task<List<AdminModelPriceOverrideDto>> GetModelOverridesAsync(int? modelId = null, CancellationToken ct = default);
    Task<AdminResult<AdminModelPriceOverrideDto>> UpsertModelOverrideAsync(UpsertModelPriceOverrideRequest request, CancellationToken ct = default);
    Task<AdminResult<bool>> DeleteModelOverrideAsync(int id, CancellationToken ct = default);

    // Working Hours
    Task<List<AdminWorkingHourDto>> GetWorkingHoursAsync(CancellationToken ct = default);
    Task<AdminResult<List<AdminWorkingHourDto>>> UpdateWorkingHoursAsync(List<UpdateWorkingHourItemRequest> requests, CancellationToken ct = default);

    // Holidays
    Task<List<AdminHolidayDto>> GetHolidaysAsync(CancellationToken ct = default);
    Task<AdminResult<AdminHolidayDto>> CreateHolidayAsync(CreateHolidayRequest request, CancellationToken ct = default);
    Task<AdminResult<bool>> DeleteHolidayAsync(int id, CancellationToken ct = default);

    // Shop Settings
    Task<AdminShopSettingDto> GetShopSettingsAsync(CancellationToken ct = default);
    Task<AdminResult<AdminShopSettingDto>> UpdateShopSettingsAsync(UpdateShopSettingRequest request, CancellationToken ct = default);
}

