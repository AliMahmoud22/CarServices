using CarServices.Application.Bookings;
using CarServices.Domain.Enums;

namespace CarServices.Application.Admin;

public record AdminResult<T>(
    bool IsSuccess,
    string? ErrorMessage,
    T? Data)
{
    public static AdminResult<T> Success(T data) => new(true, null, data);
    public static AdminResult<T> Failure(string errorMessage) => new(false, errorMessage, default);
}

// Auth
public record AdminLoginRequest(string Username, string Password);
public record AdminLoginResponse(string Token, DateTime ExpiresAtUtc, string Username);

// Dashboard
public record DashboardSummaryDto(
    int TodayBookingsCount,
    int PendingBookingsCount,
    int ConfirmedBookingsCount,
    int CompletedBookingsCount,
    int CancelledBookingsCount,
    decimal CompletedRevenueTotal);

// Bookings Admin
public record AdminBookingFilterRequest(
    DateOnly? DateFrom,
    DateOnly? DateTo,
    BookingStatus? Status,
    string? Search);

public record ChangeBookingStatusRequest(
    BookingStatus Status,
    string? InternalNotes);

// Categories
public record AdminCategoryDto(int Id, string Name, int SortOrder, int ModelsCount);
public record UpsertCategoryRequest(string Name, int SortOrder);

// Brands
public record AdminBrandDto(int Id, string Name, bool IsActive, int ModelsCount);
public record UpsertBrandRequest(string Name, bool IsActive = true);

// Models
public record AdminModelDto(
    int Id,
    int BrandId,
    string BrandName,
    int CategoryId,
    string CategoryName,
    string Name,
    int? YearFrom,
    int? YearTo,
    bool IsActive);

public record UpsertModelRequest(
    int BrandId,
    int CategoryId,
    string Name,
    int? YearFrom,
    int? YearTo,
    bool IsActive = true);

// Services
public record AdminServiceDto(
    int Id,
    string Name,
    string? Description,
    bool IsActive,
    int SortOrder);

public record UpsertServiceRequest(
    string Name,
    string? Description,
    bool IsActive = true,
    int SortOrder = 0);

// Service Prices (Category-level)
public record AdminServicePriceDto(
    int Id,
    int ServiceId,
    string ServiceName,
    int CategoryId,
    string CategoryName,
    decimal Price,
    int DurationMinutes);

public record UpsertServicePriceRequest(
    int ServiceId,
    int CategoryId,
    decimal Price,
    int DurationMinutes);

public record BulkAdjustPricesRequest(
    decimal Percentage,
    int? CategoryId = null);

// Model Price Overrides
public record AdminModelPriceOverrideDto(
    int Id,
    int ServiceId,
    string ServiceName,
    int VehicleModelId,
    string BrandName,
    string ModelName,
    decimal Price,
    int DurationMinutes,
    bool IsAvailable);

public record UpsertModelPriceOverrideRequest(
    int ServiceId,
    int VehicleModelId,
    decimal Price,
    int DurationMinutes,
    bool IsAvailable = true);

// Working Hours
public record AdminWorkingHourDto(
    int Id,
    DayOfWeek Day,
    string DayNameAr,
    TimeOnly OpenTime,
    TimeOnly CloseTime,
    bool IsClosed);

public record UpdateWorkingHourItemRequest(
    DayOfWeek Day,
    TimeOnly OpenTime,
    TimeOnly CloseTime,
    bool IsClosed);

// Holidays
public record AdminHolidayDto(int Id, DateOnly Date, string? Reason);
public record CreateHolidayRequest(DateOnly Date, string? Reason);

// Shop Settings
public record AdminShopSettingDto(
    int Id,
    int MaxConcurrentBookings,
    int SlotIntervalMinutes,
    int MinHoursBeforeBooking,
    int MaxDaysAhead);

public record UpdateShopSettingRequest(
    int MaxConcurrentBookings,
    int SlotIntervalMinutes,
    int MinHoursBeforeBooking,
    int MaxDaysAhead);

