using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using System.Runtime.CompilerServices;
using Tracker.AspNet.Models;
using Tracker.AspNet.Services.Contracts;

namespace Tracker.AspNet.Attributes;

public abstract class TrackAttributeBase : Attribute, IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext execContext, ActionExecutionDelegate next)
    {
        var options = GetOrSetOptions(execContext);

        var httpCtx = execContext.HttpContext;
        if (RequestValid(httpCtx, options) && await NotModified(httpCtx, options))
            return;

        await next();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool RequestValid(HttpContext httpCtx, ImmutableGlobalOptions options) =>
        httpCtx.RequestServices
            .GetRequiredService<IRequestFilter>()
            .ShouldProcessRequest(httpCtx, options);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Task<bool> NotModified(HttpContext httpCtx, ImmutableGlobalOptions options) =>
        httpCtx.RequestServices
            .GetRequiredService<IETagService>()
            .TrySetETagAsync(httpCtx, options, httpCtx.RequestAborted);

    protected abstract ImmutableGlobalOptions GetOrSetOptions(ActionExecutingContext execContext);
}
