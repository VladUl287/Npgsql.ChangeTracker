using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tracker.Core.Services.Contracts;
using Tracker.SqlServer.Models;
using Tracker.SqlServer.Services;

namespace Tracker.SqlServer.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSqlServerSource(
        this IServiceCollection services, string sourceId, string connectionString, TrackingMode mode = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(sourceId);
        ArgumentException.ThrowIfNullOrEmpty(connectionString);

        Func<IServiceProvider, ISourceOperations> factory = mode switch
        {
            TrackingMode.DbIndexUsageStats => (_) => new SqlServerIndexUsageOperations(sourceId, connectionString),
            TrackingMode.ChangeTracking => (_) => new SqlServerChangeTrackingOperations(sourceId, connectionString),
            _ => throw new InvalidOperationException()
        };

        return services.AddSingleton(factory);
    }

    public static IServiceCollection AddSqlServerSource<TContext>(this IServiceCollection services, TrackingMode mode = default)
         where TContext : DbContext
    {
        return services.AddSingleton<ISourceOperations>((provider) =>
        {
            using var scope = provider.CreateScope();

            using var dbContext = scope.ServiceProvider.GetRequiredService<TContext>();
            var connectionString = dbContext.Database.GetConnectionString() ??
                throw new NullReferenceException($"Connection string is not found for context {typeof(TContext).FullName}.");

            var sourceIdGenerator = scope.ServiceProvider.GetRequiredService<ISourceIdGenerator>();
            var sourceId = sourceIdGenerator.GenerateId<TContext>();

            return mode switch
            {
                TrackingMode.DbIndexUsageStats => new SqlServerIndexUsageOperations(sourceId, connectionString),
                TrackingMode.ChangeTracking => new SqlServerChangeTrackingOperations(sourceId, connectionString),
                _ => throw new InvalidOperationException()
            };
        });
    }

    public static IServiceCollection AddSqlServerSource<TContext>(this IServiceCollection services, string sourceId, TrackingMode mode = default)
         where TContext : DbContext
    {
        ArgumentException.ThrowIfNullOrEmpty(sourceId, nameof(sourceId));

        return services.AddSingleton<ISourceOperations>((provider) =>
        {
            using var scope = provider.CreateScope();

            using var dbContext = scope.ServiceProvider.GetRequiredService<TContext>();
            var connectionString = dbContext.Database.GetConnectionString() ??
                throw new NullReferenceException($"Connection string is not found for context {typeof(TContext).FullName}.");

            return mode switch
            {
                TrackingMode.DbIndexUsageStats => new SqlServerIndexUsageOperations(sourceId, connectionString),
                TrackingMode.ChangeTracking => new SqlServerChangeTrackingOperations(sourceId, connectionString),
                _ => throw new InvalidOperationException()
            };
        });
    }
}
