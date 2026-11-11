using CarServices.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CarServices.Application.Common.Interfaces;

public interface IAppDbContext
{
    DbSet<VehicleBrand> Brands { get; }
    DbSet<VehicleCategory> Categories { get; }
    DbSet<VehicleModel> Models { get; }
    DbSet<Service> Services { get; }
    DbSet<ServicePrice> ServicePrices { get; }
    DbSet<ModelPriceOverride> ModelPriceOverrides { get; }
    DbSet<Customer> Customers { get; }
    DbSet<Booking> Bookings { get; }
    DbSet<BookingItem> BookingItems { get; }
    DbSet<WorkingHour> WorkingHours { get; }
    DbSet<Holiday> Holidays { get; }
    DbSet<ShopSetting> Settings { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    Task<TResult> ExecuteInSerializableTransactionAsync<TResult>(
        Func<CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken = default);
}

