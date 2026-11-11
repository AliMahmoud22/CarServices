using CarServices.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CarServices.Infrastructure.Persistence;

public static class AppDbContextSeed
{
    public static void Seed(ModelBuilder b)
    {
        // 1. Shop Settings (single row)
        b.Entity<ShopSetting>().HasData(new ShopSetting
        {
            Id = 1,
            MaxConcurrentBookings = 2,
            SlotIntervalMinutes = 30,
            MinHoursBeforeBooking = 2,
            MaxDaysAhead = 30
        });

        // 2. Working Hours (Saturday to Thursday 09:00 - 21:00, Friday closed)
        b.Entity<WorkingHour>().HasData(
            new WorkingHour { Id = 1, Day = DayOfWeek.Saturday, OpenTime = new TimeOnly(9, 0), CloseTime = new TimeOnly(21, 0), IsClosed = false },
            new WorkingHour { Id = 2, Day = DayOfWeek.Sunday, OpenTime = new TimeOnly(9, 0), CloseTime = new TimeOnly(21, 0), IsClosed = false },
            new WorkingHour { Id = 3, Day = DayOfWeek.Monday, OpenTime = new TimeOnly(9, 0), CloseTime = new TimeOnly(21, 0), IsClosed = false },
            new WorkingHour { Id = 4, Day = DayOfWeek.Tuesday, OpenTime = new TimeOnly(9, 0), CloseTime = new TimeOnly(21, 0), IsClosed = false },
            new WorkingHour { Id = 5, Day = DayOfWeek.Wednesday, OpenTime = new TimeOnly(9, 0), CloseTime = new TimeOnly(21, 0), IsClosed = false },
            new WorkingHour { Id = 6, Day = DayOfWeek.Thursday, OpenTime = new TimeOnly(9, 0), CloseTime = new TimeOnly(21, 0), IsClosed = false },
            new WorkingHour { Id = 7, Day = DayOfWeek.Friday, OpenTime = new TimeOnly(9, 0), CloseTime = new TimeOnly(21, 0), IsClosed = true }
        );

        // 3. Vehicle Categories
        b.Entity<VehicleCategory>().HasData(
            new VehicleCategory { Id = 1, Name = "هاتشباك / صغيرة", SortOrder = 1 },
            new VehicleCategory { Id = 2, Name = "سيدان", SortOrder = 2 },
            new VehicleCategory { Id = 3, Name = "SUV / دفع رباعي", SortOrder = 3 },
            new VehicleCategory { Id = 4, Name = "فاخرة", SortOrder = 4 }
        );

        // 4. Vehicle Brands
        b.Entity<VehicleBrand>().HasData(
            new VehicleBrand { Id = 1, Name = "تويوتا (Toyota)", IsActive = true },
            new VehicleBrand { Id = 2, Name = "هيونداي (Hyundai)", IsActive = true },
            new VehicleBrand { Id = 3, Name = "كيا (Kia)", IsActive = true },
            new VehicleBrand { Id = 4, Name = "نيسان (Nissan)", IsActive = true },
            new VehicleBrand { Id = 5, Name = "مرسيدس (Mercedes-Benz)", IsActive = true }
        );

        // 5. Vehicle Models
        b.Entity<VehicleModel>().HasData(
            new VehicleModel { Id = 1, BrandId = 1, CategoryId = 1, Name = "ياريس (Yaris)", YearFrom = 2015, YearTo = 2026, IsActive = true },
            new VehicleModel { Id = 2, BrandId = 1, CategoryId = 2, Name = "كورولا (Corolla)", YearFrom = 2015, YearTo = 2026, IsActive = true },
            new VehicleModel { Id = 3, BrandId = 1, CategoryId = 3, Name = "فورتشنر (Fortuner)", YearFrom = 2016, YearTo = 2026, IsActive = true },
            new VehicleModel { Id = 4, BrandId = 2, CategoryId = 2, Name = "إلنترا (Elantra)", YearFrom = 2015, YearTo = 2026, IsActive = true },
            new VehicleModel { Id = 5, BrandId = 2, CategoryId = 3, Name = "توسان (Tucson)", YearFrom = 2016, YearTo = 2026, IsActive = true },
            new VehicleModel { Id = 6, BrandId = 3, CategoryId = 2, Name = "سيراتو (Cerato)", YearFrom = 2015, YearTo = 2026, IsActive = true },
            new VehicleModel { Id = 7, BrandId = 3, CategoryId = 3, Name = "سبورتاج (Sportage)", YearFrom = 2016, YearTo = 2026, IsActive = true },
            new VehicleModel { Id = 8, BrandId = 4, CategoryId = 2, Name = "صني (Sunny)", YearFrom = 2015, YearTo = 2026, IsActive = true },
            new VehicleModel { Id = 9, BrandId = 5, CategoryId = 4, Name = "C200", YearFrom = 2018, YearTo = 2026, IsActive = true }
        );

        // 6. Services
        b.Entity<Service>().HasData(
            new Service { Id = 1, Name = "تغيير زيت المحرك والفلتر", Description = "تغيير زيت المحرك مع فلتر الزيت وفحص السوائل", IsActive = true, SortOrder = 1 },
            new Service { Id = 2, Name = "فحص كمبيوتر شامل", Description = "فحص جميع حساسات وكنترولات السيارة بالكمبيوتر", IsActive = true, SortOrder = 2 },
            new Service { Id = 3, Name = "صيانة وتغيير تيل الفرامل", Description = "تغيير تيل الفرامل الأمامي أو الخلفي مع تنظيف الطنابير", IsActive = true, SortOrder = 3 },
            new Service { Id = 4, Name = "غسيل وتلميع شامل", Description = "غسيل داخلي وخارجي مع تلميع الهيكل", IsActive = true, SortOrder = 4 }
        );

        // 7. Service Prices per Category
        b.Entity<ServicePrice>().HasData(
            // Service 1: Oil Change
            new ServicePrice { Id = 1, ServiceId = 1, CategoryId = 1, Price = 800m, DurationMinutes = 30 },
            new ServicePrice { Id = 2, ServiceId = 1, CategoryId = 2, Price = 950m, DurationMinutes = 30 },
            new ServicePrice { Id = 3, ServiceId = 1, CategoryId = 3, Price = 1300m, DurationMinutes = 45 },
            new ServicePrice { Id = 4, ServiceId = 1, CategoryId = 4, Price = 2200m, DurationMinutes = 45 },

            // Service 2: Computer Diagnostics
            new ServicePrice { Id = 5, ServiceId = 2, CategoryId = 1, Price = 300m, DurationMinutes = 30 },
            new ServicePrice { Id = 6, ServiceId = 2, CategoryId = 2, Price = 300m, DurationMinutes = 30 },
            new ServicePrice { Id = 7, ServiceId = 2, CategoryId = 3, Price = 400m, DurationMinutes = 30 },
            new ServicePrice { Id = 8, ServiceId = 2, CategoryId = 4, Price = 600m, DurationMinutes = 45 },

            // Service 3: Brake Pads
            new ServicePrice { Id = 9, ServiceId = 3, CategoryId = 1, Price = 600m, DurationMinutes = 45 },
            new ServicePrice { Id = 10, ServiceId = 3, CategoryId = 2, Price = 750m, DurationMinutes = 45 },
            new ServicePrice { Id = 11, ServiceId = 3, CategoryId = 3, Price = 1000m, DurationMinutes = 60 },
            new ServicePrice { Id = 12, ServiceId = 3, CategoryId = 4, Price = 1800m, DurationMinutes = 60 },

            // Service 4: Full Wash & Polish
            new ServicePrice { Id = 13, ServiceId = 4, CategoryId = 1, Price = 400m, DurationMinutes = 60 },
            new ServicePrice { Id = 14, ServiceId = 4, CategoryId = 2, Price = 500m, DurationMinutes = 60 },
            new ServicePrice { Id = 15, ServiceId = 4, CategoryId = 3, Price = 700m, DurationMinutes = 90 },
            new ServicePrice { Id = 16, ServiceId = 4, CategoryId = 4, Price = 1000m, DurationMinutes = 90 }
        );
    }
}

