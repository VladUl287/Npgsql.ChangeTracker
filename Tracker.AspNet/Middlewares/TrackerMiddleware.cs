using Tracker.AspNet.Models;
using Microsoft.AspNetCore.Http;
using Tracker.AspNet.Services.Contracts;

namespace Tracker.AspNet.Middlewares;

public sealed class TrackerMiddleware(
    RequestDelegate next, IRequestFilter requestFilter, IETagService eTagService,
    ImmutableGlobalOptions opts)
{
    public async Task InvokeAsync(HttpContext ctx)
    {
        var shouldProcessRequest = requestFilter.ShouldProcessRequest(ctx, opts);
        if (!shouldProcessRequest)
        {
            await next(ctx);
            return;
        }

        var token = ctx.RequestAborted;
        if (token.IsCancellationRequested)
            return;

        var shouldReturnNotModified = await eTagService.TrySetETagAsync(ctx, opts, token);
        if (!shouldReturnNotModified)
        {
            await next(ctx);
            return;
        }
    }
}