using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Tracker.AspNet.Logging;
using Tracker.AspNet.Models;
using Tracker.AspNet.Services.Contracts;

namespace Tracker.AspNet.Services;

/// <summary>
/// Basic implementation of <see cref="IRequestFilter"/> which determines whether ETag generation and comparison are permitted based on context data, 
/// configured options, and HTTP specification compliance requirements.
/// </summary>
public sealed class DefaultRequestFilter(ILogger<DefaultRequestFilter> logger) : IRequestFilter
{
    private static readonly string[] _invalidRequestDirectives = ["no-transform", "no-store"];
    private static readonly string[] _invalidResponseDirectives = ["no-transform", "no-store", "immutable"];

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

        if (AnyInvalidCacheControl(ctx.Request.Headers.CacheControl, _invalidRequestDirectives, out var reqDirective))
        {
            logger.LogRequestNotValidCacheControlDirective(reqDirective, ctx.TraceIdentifier);
            return false;
        }

        if (AnyInvalidCacheControl(ctx.Response.Headers.CacheControl, _invalidResponseDirectives, out var resDirective))
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool AnyInvalidCacheControl(
        StringValues headers, ReadOnlySpan<string> invalidDirectives, [NotNullWhen(true)] out string? directive)
    {
        directive = null;

        if (headers.Count == 0)
            return false;

        foreach (var header in headers)
        {
            if (header is null)
                continue;

            foreach (var invalidDirective in invalidDirectives)
            {
                if (header.Contains(invalidDirective, StringComparison.OrdinalIgnoreCase))
                {
                    directive = invalidDirective;
                    return true;
                }
            }
        }

        return false;
    }
}
