using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tracker.AspNet.Filters;
using Tracker.AspNet.Models;
using Tracker.AspNet.Services.Contracts;

namespace Tracker.AspNet.Extensions;

public static class MinimalApiExtensions
{
    public static IEndpointConventionBuilder WithTracking(this IEndpointConventionBuilder endpoint)
    {
        return endpoint.AddEndpointFilter<IEndpointConventionBuilder, ETagEndpointFilter>();
    }

    public static IEndpointConventionBuilder WithTracking<TContext>(this IEndpointConventionBuilder endpoint, GlobalOptions options)
        where TContext : DbContext
    {
        return endpoint.AddEndpointFilterFactory((provider, next) =>
        {
            var builder = provider.ApplicationServices.GetRequiredService<IOptionsBuilder<GlobalOptions, ImmutableGlobalOptions>>();
            var etagService = provider.ApplicationServices.GetRequiredService<IETagService>();
            var immutableOptions = builder.Build<TContext>(options);
            var filter = new ETagEndpointFilter(etagService, immutableOptions);
            return (context) => filter.InvokeAsync(context, next);
        });
    }

    public static IEndpointConventionBuilder WithTracking<TContext>(this IEndpointConventionBuilder endpoint, Action<GlobalOptions> configure)
        where TContext : DbContext
    {
        var options = new GlobalOptions();
        configure(options);
        return endpoint.WithTracking<TContext>(options);
    }
}
