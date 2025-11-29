using Microsoft.AspNetCore.Builder;

namespace Tracker.AspNet.Extensions;

public static class MinimalApiExtensions
{
    public static IEndpointConventionBuilder WithTracking(this IEndpointConventionBuilder endpoint, string tables)
    {
        endpoint.WithMetadata(new TrackRouteMetadata(tables));
        return endpoint;
    }
}

public sealed record TrackRouteMetadata(string Tables);
