using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Tracker.AspNet.Models;
using Tracker.AspNet.Services.Contracts;

namespace Tracker.AspNet.Attributes;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class TrackAttribute() : Attribute, IAsyncActionFilter
{
    public TrackAttribute(string[] tables) : this()
    {
        ArgumentNullException.ThrowIfNull(tables, nameof(tables));
        Tables = tables;
    }

    public string[] Tables { get; } = [];

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var httpContext = context.HttpContext;

        var requestFilter = httpContext.RequestServices.GetRequiredService<IRequestFilter>();

        static GlobalOptions optionsProvider(HttpContext ctx) => ctx.RequestServices.GetRequiredService<GlobalOptions>();

        var shouldProcessRequest = requestFilter.ShouldProcessRequest(httpContext, optionsProvider, httpContext);
        if (!shouldProcessRequest)
        {
            await next();
            return;
        }

        var options = optionsProvider(httpContext);
        options = options.Copy();
        options.Tables = Tables;

        var etagService = context.HttpContext.RequestServices.GetRequiredService<IETagService>();
        var token = context.HttpContext.RequestAborted;

        var shouldReturnNotModified = await etagService.TrySetETagAsync(context.HttpContext, options, token);
        if (shouldReturnNotModified)
        {
            context.Result = new StatusCodeResult(StatusCodes.Status304NotModified);
            return;
        }

        await next();
    }
}