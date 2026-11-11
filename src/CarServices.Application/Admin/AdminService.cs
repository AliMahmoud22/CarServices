using CarServices.Application.Bookings;
using CarServices.Application.Common.Interfaces;
using CarServices.Application.Common.Time;
using CarServices.Domain.Entities;
using CarServices.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CarServices.Application.Admin;

public class AdminService(
    IAppDbContext db,
    IBookingService bookingService) : IAdminService
{
    // -------------------------------------------------------------------------
    // Dashboard & Bookings
    // -------------------------------------------------------------------------
    public async Task<DashboardSummaryDto> GetDashboardSummaryAsync(CancellationToken ct = default)
    {
        var todayCairo = EgyptTime.TodayInCairo();
        var todayStartUtc = EgyptTime.ToUtc(todayCairo, TimeOnly.MinValue);
        var todayEndUtc = EgyptTime.ToUtc(todayCairo.AddDays(1), TimeOnly.MinValue);

        var todayCount = await db.Bookings
            .AsNoTracking()
            .CountAsync(b => b.StartAt >= todayStartUtc && b.StartAt < todayEndUtc && b.Status != BookingStatus.Cancelled, ct);

        var statusCounts = await db.Bookings
            .AsNoTracking()
            .GroupBy(b => b.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var completedRevenue = await db.Bookings
            .AsNoTracking()
            .Where(b => b.Status == BookingStatus.Completed)
            .SumAsync(b => (decimal?)b.TotalPrice, ct) ?? 0m;

        int GetCount(BookingStatus s) => statusCounts.FirstOrDefault(x => x.Status == s)?.Count ?? 0;

        return new DashboardSummaryDto(
            TodayBookingsCount: todayCount,
            PendingBookingsCount: GetCount(BookingStatus.Pending),
            ConfirmedBookingsCount: GetCount(BookingStatus.Confirmed),
            CompletedBookingsCount: GetCount(BookingStatus.Completed),
            CancelledBookingsCount: GetCount(BookingStatus.Cancelled),
            CompletedRevenueTotal: completedRevenue);
    }

    public async Task<List<BookingDetailsDto>> GetBookingsAsync(
        AdminBookingFilterRequest filter,
        CancellationToken ct = default)
    {
        var query = db.Bookings
            .AsNoTracking()
            .Include(b => b.Customer)
            .Include(b => b.VehicleModel)
                .ThenInclude(m => m.Brand)
            .Include(b => b.VehicleModel)
                .ThenInclude(m => m.Category)
            .Include(b => b.Items)
            .AsQueryable();

        if (filter.DateFrom.HasValue)
        {
            var fromUtc = EgyptTime.ToUtc(filter.DateFrom.Value, TimeOnly.MinValue);
            query = query.Where(b => b.StartAt >= fromUtc);
        }

        if (filter.DateTo.HasValue)
        {
            var toUtc = EgyptTime.ToUtc(filter.DateTo.Value.AddDays(1), TimeOnly.MinValue);
            query = query.Where(b => b.StartAt < toUtc);
        }

        if (filter.Status.HasValue)
        {
            query = query.Where(b => b.Status == filter.Status.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = filter.Search.Trim();
            query = query.Where(b =>
                b.Reference.Contains(term) ||
                b.Customer.FullName.Contains(term) ||
                b.Customer.Phone.Contains(term) ||
                (b.PlateNumber != null && b.PlateNumber.Contains(term)));
        }

        var bookings = await query
            .OrderByDescending(b => b.StartAt)
            .ToListAsync(ct);

        return bookings.Select(MapBookingToDto).ToList();
    }

    public Task<BookingOperationResult> ChangeBookingStatusAsync(
        int bookingId,
        ChangeBookingStatusRequest request,
        CancellationToken ct = default) =>
        bookingService.ChangeStatusAsync(bookingId, request.Status, request.InternalNotes, ct);

    // -------------------------------------------------------------------------
    // Categories
    // -------------------------------------------------------------------------
    public Task<List<AdminCategoryDto>> GetCategoriesAsync(CancellationToken ct = default) =>
        db.Categories
            .AsNoTracking()
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .Select(c => new AdminCategoryDto(c.Id, c.Name, c.SortOrder, c.Models.Count))
            .ToListAsync(ct);

    public async Task<AdminResult<AdminCategoryDto>> CreateCategoryAsync(
        UpsertCategoryRequest request,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return AdminResult<AdminCategoryDto>.Failure("اسم الفئة مطلوب.");

        var name = request.Name.Trim();
        if (await db.Categories.AnyAsync(c => c.Name == name, ct))
            return AdminResult<AdminCategoryDto>.Failure("توجد فئة بنفس الاسم بالفعل.");

        var category = new VehicleCategory
        {
            Name = name,
            SortOrder = request.SortOrder
        };

        db.Categories.Add(category);
        await db.SaveChangesAsync(ct);

        return AdminResult<AdminCategoryDto>.Success(
            new AdminCategoryDto(category.Id, category.Name, category.SortOrder, 0));
    }

    public async Task<AdminResult<AdminCategoryDto>> UpdateCategoryAsync(
        int id,
        UpsertCategoryRequest request,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return AdminResult<AdminCategoryDto>.Failure("اسم الفئة مطلوب.");

        var category = await db.Categories
            .Include(c => c.Models)
            .FirstOrDefaultAsync(c => c.Id == id, ct);

        if (category is null)
            return AdminResult<AdminCategoryDto>.Failure("الفئة غير موجودة.");

        var name = request.Name.Trim();
        if (await db.Categories.AnyAsync(c => c.Id != id && c.Name == name, ct))
            return AdminResult<AdminCategoryDto>.Failure("توجد فئة أخرى بنفس الاسم.");

        category.Name = name;
        category.SortOrder = request.SortOrder;
        await db.SaveChangesAsync(ct);

        return AdminResult<AdminCategoryDto>.Success(
            new AdminCategoryDto(category.Id, category.Name, category.SortOrder, category.Models.Count));
    }

    public async Task<AdminResult<bool>> DeleteCategoryAsync(int id, CancellationToken ct = default)
    {
        var category = await db.Categories
            .Include(c => c.Models)
            .FirstOrDefaultAsync(c => c.Id == id, ct);

        if (category is null)
            return AdminResult<bool>.Failure("الفئة غير موجودة.");

        if (category.Models.Count > 0)
            return AdminResult<bool>.Failure("لا يمكن حذف الفئة لوجود موديلات مرتبطة بها.");

        db.Categories.Remove(category);
        await db.SaveChangesAsync(ct);

        return AdminResult<bool>.Success(true);
    }

    // -------------------------------------------------------------------------
    // Brands
    // -------------------------------------------------------------------------
    public Task<List<AdminBrandDto>> GetBrandsAsync(CancellationToken ct = default) =>
        db.Brands
            .AsNoTracking()
            .OrderBy(b => b.Name)
            .Select(b => new AdminBrandDto(b.Id, b.Name, b.IsActive, b.Models.Count))
            .ToListAsync(ct);

    public async Task<AdminResult<AdminBrandDto>> CreateBrandAsync(
        UpsertBrandRequest request,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return AdminResult<AdminBrandDto>.Failure("اسم الماركة مطلوب.");

        var name = request.Name.Trim();
        if (await db.Brands.AnyAsync(b => b.Name == name, ct))
            return AdminResult<AdminBrandDto>.Failure("توجد ماركة بنفس الاسم بالفعل.");

        var brand = new VehicleBrand
        {
            Name = name,
            IsActive = request.IsActive
        };

        db.Brands.Add(brand);
        await db.SaveChangesAsync(ct);

        return AdminResult<AdminBrandDto>.Success(
            new AdminBrandDto(brand.Id, brand.Name, brand.IsActive, 0));
    }

    public async Task<AdminResult<AdminBrandDto>> UpdateBrandAsync(
        int id,
        UpsertBrandRequest request,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return AdminResult<AdminBrandDto>.Failure("اسم الماركة مطلوب.");

        var brand = await db.Brands
            .Include(b => b.Models)
            .FirstOrDefaultAsync(b => b.Id == id, ct);

        if (brand is null)
            return AdminResult<AdminBrandDto>.Failure("الماركة غير موجودة.");

        var name = request.Name.Trim();
        if (await db.Brands.AnyAsync(b => b.Id != id && b.Name == name, ct))
            return AdminResult<AdminBrandDto>.Failure("توجد ماركة أخرى بنفس الاسم.");

        brand.Name = name;
        brand.IsActive = request.IsActive;
        await db.SaveChangesAsync(ct);

        return AdminResult<AdminBrandDto>.Success(
            new AdminBrandDto(brand.Id, brand.Name, brand.IsActive, brand.Models.Count));
    }

    public async Task<AdminResult<bool>> DeleteBrandAsync(int id, CancellationToken ct = default)
    {
        var brand = await db.Brands
            .Include(b => b.Models)
            .FirstOrDefaultAsync(b => b.Id == id, ct);

        if (brand is null)
            return AdminResult<bool>.Failure("الماركة غير موجودة.");

        if (brand.Models.Count > 0)
            return AdminResult<bool>.Failure("لا يمكن حذف الماركة لوجود موديلات مرتبطة بها.");

        db.Brands.Remove(brand);
        await db.SaveChangesAsync(ct);

        return AdminResult<bool>.Success(true);
    }

    // -------------------------------------------------------------------------
    // Models
    // -------------------------------------------------------------------------
    public async Task<List<AdminModelDto>> GetModelsAsync(
        int? brandId = null,
        int? categoryId = null,
        CancellationToken ct = default)
    {
        var query = db.Models.AsNoTracking().AsQueryable();

        if (brandId.HasValue)
            query = query.Where(m => m.BrandId == brandId.Value);

        if (categoryId.HasValue)
            query = query.Where(m => m.CategoryId == categoryId.Value);

        return await query
            .OrderBy(m => m.Brand.Name)
            .ThenBy(m => m.Name)
            .Select(m => new AdminModelDto(
                m.Id,
                m.BrandId,
                m.Brand.Name,
                m.CategoryId,
                m.Category.Name,
                m.Name,
                m.YearFrom,
                m.YearTo,
                m.IsActive))
            .ToListAsync(ct);
    }

    public async Task<AdminResult<AdminModelDto>> CreateModelAsync(
        UpsertModelRequest request,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return AdminResult<AdminModelDto>.Failure("اسم الموديل مطلوب.");

        var brand = await db.Brands.FirstOrDefaultAsync(b => b.Id == request.BrandId, ct);
        if (brand is null)
            return AdminResult<AdminModelDto>.Failure("الماركة غير موجودة.");

        var category = await db.Categories.FirstOrDefaultAsync(c => c.Id == request.CategoryId, ct);
        if (category is null)
            return AdminResult<AdminModelDto>.Failure("الفئة غير موجودة.");

        var name = request.Name.Trim();
        if (await db.Models.AnyAsync(m => m.BrandId == request.BrandId && m.Name == name, ct))
            return AdminResult<AdminModelDto>.Failure("يوجد موديل بنفس الاسم لهذه الماركة بالفعل.");

        var model = new VehicleModel
        {
            BrandId = request.BrandId,
            CategoryId = request.CategoryId,
            Name = name,
            YearFrom = request.YearFrom,
            YearTo = request.YearTo,
            IsActive = request.IsActive
        };

        db.Models.Add(model);
        await db.SaveChangesAsync(ct);

        return AdminResult<AdminModelDto>.Success(new AdminModelDto(
            model.Id,
            model.BrandId,
            brand.Name,
            model.CategoryId,
            category.Name,
            model.Name,
            model.YearFrom,
            model.YearTo,
            model.IsActive));
    }

    public async Task<AdminResult<AdminModelDto>> UpdateModelAsync(
        int id,
        UpsertModelRequest request,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return AdminResult<AdminModelDto>.Failure("اسم الموديل مطلوب.");

        var model = await db.Models.FirstOrDefaultAsync(m => m.Id == id, ct);
        if (model is null)
            return AdminResult<AdminModelDto>.Failure("الموديل غير موجود.");

        var brand = await db.Brands.FirstOrDefaultAsync(b => b.Id == request.BrandId, ct);
        if (brand is null)
            return AdminResult<AdminModelDto>.Failure("الماركة غير موجودة.");

        var category = await db.Categories.FirstOrDefaultAsync(c => c.Id == request.CategoryId, ct);
        if (category is null)
            return AdminResult<AdminModelDto>.Failure("الفئة غير موجودة.");

        var name = request.Name.Trim();
        if (await db.Models.AnyAsync(m => m.Id != id && m.BrandId == request.BrandId && m.Name == name, ct))
            return AdminResult<AdminModelDto>.Failure("يوجد موديل آخر بنفس الاسم لهذه الماركة.");

        model.BrandId = request.BrandId;
        model.CategoryId = request.CategoryId;
        model.Name = name;
        model.YearFrom = request.YearFrom;
        model.YearTo = request.YearTo;
        model.IsActive = request.IsActive;

        await db.SaveChangesAsync(ct);

        return AdminResult<AdminModelDto>.Success(new AdminModelDto(
            model.Id,
            model.BrandId,
            brand.Name,
            model.CategoryId,
            category.Name,
            model.Name,
            model.YearFrom,
            model.YearTo,
            model.IsActive));
    }

    public async Task<AdminResult<bool>> DeleteModelAsync(int id, CancellationToken ct = default)
    {
        var model = await db.Models.FirstOrDefaultAsync(m => m.Id == id, ct);
        if (model is null)
            return AdminResult<bool>.Failure("الموديل غير موجود.");

        var hasBookings = await db.Bookings.AnyAsync(b => b.VehicleModelId == id, ct);
        if (hasBookings)
            return AdminResult<bool>.Failure("لا يمكن حذف الموديل لوجود حجوزات مرتبطة به. يمكنك تعطيله (IsActive = false) بدلاً من ذلك.");

        db.Models.Remove(model);
        await db.SaveChangesAsync(ct);

        return AdminResult<bool>.Success(true);
    }

    // -------------------------------------------------------------------------
    // Services
    // -------------------------------------------------------------------------
    public Task<List<AdminServiceDto>> GetServicesAsync(CancellationToken ct = default) =>
        db.Services
            .AsNoTracking()
            .OrderBy(s => s.SortOrder)
            .ThenBy(s => s.Name)
            .Select(s => new AdminServiceDto(s.Id, s.Name, s.Description, s.IsActive, s.SortOrder))
            .ToListAsync(ct);

    public async Task<AdminResult<AdminServiceDto>> CreateServiceAsync(
        UpsertServiceRequest request,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return AdminResult<AdminServiceDto>.Failure("اسم الخدمة مطلوب.");

        var service = new Service
        {
            Name = request.Name.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            IsActive = request.IsActive,
            SortOrder = request.SortOrder
        };

        db.Services.Add(service);
        await db.SaveChangesAsync(ct);

        return AdminResult<AdminServiceDto>.Success(
            new AdminServiceDto(service.Id, service.Name, service.Description, service.IsActive, service.SortOrder));
    }

    public async Task<AdminResult<AdminServiceDto>> UpdateServiceAsync(
        int id,
        UpsertServiceRequest request,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return AdminResult<AdminServiceDto>.Failure("اسم الخدمة مطلوب.");

        var service = await db.Services.FirstOrDefaultAsync(s => s.Id == id, ct);
        if (service is null)
            return AdminResult<AdminServiceDto>.Failure("الخدمة غير موجودة.");

        service.Name = request.Name.Trim();
        service.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        service.IsActive = request.IsActive;
        service.SortOrder = request.SortOrder;

        await db.SaveChangesAsync(ct);

        return AdminResult<AdminServiceDto>.Success(
            new AdminServiceDto(service.Id, service.Name, service.Description, service.IsActive, service.SortOrder));
    }

    public async Task<AdminResult<bool>> DeleteServiceAsync(int id, CancellationToken ct = default)
    {
        var service = await db.Services.FirstOrDefaultAsync(s => s.Id == id, ct);
        if (service is null)
            return AdminResult<bool>.Failure("الخدمة غير موجودة.");

        var prices = await db.ServicePrices.Where(sp => sp.ServiceId == id).ToListAsync(ct);
        var overrides = await db.ModelPriceOverrides.Where(o => o.ServiceId == id).ToListAsync(ct);

        db.ServicePrices.RemoveRange(prices);
        db.ModelPriceOverrides.RemoveRange(overrides);
        db.Services.Remove(service);

        await db.SaveChangesAsync(ct);
        return AdminResult<bool>.Success(true);
    }

    // -------------------------------------------------------------------------
    // Service Prices & Bulk Adjustment
    // -------------------------------------------------------------------------
    public async Task<List<AdminServicePriceDto>> GetServicePricesAsync(
        int? serviceId = null,
        int? categoryId = null,
        CancellationToken ct = default)
    {
        var query = db.ServicePrices.AsNoTracking().AsQueryable();

        if (serviceId.HasValue)
            query = query.Where(sp => sp.ServiceId == serviceId.Value);

        if (categoryId.HasValue)
            query = query.Where(sp => sp.CategoryId == categoryId.Value);

        return await query
            .OrderBy(sp => sp.Service.SortOrder)
            .ThenBy(sp => sp.Category.SortOrder)
            .Select(sp => new AdminServicePriceDto(
                sp.Id,
                sp.ServiceId,
                sp.Service.Name,
                sp.CategoryId,
                sp.Category.Name,
                sp.Price,
                sp.DurationMinutes))
            .ToListAsync(ct);
    }

    public async Task<AdminResult<AdminServicePriceDto>> UpsertServicePriceAsync(
        UpsertServicePriceRequest request,
        CancellationToken ct = default)
    {
        if (request.Price < 0)
            return AdminResult<AdminServicePriceDto>.Failure("السعر يجب أن يكون رقمًا موجبًا.");

        if (request.DurationMinutes <= 0)
            return AdminResult<AdminServicePriceDto>.Failure("مدة الخدمة يجب أن تكون أكبر من صفر.");

        var service = await db.Services.FirstOrDefaultAsync(s => s.Id == request.ServiceId, ct);
        if (service is null)
            return AdminResult<AdminServicePriceDto>.Failure("الخدمة غير موجودة.");

        var category = await db.Categories.FirstOrDefaultAsync(c => c.Id == request.CategoryId, ct);
        if (category is null)
            return AdminResult<AdminServicePriceDto>.Failure("الفئة غير موجودة.");

        var existing = await db.ServicePrices
            .FirstOrDefaultAsync(sp => sp.ServiceId == request.ServiceId && sp.CategoryId == request.CategoryId, ct);

        if (existing is null)
        {
            existing = new ServicePrice
            {
                ServiceId = request.ServiceId,
                CategoryId = request.CategoryId,
                Price = request.Price,
                DurationMinutes = request.DurationMinutes
            };
            db.ServicePrices.Add(existing);
        }
        else
        {
            existing.Price = request.Price;
            existing.DurationMinutes = request.DurationMinutes;
        }

        await db.SaveChangesAsync(ct);

        return AdminResult<AdminServicePriceDto>.Success(new AdminServicePriceDto(
            existing.Id,
            existing.ServiceId,
            service.Name,
            existing.CategoryId,
            category.Name,
            existing.Price,
            existing.DurationMinutes));
    }

    public async Task<AdminResult<bool>> DeleteServicePriceAsync(int id, CancellationToken ct = default)
    {
        var existing = await db.ServicePrices.FirstOrDefaultAsync(sp => sp.Id == id, ct);
        if (existing is null)
            return AdminResult<bool>.Failure("التسعيرة غير موجودة.");

        db.ServicePrices.Remove(existing);
        await db.SaveChangesAsync(ct);
        return AdminResult<bool>.Success(true);
    }

    public async Task<AdminResult<int>> BulkAdjustPricesAsync(
        BulkAdjustPricesRequest request,
        CancellationToken ct = default)
    {
        if (request.Percentage <= -100m || request.Percentage == 0m)
            return AdminResult<int>.Failure("النسبة المئوية غير صالحة.");

        var factor = 1m + (request.Percentage / 100m);

        var query = db.ServicePrices.AsQueryable();
        if (request.CategoryId.HasValue)
            query = query.Where(sp => sp.CategoryId == request.CategoryId.Value);

        var prices = await query.ToListAsync(ct);
        foreach (var sp in prices)
        {
            sp.Price = Math.Round(sp.Price * factor, 2);
        }

        await db.SaveChangesAsync(ct);
        return AdminResult<int>.Success(prices.Count);
    }

    // -------------------------------------------------------------------------
    // Model Price Overrides
    // -------------------------------------------------------------------------
    public async Task<List<AdminModelPriceOverrideDto>> GetModelOverridesAsync(
        int? modelId = null,
        CancellationToken ct = default)
    {
        var query = db.ModelPriceOverrides.AsNoTracking().AsQueryable();
        if (modelId.HasValue)
            query = query.Where(o => o.VehicleModelId == modelId.Value);

        var overrides = await query.ToListAsync(ct);
        var serviceIds = overrides.Select(o => o.ServiceId).Distinct().ToList();
        var modelIds = overrides.Select(o => o.VehicleModelId).Distinct().ToList();

        var services = await db.Services
            .AsNoTracking()
            .Where(s => serviceIds.Contains(s.Id))
            .ToDictionaryAsync(s => s.Id, s => s.Name, ct);

        var models = await db.Models
            .AsNoTracking()
            .Where(m => modelIds.Contains(m.Id))
            .Select(m => new { m.Id, m.Name, BrandName = m.Brand.Name })
            .ToDictionaryAsync(m => m.Id, ct);

        return overrides.Select(o =>
        {
            var serviceName = services.GetValueOrDefault(o.ServiceId, "");
            var modelInfo = models.GetValueOrDefault(o.VehicleModelId);
            return new AdminModelPriceOverrideDto(
                o.Id,
                o.ServiceId,
                serviceName,
                o.VehicleModelId,
                modelInfo?.BrandName ?? "",
                modelInfo?.Name ?? "",
                o.Price,
                o.DurationMinutes,
                o.IsAvailable);
        }).ToList();
    }

    public async Task<AdminResult<AdminModelPriceOverrideDto>> UpsertModelOverrideAsync(
        UpsertModelPriceOverrideRequest request,
        CancellationToken ct = default)
    {
        if (request.Price < 0)
            return AdminResult<AdminModelPriceOverrideDto>.Failure("السعر يجب أن يكون رقمًا موجبًا.");

        if (request.DurationMinutes <= 0)
            return AdminResult<AdminModelPriceOverrideDto>.Failure("مدة الخدمة يجب أن تكون أكبر من صفر.");

        var service = await db.Services.FirstOrDefaultAsync(s => s.Id == request.ServiceId, ct);
        if (service is null)
            return AdminResult<AdminModelPriceOverrideDto>.Failure("الخدمة غير موجودة.");

        var model = await db.Models
            .Include(m => m.Brand)
            .FirstOrDefaultAsync(m => m.Id == request.VehicleModelId, ct);

        if (model is null)
            return AdminResult<AdminModelPriceOverrideDto>.Failure("الموديل غير موجود.");

        var existing = await db.ModelPriceOverrides
            .FirstOrDefaultAsync(o => o.ServiceId == request.ServiceId && o.VehicleModelId == request.VehicleModelId, ct);

        if (existing is null)
        {
            existing = new ModelPriceOverride
            {
                ServiceId = request.ServiceId,
                VehicleModelId = request.VehicleModelId,
                Price = request.Price,
                DurationMinutes = request.DurationMinutes,
                IsAvailable = request.IsAvailable
            };
            db.ModelPriceOverrides.Add(existing);
        }
        else
        {
            existing.Price = request.Price;
            existing.DurationMinutes = request.DurationMinutes;
            existing.IsAvailable = request.IsAvailable;
        }

        await db.SaveChangesAsync(ct);

        return AdminResult<AdminModelPriceOverrideDto>.Success(new AdminModelPriceOverrideDto(
            existing.Id,
            existing.ServiceId,
            service.Name,
            existing.VehicleModelId,
            model.Brand.Name,
            model.Name,
            existing.Price,
            existing.DurationMinutes,
            existing.IsAvailable));
    }

    public async Task<AdminResult<bool>> DeleteModelOverrideAsync(int id, CancellationToken ct = default)
    {
        var existing = await db.ModelPriceOverrides.FirstOrDefaultAsync(o => o.Id == id, ct);
        if (existing is null)
            return AdminResult<bool>.Failure("الاستثناء غير موجود.");

        db.ModelPriceOverrides.Remove(existing);
        await db.SaveChangesAsync(ct);
        return AdminResult<bool>.Success(true);
    }

    // -------------------------------------------------------------------------
    // Working Hours
    // -------------------------------------------------------------------------
    public async Task<List<AdminWorkingHourDto>> GetWorkingHoursAsync(CancellationToken ct = default)
    {
        var hours = await db.WorkingHours
            .AsNoTracking()
            .OrderBy(w => w.Id)
            .ToListAsync(ct);

        return hours.Select(w => new AdminWorkingHourDto(
            w.Id,
            w.Day,
            GetArabicDayName(w.Day),
            w.OpenTime,
            w.CloseTime,
            w.IsClosed)).ToList();
    }

    public async Task<AdminResult<List<AdminWorkingHourDto>>> UpdateWorkingHoursAsync(
        List<UpdateWorkingHourItemRequest> requests,
        CancellationToken ct = default)
    {
        if (requests is null || requests.Count == 0)
            return AdminResult<List<AdminWorkingHourDto>>.Failure("بيانات ساعات العمل مطلوبة.");

        var existingHours = await db.WorkingHours.ToListAsync(ct);

        foreach (var req in requests)
        {
            if (!req.IsClosed && req.CloseTime <= req.OpenTime)
                return AdminResult<List<AdminWorkingHourDto>>.Failure($"وقت الإغلاق يجب أن يكون بعد وقت الفتح ليوم {GetArabicDayName(req.Day)}.");

            var row = existingHours.FirstOrDefault(w => w.Day == req.Day);
            if (row is not null)
            {
                row.OpenTime = req.OpenTime;
                row.CloseTime = req.CloseTime;
                row.IsClosed = req.IsClosed;
            }
        }

        await db.SaveChangesAsync(ct);
        var updated = await GetWorkingHoursAsync(ct);
        return AdminResult<List<AdminWorkingHourDto>>.Success(updated);
    }

    // -------------------------------------------------------------------------
    // Holidays
    // -------------------------------------------------------------------------
    public Task<List<AdminHolidayDto>> GetHolidaysAsync(CancellationToken ct = default) =>
        db.Holidays
            .AsNoTracking()
            .OrderBy(h => h.Date)
            .Select(h => new AdminHolidayDto(h.Id, h.Date, h.Reason))
            .ToListAsync(ct);

    public async Task<AdminResult<AdminHolidayDto>> CreateHolidayAsync(
        CreateHolidayRequest request,
        CancellationToken ct = default)
    {
        if (request.Date == default)
            return AdminResult<AdminHolidayDto>.Failure("تاريخ الإجازة مطلوب.");

        if (await db.Holidays.AnyAsync(h => h.Date == request.Date, ct))
            return AdminResult<AdminHolidayDto>.Failure("هذا التاريخ مسجل كإجازة بالفعل.");

        var holiday = new Holiday
        {
            Date = request.Date,
            Reason = string.IsNullOrWhiteSpace(request.Reason) ? null : request.Reason.Trim()
        };

        db.Holidays.Add(holiday);
        await db.SaveChangesAsync(ct);

        return AdminResult<AdminHolidayDto>.Success(new AdminHolidayDto(holiday.Id, holiday.Date, holiday.Reason));
    }

    public async Task<AdminResult<bool>> DeleteHolidayAsync(int id, CancellationToken ct = default)
    {
        var holiday = await db.Holidays.FirstOrDefaultAsync(h => h.Id == id, ct);
        if (holiday is null)
            return AdminResult<bool>.Failure("الإجازة غير موجودة.");

        db.Holidays.Remove(holiday);
        await db.SaveChangesAsync(ct);
        return AdminResult<bool>.Success(true);
    }

    // -------------------------------------------------------------------------
    // Shop Settings
    // -------------------------------------------------------------------------
    public async Task<AdminShopSettingDto> GetShopSettingsAsync(CancellationToken ct = default)
    {
        var setting = await db.Settings.AsNoTracking().FirstOrDefaultAsync(ct) ?? new ShopSetting { Id = 1 };
        return new AdminShopSettingDto(
            setting.Id,
            setting.MaxConcurrentBookings,
            setting.SlotIntervalMinutes,
            setting.MinHoursBeforeBooking,
            setting.MaxDaysAhead);
    }

    public async Task<AdminResult<AdminShopSettingDto>> UpdateShopSettingsAsync(
        UpdateShopSettingRequest request,
        CancellationToken ct = default)
    {
        if (request.MaxConcurrentBookings < 1)
            return AdminResult<AdminShopSettingDto>.Failure("عدد الحجوزات المتزامنة يجب أن يكون 1 على الأقل.");

        if (request.SlotIntervalMinutes < 10)
            return AdminResult<AdminShopSettingDto>.Failure("فاصل المواعيد يجب أن يكون 10 دقائق على الأقل.");

        if (request.MinHoursBeforeBooking < 0)
            return AdminResult<AdminShopSettingDto>.Failure("الحد الأدنى للساعات قبل الحجز غير صالح.");

        if (request.MaxDaysAhead < 1)
            return AdminResult<AdminShopSettingDto>.Failure("عدد الأيام المتاحة للحجز مقدمًا يجب أن يكون 1 على الأقل.");

        var setting = await db.Settings.FirstOrDefaultAsync(ct);
        if (setting is null)
        {
            setting = new ShopSetting();
            db.Settings.Add(setting);
        }

        setting.MaxConcurrentBookings = request.MaxConcurrentBookings;
        setting.SlotIntervalMinutes = request.SlotIntervalMinutes;
        setting.MinHoursBeforeBooking = request.MinHoursBeforeBooking;
        setting.MaxDaysAhead = request.MaxDaysAhead;

        await db.SaveChangesAsync(ct);

        return AdminResult<AdminShopSettingDto>.Success(new AdminShopSettingDto(
            setting.Id,
            setting.MaxConcurrentBookings,
            setting.SlotIntervalMinutes,
            setting.MinHoursBeforeBooking,
            setting.MaxDaysAhead));
    }

    private static string GetArabicDayName(DayOfWeek day) => day switch
    {
        DayOfWeek.Saturday => "السبت",
        DayOfWeek.Sunday => "الأحد",
        DayOfWeek.Monday => "الاثنين",
        DayOfWeek.Tuesday => "الثلاثاء",
        DayOfWeek.Wednesday => "الأربعاء",
        DayOfWeek.Thursday => "الخميس",
        DayOfWeek.Friday => "الجمعة",
        _ => day.ToString()
    };

    private static BookingDetailsDto MapBookingToDto(Booking booking)
    {
        var startCairo = EgyptTime.ToCairo(booking.StartAt);
        var endCairo = EgyptTime.ToCairo(booking.EndAt);
        var date = DateOnly.FromDateTime(startCairo);
        var startTime = TimeOnly.FromDateTime(startCairo);
        var endTime = TimeOnly.FromDateTime(endCairo);

        var statusArabic = booking.Status switch
        {
            BookingStatus.Pending => "قيد الانتظار",
            BookingStatus.Confirmed => "مؤكد",
            BookingStatus.Completed => "مكتمل",
            BookingStatus.Cancelled => "ملغي",
            BookingStatus.NoShow => "لم يحضر",
            _ => booking.Status.ToString()
        };

        var servicesText = string.Join("، ", booking.Items.Select(i => i.ServiceName));
        var whatsappMessage =
            $"تأكيد حجز خدمات السيارات\n" +
            $"رقم الحجز: {booking.Reference}\n" +
            $"السيارة: {booking.VehicleModel.Brand.Name} - {booking.VehicleModel.Name}\n" +
            $"الخدمات: {servicesText}\n" +
            $"الموعد: {date:yyyy-MM-dd} الساعة {startTime:HH:mm}\n" +
            $"الإجمالي: {booking.TotalPrice:0.##} ج.م";

        var intlPhone = booking.Customer.Phone.StartsWith('0')
            ? "2" + booking.Customer.Phone
            : booking.Customer.Phone;

        var whatsappUrl = $"https://wa.me/{intlPhone}?text={Uri.EscapeDataString(whatsappMessage)}";

        return new BookingDetailsDto(
            booking.Id,
            booking.Reference,
            booking.Status,
            statusArabic,
            booking.Customer.FullName,
            booking.Customer.Phone,
            booking.Customer.Email,
            booking.VehicleModelId,
            booking.VehicleModel.Brand.Name,
            booking.VehicleModel.Name,
            booking.VehicleModel.Category.Name,
            booking.ModelYear,
            booking.PlateNumber,
            date,
            startTime,
            endTime,
            booking.StartAt,
            booking.EndAt,
            booking.TotalPrice,
            booking.TotalDurationMinutes,
            booking.CustomerNotes,
            booking.CreatedAt,
            booking.Items.Select(i => new BookingItemDto(
                i.ServiceId,
                i.ServiceName,
                i.Price,
                i.DurationMinutes)).ToList(),
            whatsappUrl);
    }
}

