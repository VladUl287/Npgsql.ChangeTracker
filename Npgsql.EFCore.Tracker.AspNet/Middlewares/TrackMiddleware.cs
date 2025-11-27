using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql.EFCore.Tracker.AspNet.Services.Contracts;
using Npgsql.EFCore.Tracker.Core.Extensions;

namespace Npgsql.EFCore.Tracker.AspNet.Middlewares;

public sealed class TrackMiddleware<TContext>(RequestDelegate next, IActionsRegistry registry)
    where TContext : DbContext
{
    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.GetEncodedPathAndQuery();
        var descriptor = registry.Get(path);
        if (descriptor is not null)
        {
            var dbContext = context.RequestServices.GetService<TContext>();

            if (dbContext is not null)
            {
                var lastTimestamp = await dbContext.GetLastTimestamp(descriptor.Tables, default);
                Console.WriteLine(lastTimestamp);
            }
        }

        await next(context);
    }
}
