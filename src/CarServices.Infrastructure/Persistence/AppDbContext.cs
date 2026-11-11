using System.Data;
using CarServices.Application.Common.Interfaces;
using CarServices.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CarServices.Infrastructure.Persistence
{
    public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options), IAppDbContext
    {
        public async Task<TResult> ExecuteInSerializableTransactionAsync<TResult>(
            Func<CancellationToken, Task<TResult>> operation,
            CancellationToken cancellationToken = default)
        {
            var strategy = Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                await using var tx = await Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
                var result = await operation(cancellationToken);
                await tx.CommitAsync(cancellationToken);
                return result;
            });
        }
        public DbSet<VehicleBrand> Brands => Set<VehicleBrand>();
        public DbSet<VehicleCategory> Categories => Set<VehicleCategory>();
        public DbSet<VehicleModel> Models => Set<VehicleModel>();
        public DbSet<Service> Services => Set<Service>();
        public DbSet<ServicePrice> ServicePrices => Set<ServicePrice>();
        public DbSet<ModelPriceOverride> ModelPriceOverrides => Set<ModelPriceOverride>();
        public DbSet<Customer> Customers => Set<Customer>();
        public DbSet<Booking> Bookings => Set<Booking>();
        public DbSet<BookingItem> BookingItems => Set<BookingItem>();
        public DbSet<WorkingHour> WorkingHours => Set<WorkingHour>();
        public DbSet<Holiday> Holidays => Set<Holiday>();
        public DbSet<ShopSetting> Settings => Set<ShopSetting>();

        protected override void OnModelCreating(ModelBuilder b)
        {
            b.Entity<VehicleBrand>().HasIndex(x => x.Name).IsUnique();
            b.Entity<VehicleCategory>().HasIndex(x => x.Name).IsUnique();

            b.Entity<VehicleModel>(e =>
            {
                e.HasIndex(x => new { x.BrandId, x.Name }).IsUnique();
                e.HasOne(x => x.Brand).WithMany(x => x.Models).HasForeignKey(x => x.BrandId);
                e.HasOne(x => x.Category).WithMany(x => x.Models).HasForeignKey(x => x.CategoryId);
            });

            b.Entity<ServicePrice>(e =>
            {
                e.HasIndex(x => new { x.ServiceId, x.CategoryId }).IsUnique();
                e.Property(x => x.Price).HasPrecision(10, 2);
            });

            b.Entity<ModelPriceOverride>(e =>
            {
                e.HasIndex(x => new { x.ServiceId, x.VehicleModelId }).IsUnique();
                e.Property(x => x.Price).HasPrecision(10, 2);
            });

            b.Entity<Customer>().HasIndex(x => x.Phone);

            b.Entity<Booking>(e =>
            {
                e.HasIndex(x => x.Reference).IsUnique();
                e.HasIndex(x => new { x.StartAt, x.EndAt });      // للبحث عن التعارضات
                e.HasIndex(x => x.Status);
                e.Property(x => x.TotalPrice).HasPrecision(10, 2);
                e.Property(x => x.Status).HasConversion<int>();
            });

            b.Entity<BookingItem>(e =>
            {
                e.Property(x => x.Price).HasPrecision(10, 2);
                e.HasOne(x => x.Booking).WithMany(x => x.Items).HasForeignKey(x => x.BookingId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            b.Entity<WorkingHour>().HasIndex(x => x.Day).IsUnique();
            b.Entity<Holiday>().HasIndex(x => x.Date).IsUnique();

            AppDbContextSeed.Seed(b);
        }
    }
}
