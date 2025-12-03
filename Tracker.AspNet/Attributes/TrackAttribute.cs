using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Immutable;
using Tracker.AspNet.Models;
using Tracker.AspNet.Services.Contracts;

namespace Tracker.AspNet.Attributes;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class TrackAttribute : Attribute, IAsyncActionFilter
{
    public TrackAttribute()
    { }

    public TrackAttribute(params string[] tables)
    {
        ArgumentNullException.ThrowIfNull(tables, nameof(tables));
        Tables = [.. tables];
    }

    public ImmutableArray<string> Tables { get; } = [];

    public async Task OnActionExecutionAsync(ActionExecutingContext execContext, ActionExecutionDelegate next)
    {
        static ImmutableGlobalOptions OptionsProvider(HttpContext ctx) => ctx.RequestServices.GetRequiredService<ImmutableGlobalOptions>();

        var httpCtx = execContext.HttpContext;

        var requestFilter = httpCtx.RequestServices.GetRequiredService<IRequestFilter>();
        var shouldProcessRequest = requestFilter.ShouldProcessRequest(httpCtx, OptionsProvider, httpCtx);
        if (!shouldProcessRequest)
        {
            await next();
            return;
        }

        var options = OptionsProvider(httpCtx);
        if (Tables is { Length: > 0 })
        {
            options = options with
            {
                Tables = Tables
            };
        }

        var etagService = execContext.HttpContext.RequestServices.GetRequiredService<IETagService>();
        var token = execContext.HttpContext.RequestAborted;

        var shouldReturnNotModified = await etagService.TrySetETagAsync(httpCtx, options, token);
        if (!shouldReturnNotModified)
        {
            await next();
            return;
        }
    }
}