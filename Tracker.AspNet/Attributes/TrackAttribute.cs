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
        static ImmutableGlobalOptions optionsProvider(HttpContext ctx) => ctx.RequestServices.GetRequiredService<ImmutableGlobalOptions>();

        var context = execContext.HttpContext;

        var requestFilter = context.RequestServices.GetRequiredService<IRequestFilter>();
        var shouldProcessRequest = requestFilter.ShouldProcessRequest(context, optionsProvider, context);
        if (!shouldProcessRequest)
        {
            await next();
            return;
        }

        var options = optionsProvider(context);
        if (Tables is { Length: > 0 })
        {
            options = options with
            {
                Tables = [.. Tables]
            };
        }

        var etagService = execContext.HttpContext.RequestServices.GetRequiredService<IETagService>();
        var token = execContext.HttpContext.RequestAborted;

        var shouldReturnNotModified = await etagService.TrySetETagAsync(execContext.HttpContext, options, token);
        if (shouldReturnNotModified)
        {
            execContext.Result = new StatusCodeResult(StatusCodes.Status304NotModified);
            return;
        }

        await next();
    }
}