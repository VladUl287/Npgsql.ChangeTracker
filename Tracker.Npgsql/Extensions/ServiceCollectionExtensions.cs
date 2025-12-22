using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Tracker.Core.Services.Contracts;
using Tracker.Npgsql.Services;

namespace Tracker.Npgsql.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddNpgsqlProvider<TContext>(this IServiceCollection services)
         where TContext : DbContext
    {
        return services.AddSingleton<ISourceProvider>((provider) =>
        {
            using var scope = provider.CreateScope();

            using var dbContext = scope.ServiceProvider.GetRequiredService<TContext>();
            var connectionString = dbContext.Database.GetConnectionString() ??
                throw new NullReferenceException($"Connection string is not found for context {typeof(TContext).FullName}.");

            var sourceIdGenerator = scope.ServiceProvider.GetRequiredService<IProviderIdGenerator>();
            var sourceId = sourceIdGenerator.GenerateId<TContext>();

            return new NpgsqlOperations(sourceId, connectionString);
        });
    }

    public static IServiceCollection AddNpgsqlProvider<TContext>(this IServiceCollection services, string sourceId)
         where TContext : DbContext
    {
        ArgumentException.ThrowIfNullOrEmpty(sourceId);

        return services.AddSingleton<ISourceProvider>((provider) =>
        {
            using var scope = provider.CreateScope();

            using var dbContext = scope.ServiceProvider.GetRequiredService<TContext>();
            var connectionString = dbContext.Database.GetConnectionString() ??
                throw new NullReferenceException($"Connection string is not found for context {typeof(TContext).FullName}.");

            return new NpgsqlOperations(sourceId, connectionString);
        });
    }

    public static IServiceCollection AddNpgsqlProvider(this IServiceCollection services, string sourceId, string connectionString)
    {
        ArgumentException.ThrowIfNullOrEmpty(sourceId);
        ArgumentException.ThrowIfNullOrEmpty(connectionString);

        return services.AddSingleton<ISourceProvider>((_) =>
            new NpgsqlOperations(sourceId, connectionString)
        );
    }

    public static IServiceCollection AddNpgsqlSource(this IServiceCollection services, string sourceId, Action<NpgsqlDataSourceBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        ArgumentException.ThrowIfNullOrEmpty(sourceId);

        return services.AddSingleton<ISourceProvider>((_) =>
        {
            var dataSourceBuilder = new NpgsqlDataSourceBuilder();
            configure(dataSourceBuilder);
            var dataSource = dataSourceBuilder.Build();
            return new NpgsqlOperations(sourceId, dataSource);
        });
    }
}
