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
    public bool RequestValid(HttpContext ctx, ImmutableGlobalOptions options)
    {
        logger.LogFilterStarted(ctx.TraceIdentifier, ctx.Request.Path);

        if (!HttpMethods.IsGet(ctx.Request.Method))
        {
            logger.LogNotGetRequest(ctx.Request.Method, ctx.TraceIdentifier);
            return false;
        }

        if (ctx.Response.Headers.ETag.Count > 0)
        {
            logger.LogEtagHeaderPresented(ctx.TraceIdentifier);
            return false;
        }

        var defaultRequestDirectives = directiveChecker.DefaultInvalidRequestDirectives;
        if (directiveChecker.AnyInvalidDirective(ctx.Request.Headers.CacheControl, defaultRequestDirectives, out var reqDirective))
        {
            logger.LogRequestNotValidCacheControlDirective(reqDirective, ctx.TraceIdentifier);
            return false;
        }

        var defaultResponseDirectives = directiveChecker.DefaultInvalidResponseDirectives;
        if (directiveChecker.AnyInvalidDirective(ctx.Response.Headers.CacheControl, defaultResponseDirectives, out var resDirective))
        {
            logger.LogResponseNotValidCacheControlDirective(resDirective, ctx.TraceIdentifier);
            return false;
        }

        if (!options.Filter(ctx))
        {
            logger.LogFilterRejected(ctx.TraceIdentifier);
            return false;
        }

        logger.LogContextFilterFinished(ctx.TraceIdentifier);
        return true;
    }
}
