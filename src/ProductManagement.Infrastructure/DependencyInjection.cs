using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProductManagement.Application.Abstractions;
using ProductManagement.Infrastructure.Caching;
using ProductManagement.Infrastructure.Inventory;
using ProductManagement.Infrastructure.Persistence;

namespace ProductManagement.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Database")
            ?? throw new InvalidOperationException("Connection string 'Database' is not configured.");

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString)
                   .UseSnakeCaseNamingConvention());

        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());
        services.AddScoped<IInventoryService, InventoryService>();

        // Cache-aside: use Redis when configured, otherwise a no-op cache so the app
        // still runs without a Redis dependency.
        var redisConnection = configuration.GetConnectionString("Redis");
        if (!string.IsNullOrWhiteSpace(redisConnection))
        {
            services.AddStackExchangeRedisCache(options => options.Configuration = redisConnection);
            services.AddScoped<IProductCache, RedisProductCache>();
        }
        else
        {
            services.AddSingleton<IProductCache, NullProductCache>();
        }

        return services;
    }
}
