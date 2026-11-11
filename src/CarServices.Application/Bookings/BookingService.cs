using System.Security.Cryptography;
using CarServices.Application.Common.Interfaces;
using CarServices.Application.Common.Time;
using CarServices.Application.Pricing;
using CarServices.Domain.Entities;
using CarServices.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CarServices.Application.Bookings;

public class BookingService(
    IAppDbContext db,
    IPriceCalculator priceCalculator) : IBookingService
{
    public async Task<BookingOperationResult> CreateBookingAsync(
        CreateBookingRequest request,
        CancellationToken ct = default)
    {
        // 1. Calculate frozen quote and validate model + services
        var quoteResult = await priceCalculator.CalculateQuoteAsync(request.ModelId, request.ServiceIds, ct);
        if (!quoteResult.IsSuccess || quoteResult.Quote is null)
            return BookingOperationResult.Failure(quoteResult.ErrorMessage ?? "تعذر حساب تكلفة الخدمات.");

        var quote = quoteResult.Quote;

        // 2. Load shop settings
        var settings = await db.Settings
            .AsNoTracking()
            .FirstOrDefaultAsync(ct) ?? new ShopSetting();

        var todayCairo = EgyptTime.TodayInCairo();
        if (request.Date < todayCairo)
            return BookingOperationResult.Failure("لا يمكن الحجز في تاريخ سابق.");

        if (request.Date > todayCairo.AddDays(settings.MaxDaysAhead))
            return BookingOperationResult.Failure($"لا يمكن الحجز لأكثر من {settings.MaxDaysAhead} يومًا مقدمًا.");

        // 3. Check holiday
        var isHoliday = await db.Holidays
            .AsNoTracking()
            .AnyAsync(h => h.Date == request.Date, ct);

        if (isHoliday)
            return BookingOperationResult.Failure("المركز مغلق في هذا التاريخ بسبب إجازة.");

        // 4. Check working hours
        var workingHour = await db.WorkingHours
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.Day == request.Date.DayOfWeek, ct);

        if (workingHour is null || workingHour.IsClosed || workingHour.CloseTime <= workingHour.OpenTime)
            return BookingOperationResult.Failure("المركز مغلق في هذا اليوم.");

        var startMinutes = request.Time.Hour * 60 + request.Time.Minute;
        var openMinutes = workingHour.OpenTime.Hour * 60 + workingHour.OpenTime.Minute;
        var closeMinutes = workingHour.CloseTime.Hour * 60 + workingHour.CloseTime.Minute;

        if (startMinutes < openMinutes || startMinutes + quote.TotalDurationMinutes > closeMinutes)
            return BookingOperationResult.Failure("الموعد المختار خارج ساعات العمل أو لا يكفي لإتمام الخدمات المختارة قبل الإغلاق.");

        var slotInterval = Math.Max(5, settings.SlotIntervalMinutes);
        if ((startMinutes - openMinutes) % slotInterval != 0)
            return BookingOperationResult.Failure("الموعد المختار غير متوافق مع فترات الحجز المتاحة.");

        var startAtUtc = EgyptTime.ToUtc(request.Date, request.Time);
        var endAtUtc = startAtUtc.AddMinutes(quote.TotalDurationMinutes);

        var minAllowedStartUtc = EgyptTime.UtcNow().AddHours(settings.MinHoursBeforeBooking);
        if (startAtUtc < minAllowedStartUtc)
            return BookingOperationResult.Failure($"يجب الحجز قبل الموعد بـ {settings.MinHoursBeforeBooking} ساعة على الأقل.");

        var normalizedPhone = NormalizeEgyptianPhone(request.CustomerPhone);

        // 5. Serializable transaction to prevent concurrent double-booking on the same slot
        var createdReference = await db.ExecuteInSerializableTransactionAsync<string?>(async token =>
        {
            var overlappingBookingsCount = await db.Bookings
                .Where(b => (b.Status == BookingStatus.Pending || b.Status == BookingStatus.Confirmed)
                            && b.StartAt < endAtUtc
                            && b.EndAt > startAtUtc)
                .CountAsync(token);

            if (overlappingBookingsCount >= settings.MaxConcurrentBookings)
                return null;

            // Find or create customer by phone
            var customer = await db.Customers
                .FirstOrDefaultAsync(c => c.Phone == normalizedPhone, token);

            if (customer is null)
            {
                customer = new Customer
                {
                    FullName = request.CustomerName.Trim(),
                    Phone = normalizedPhone,
                    Email = string.IsNullOrWhiteSpace(request.CustomerEmail) ? null : request.CustomerEmail.Trim(),
                    CreatedAt = DateTime.UtcNow
                };
                db.Customers.Add(customer);
            }
            else
            {
                customer.FullName = request.CustomerName.Trim();
                if (!string.IsNullOrWhiteSpace(request.CustomerEmail))
                    customer.Email = request.CustomerEmail.Trim();
            }

            var reference = await GenerateUniqueReferenceAsync(request.Date, token);

            var booking = new Booking
            {
                Reference = reference,
                Customer = customer,
                VehicleModelId = quote.ModelId,
                ModelYear = request.ModelYear,
                PlateNumber = string.IsNullOrWhiteSpace(request.PlateNumber) ? null : request.PlateNumber.Trim(),
                StartAt = startAtUtc,
                EndAt = endAtUtc,
                TotalPrice = quote.TotalPrice,
                TotalDurationMinutes = quote.TotalDurationMinutes,
                Status = BookingStatus.Pending,
                CustomerNotes = string.IsNullOrWhiteSpace(request.CustomerNotes) ? null : request.CustomerNotes.Trim(),
                CreatedAt = DateTime.UtcNow,
                Items = quote.Items.Select(i => new BookingItem
                {
                    ServiceId = i.ServiceId,
                    ServiceName = i.ServiceName,
                    Price = i.Price,
                    DurationMinutes = i.DurationMinutes
                }).ToList()
            };

            db.Bookings.Add(booking);
            await db.SaveChangesAsync(token);
            return booking.Reference;
        }, ct);

        if (createdReference is null)
            return BookingOperationResult.Failure("عذرًا، هذا الموعد لم يعد متاحًا. يرجى اختيار موعد آخر.");

        var details = await GetBookingByReferenceAsync(createdReference, ct);
        return BookingOperationResult.Success(details!);
    }

    public async Task<BookingDetailsDto?> GetBookingByReferenceAsync(
        string reference,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(reference))
            return null;

        var normalizedRef = reference.Trim().ToUpperInvariant();

        var booking = await db.Bookings
            .AsNoTracking()
            .Include(b => b.Customer)
            .Include(b => b.VehicleModel)
                .ThenInclude(m => m.Brand)
            .Include(b => b.VehicleModel)
                .ThenInclude(m => m.Category)
            .Include(b => b.Items)
            .FirstOrDefaultAsync(b => b.Reference == normalizedRef, ct);

        return booking is null ? null : MapToDetailsDto(booking);
    }

    public async Task<BookingOperationResult> CancelBookingByReferenceAsync(
        string reference,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(reference))
            return BookingOperationResult.Failure("رقم الحجز مطلوب.");

        var normalizedRef = reference.Trim().ToUpperInvariant();

        var booking = await db.Bookings
            .Include(b => b.Customer)
            .Include(b => b.VehicleModel)
                .ThenInclude(m => m.Brand)
            .Include(b => b.VehicleModel)
                .ThenInclude(m => m.Category)
            .Include(b => b.Items)
            .FirstOrDefaultAsync(b => b.Reference == normalizedRef, ct);

        if (booking is null)
            return BookingOperationResult.Failure("الحجز غير موجود.");

        if (booking.Status == BookingStatus.Completed)
            return BookingOperationResult.Failure("لا يمكن إلغاء حجز مكتمل.");

        if (booking.Status == BookingStatus.Cancelled)
            return BookingOperationResult.Failure("هذا الحجز ملغي بالفعل.");

        booking.Status = BookingStatus.Cancelled;
        await db.SaveChangesAsync(ct);

        return BookingOperationResult.Success(MapToDetailsDto(booking));
    }

    public async Task<BookingOperationResult> ChangeStatusAsync(
        int bookingId,
        BookingStatus newStatus,
        string? internalNotes = null,
        CancellationToken ct = default)
    {
        var booking = await db.Bookings
            .Include(b => b.Customer)
            .Include(b => b.VehicleModel)
                .ThenInclude(m => m.Brand)
            .Include(b => b.VehicleModel)
                .ThenInclude(m => m.Category)
            .Include(b => b.Items)
            .FirstOrDefaultAsync(b => b.Id == bookingId, ct);

        if (booking is null)
            return BookingOperationResult.Failure("الحجز غير موجود.");

        booking.Status = newStatus;
        if (internalNotes is not null)
            booking.InternalNotes = internalNotes.Trim();

        await db.SaveChangesAsync(ct);

        return BookingOperationResult.Success(MapToDetailsDto(booking));
    }

    private async Task<string> GenerateUniqueReferenceAsync(DateOnly date, CancellationToken ct)
    {
        const string chars = "23456789ABCDEFGHJKLMNPQRSTUVWXYZ";
        var datePart = date.ToString("yyyyMMdd");

        for (var attempt = 0; attempt < 10; attempt++)
        {
            var suffix = new char[4];
            for (var i = 0; i < suffix.Length; i++)
            {
                suffix[i] = chars[RandomNumberGenerator.GetInt32(chars.Length)];
            }

            var candidate = $"CS-{datePart}-{new string(suffix)}";
            var exists = await db.Bookings.AnyAsync(b => b.Reference == candidate, ct);
            if (!exists)
                return candidate;
        }

        return $"CS-{datePart}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";
    }

    private static string NormalizeEgyptianPhone(string phone)
    {
        var cleaned = phone.Trim().Replace(" ", "").Replace("-", "");
        if (cleaned.StartsWith("+20"))
            cleaned = "0" + cleaned[3..];
        else if (cleaned.StartsWith("0020"))
            cleaned = "0" + cleaned[4..];
        else if (cleaned.Length == 10 && cleaned.StartsWith('1'))
            cleaned = "0" + cleaned;

        return cleaned;
    }

    private static BookingDetailsDto MapToDetailsDto(Booking booking)
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
