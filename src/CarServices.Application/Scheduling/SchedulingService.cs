using CarServices.Application.Common.Interfaces;
using CarServices.Application.Common.Time;
using CarServices.Application.Pricing;
using CarServices.Domain.Entities;
using CarServices.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CarServices.Application.Scheduling;

public class SchedulingService(IAppDbContext db, IPriceCalculator priceCalculator) : ISchedulingService
{
    public async Task<SlotsQueryResult> GetAvailableSlotsAsync(
        DateOnly date,
        int modelId,
        IReadOnlyCollection<int> serviceIds,
        CancellationToken ct = default)
    {
        // 1. Calculate duration & validate model + services
        var quoteResult = await priceCalculator.CalculateQuoteAsync(modelId, serviceIds, ct);
        if (!quoteResult.IsSuccess || quoteResult.Quote is null)
            return SlotsQueryResult.Failure(quoteResult.ErrorMessage ?? "تعذر حساب مدة الخدمات.");

        var totalDurationMinutes = quoteResult.Quote.TotalDurationMinutes;

        // 2. Load shop settings
        var settings = await db.Settings
            .AsNoTracking()
            .FirstOrDefaultAsync(ct) ?? new ShopSetting();

        var nowCairo = EgyptTime.NowInCairo();
        var todayCairo = DateOnly.FromDateTime(nowCairo);

        if (date < todayCairo)
            return SlotsQueryResult.Failure("لا يمكن الحجز في تاريخ سابق.");

        if (date > todayCairo.AddDays(settings.MaxDaysAhead))
            return SlotsQueryResult.Failure($"لا يمكن الحجز لأكثر من {settings.MaxDaysAhead} يومًا مقدمًا.");

        // 3. Check holiday
        var holiday = await db.Holidays
            .AsNoTracking()
            .FirstOrDefaultAsync(h => h.Date == date, ct);

        if (holiday is not null)
        {
            return SlotsQueryResult.Success(new AvailableSlotsResponse(
                date,
                IsClosed: true,
                ClosedReason: string.IsNullOrWhiteSpace(holiday.Reason) ? "إجازة رسمية" : holiday.Reason,
                TotalDurationMinutes: totalDurationMinutes,
                Slots: []));
        }

        // 4. Check working hours for that day of week
        var workingHour = await db.WorkingHours
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.Day == date.DayOfWeek, ct);

        if (workingHour is null || workingHour.IsClosed || workingHour.CloseTime <= workingHour.OpenTime)
        {
            return SlotsQueryResult.Success(new AvailableSlotsResponse(
                date,
                IsClosed: true,
                ClosedReason: "المركز مغلق في هذا اليوم",
                TotalDurationMinutes: totalDurationMinutes,
                Slots: []));
        }

        var dayOpenUtc = EgyptTime.ToUtc(date, workingHour.OpenTime);
        var dayCloseUtc = EgyptTime.ToUtc(date, workingHour.CloseTime);

        // 5. Fetch active bookings that overlap with the working day
        var existingBookings = await db.Bookings
            .AsNoTracking()
            .Where(b => (b.Status == BookingStatus.Pending || b.Status == BookingStatus.Confirmed)
                        && b.StartAt < dayCloseUtc
                        && b.EndAt > dayOpenUtc)
            .Select(b => new { b.StartAt, b.EndAt })
            .ToListAsync(ct);

        var minAllowedStartUtc = EgyptTime.UtcNow().AddHours(settings.MinHoursBeforeBooking);
        var slotInterval = Math.Max(5, settings.SlotIntervalMinutes);

        var slots = new List<SlotDto>();

        var openMinutes = workingHour.OpenTime.Hour * 60 + workingHour.OpenTime.Minute;
        var closeMinutes = workingHour.CloseTime.Hour * 60 + workingHour.CloseTime.Minute;

        for (var curMinutes = openMinutes; curMinutes + totalDurationMinutes <= closeMinutes; curMinutes += slotInterval)
        {
            var slotTime = new TimeOnly(curMinutes / 60, curMinutes % 60);
            var slotStartUtc = EgyptTime.ToUtc(date, slotTime);
            var slotEndUtc = slotStartUtc.AddMinutes(totalDurationMinutes);

            if (slotStartUtc < minAllowedStartUtc)
                continue;

            var overlappingCount = existingBookings.Count(b =>
                b.StartAt < slotEndUtc && b.EndAt > slotStartUtc);

            var isAvailable = overlappingCount < settings.MaxConcurrentBookings;

            if (isAvailable)
            {
                slots.Add(new SlotDto(
                    slotTime,
                    slotStartUtc,
                    slotEndUtc,
                    IsAvailable: true));
            }
        }

        return SlotsQueryResult.Success(new AvailableSlotsResponse(
            date,
            IsClosed: false,
            ClosedReason: null,
            TotalDurationMinutes: totalDurationMinutes,
            Slots: slots));
    }
}

