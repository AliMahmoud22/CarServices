
using CarServices.Application.Admin;
using CarServices.Application.Bookings;
using CarServices.Application.Catalog;
using CarServices.Application.Pricing;
using CarServices.Application.Scheduling;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace CarServices.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationDI(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        services.AddScoped<ICatalogService, CatalogService>();
        services.AddScoped<IPriceCalculator, PriceCalculator>();
        services.AddScoped<ISchedulingService, SchedulingService>();
        services.AddScoped<IBookingService, BookingService>();
        services.AddScoped<IAdminService, AdminService>();

        return services;
    }
}

