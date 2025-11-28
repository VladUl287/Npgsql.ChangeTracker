using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql.EFCore.Tracker.AspNet.Models;
using Npgsql.EFCore.Tracker.AspNet.Services.Contracts;
using Npgsql.EFCore.Tracker.AspNet.Utils;
using Npgsql.EFCore.Tracker.Core.Extensions;
using System.Net;
using System.Runtime.CompilerServices;

namespace Npgsql.EFCore.Tracker.AspNet.Middlewares;

public sealed class TrackMiddleware<TContext>(
    RequestDelegate next, IActionsRegistry actionsRegistry, IPathResolver pathResolver) where TContext : DbContext
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (IsMethodMatch(context))
        {
            var path = pathResolver.Resolve(context);
            var descriptor = actionsRegistry.Get(path);

            if (await NotModified(context, descriptor, default))
                return;
        }

        await next(context);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsMethodMatch(HttpContext context)
    {
        return context.Request.Method == HttpMethod.Get.Method;
    }

    private static async ValueTask<bool> NotModified(HttpContext context, ActionDescriptor descriptor, CancellationToken token)
    {
        try
        {
            if (descriptor is null)
            {
                //log
                return false;
            }

            if (context.Response.Headers.Any(c => c.Key.Equals("ETag", StringComparison.OrdinalIgnoreCase)))
            {
                //log
                return false;
            }

            var dbContext = context.RequestServices.GetService<TContext>();
            if (dbContext is null)
            {
                //log
                return false;
            }

            var lastTimestamp = await dbContext.GetLastTimestamp(descriptor.Tables, token);
            if (string.IsNullOrEmpty(lastTimestamp))
            {
                //log
                return false;
            }

            var dateTime = DateTimeOffset.Parse(lastTimestamp);
            var etag = ETagUtils.GenETagTicks(dateTime);

            if (context.Request.Headers["If-None-Match"] == etag)
            {
                //log
                context.Response.StatusCode = (int)HttpStatusCode.NotModified;
                return true;
            }

            //log
            context.Response.Headers["ETag"] = etag;
            return false;
        }
        catch (Exception)
        {
            //log
            return false;
        }
    }
}
