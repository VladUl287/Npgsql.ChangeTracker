using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Tracker.AspNet.Logging;
using Tracker.AspNet.Models;
using Tracker.AspNet.Services.Contracts;

namespace Tracker.AspNet.Services;

/// <summary>
/// Basic implementation of <see cref="IRequestFilter"/> which determines whether ETag generation and comparison are permitted based on context data, 
/// configured options, and HTTP specification compliance requirements.
/// </summary>
public sealed class DefaultRequestFilter(IDirectiveChecker directiveChecker, ILogger<DefaultRequestFilter> logger) : IRequestFilter
{
    public bool RequestValid(HttpContext ctx, ImmutableGlobalOptions opts)
    {
        var traceId = new TraceId(ctx);

        logger.LogFilterStarted(traceId, ctx.Request.Path);
        if (!HttpMethods.IsGet(ctx.Request.Method))
        {
            logger.LogNotGetRequest(ctx.Request.Method, traceId);
            return false;
        }

        if (ctx.Response.Headers.ETag.Count > 0)
        {
            logger.LogEtagHeaderPresented(traceId);
            return false;
        }

        if (directiveChecker.AnyInvalidDirective(ctx.Request.Headers.CacheControl, opts.InvalidRequestDirectives.AsSpan(), out var reqDirective))
        {
            logger.LogRequestNotValidCacheControlDirective(reqDirective, traceId);
            return false;
        }

        if (directiveChecker.AnyInvalidDirective(ctx.Response.Headers.CacheControl, opts.InvalidResponseDirectives.AsSpan(), out var resDirective))
        {
            logger.LogResponseNotValidCacheControlDirective(resDirective, traceId);
            return false;
        }

        if (!opts.Filter(ctx))
        {
            logger.LogFilterRejected(traceId);
            return false;
        }

        logger.LogContextFilterFinished(traceId);
        return true;
    }
}
