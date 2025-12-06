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

        if (ShouldProcessRequestAsync(httpCtx, options))
        {
            var cancelToken = httpCtx.RequestAborted;
            if (cancelToken.IsCancellationRequested)
                return;

            if (await ShouldReturnNotModifiedAsync(httpCtx, options, cancelToken))
                return;
        }

        await next();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool ShouldProcessRequestAsync(HttpContext httpCtx, ImmutableGlobalOptions options) =>
        httpCtx.RequestServices
            .GetRequiredService<IRequestFilter>()
            .ShouldProcessRequest(httpCtx, options);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Task<bool> ShouldReturnNotModifiedAsync(HttpContext httpCtx, ImmutableGlobalOptions options, CancellationToken token) =>
        httpCtx.RequestServices
            .GetRequiredService<IETagService>()
            .TrySetETagAsync(httpCtx, options, token);

    protected abstract ImmutableGlobalOptions GetOrSetOptions(ActionExecutingContext execContext);
}
