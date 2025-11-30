using Microsoft.AspNetCore.Routing;
using System.Reflection;
using Tracker.AspNet.Attributes;
using Tracker.AspNet.Extensions;
using Tracker.AspNet.Models;
using Tracker.AspNet.Services.Contracts;

namespace Tracker.AspNet.Services;

public class DefaultActionsDescriptorProvider(EndpointDataSource endpointRouteBuilder) : IActionsDescriptorProvider
{
    public virtual IEnumerable<ActionDescriptor> GetActionsDescriptors(params Assembly[] assemblies)
    {
        foreach (var endpoint in endpointRouteBuilder.Endpoints)
        {
            if (endpoint is not RouteEndpoint routeEndpoint)
            {
                continue;
            }

            var methods = routeEndpoint.Metadata.GetMetadata<HttpMethodMetadata>()?.HttpMethods ?? [];
            if (methods is not ["GET"])
            {
                continue;
            }

            var controllerTracking = routeEndpoint.Metadata.GetMetadata<TrackAttribute>();
            if (controllerTracking is not null)
            {
                yield return new ActionDescriptor
                {
                    Route = routeEndpoint.RoutePattern.RawText ?? controllerTracking.Route,
                    Tables = controllerTracking.Tables ?? []
                };
            }

            var minimalApiTracking = routeEndpoint.Metadata.GetMetadata<TrackRouteMetadata>();
            if (minimalApiTracking is not null && routeEndpoint is { RoutePattern.RawText: not null })
            {
                yield return new ActionDescriptor
                {
                    Route = routeEndpoint.RoutePattern.RawText,
                    Tables = minimalApiTracking.Tables ?? []
                };
            }
        }
    }
}
