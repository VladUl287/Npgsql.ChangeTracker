using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Tracker.AspNet.Extensions;
using Tracker.AspNet.Services.Contracts;

namespace Tracker.AspNet.Attributes;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class TrackAttribute : Attribute, IAsyncActionFilter
{
    public TrackAttribute()
    {}

    public TrackAttribute(string[] tables)
    {
        ArgumentNullException.ThrowIfNull(tables, nameof(tables));
        Tables = tables;
    }

    public string[] Tables { get; } = [];
    public bool IsGlobalTrack => Tables is null or { Length: 0 };

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (!context.HttpContext.IsGetRequest())
        {
            await next();
            return;
        }

        var etagService = context.HttpContext.RequestServices.GetRequiredService<IETagService>();
        var token = context.HttpContext.RequestAborted;

        var shouldReturnNotModified = IsGlobalTrack
          ? await etagService.TrySetETagAsync(context.HttpContext, token)
          : await etagService.TrySetETagAsync(context.HttpContext, Tables, token);

        if (shouldReturnNotModified)
        {
            context.Result = new StatusCodeResult(StatusCodes.Status304NotModified);
            return;
        }

        await next();
    }
}