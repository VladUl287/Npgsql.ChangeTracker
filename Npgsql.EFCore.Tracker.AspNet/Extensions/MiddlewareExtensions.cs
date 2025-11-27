using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Npgsql.EFCore.Tracker.AspNet.Middlewares;

namespace Npgsql.EFCore.Tracker.AspNet.Extensions;

public static class MiddlewareExtensions
{
    public static IApplicationBuilder UseTracker<TContext>(this IApplicationBuilder builder)
        where TContext : DbContext
    {
        return builder
            .UseMiddleware<TrackMiddleware<TContext>>();
    }
}
