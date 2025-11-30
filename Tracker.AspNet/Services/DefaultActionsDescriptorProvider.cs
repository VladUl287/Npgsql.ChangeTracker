using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Reflection;
using Tracker.AspNet.Attributes;
using Tracker.AspNet.Extensions;
using Tracker.AspNet.Models;
using Tracker.AspNet.Services.Contracts;

namespace Tracker.AspNet.Services;

public class DefaultActionsDescriptorProvider<TContext>(
    EndpointDataSource endpDataSrc, IServiceScopeFactory scopeFactory, ILogger<DefaultActionsDescriptorProvider<TContext>> logger) : IActionsDescriptorProvider
    where TContext : DbContext
{
    public virtual IEnumerable<ActionDescriptor> GetActionsDescriptors(params Assembly[] assemblies)
    {
        using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TContext>();

        foreach (var endpoint in endpDataSrc.Endpoints)
        {
            if (endpoint is not RouteEndpoint routeEndpoint)
                continue;

            var methods = routeEndpoint.Metadata.GetMetadata<HttpMethodMetadata>()?.HttpMethods ?? [];
            if (!methods.Any(c => c.Equals("GET", StringComparison.OrdinalIgnoreCase)))
                continue;

            var controllerTracking = routeEndpoint.Metadata.GetMetadata<TrackAttribute>();
            if (controllerTracking is not null)
            {
                var route = routeEndpoint.RoutePattern.RawText ?? controllerTracking.Route ??
                    throw new NullReferenceException($"Route for '{routeEndpoint.DisplayName}' not found.");

                string[] tables = ResolveTables(dbContext, controllerTracking.Tables, controllerTracking.Entities);
                yield return new ActionDescriptor
                {
                    Route = route,
                    Tables = tables
                };
            }

            var trackRouteMetadata = routeEndpoint.Metadata.GetMetadata<TrackRouteMetadata>();
            if (trackRouteMetadata is not null && routeEndpoint is { RoutePattern.RawText: not null })
            {
                var route = routeEndpoint.RoutePattern.RawText ?? trackRouteMetadata.Route ??
                    throw new NullReferenceException($"Route for '{routeEndpoint.DisplayName}' not found.");

                string[] tables = ResolveTables(dbContext,trackRouteMetadata.Tables, trackRouteMetadata.Entities);
                yield return new ActionDescriptor
                {
                    Route = route,
                    Tables = tables
                };
            }
        }
    }

    private string[] ResolveTables(TContext context, string[]? tables, Type[]? entities)
    {
        var result = new HashSet<string>();

        foreach (var table in tables ?? [])
        {
            var added = result.Add(table);
            if (!added)
            {
                //log warning
            }
        }

        var contextModel = context.Model;
        foreach (var entity in entities ?? [])
        {
            var entityType = contextModel.FindEntityType(entity) ??
                throw new NullReferenceException($"Table entity type not found for type {entity.FullName}");

            var tableName = entityType.GetSchemaQualifiedTableName() ??
                throw new NullReferenceException($"Table entity type not found for type {entity.FullName}");

            var added = result.Add(tableName);
            if (!added)
            {
                //log warning
            }
        }

        return result.ToArray();
    }
}
