using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Buffers;
using Tracker.AspNet.Logging;
using Tracker.AspNet.Models;
using Tracker.AspNet.Services.Contracts;
using Tracker.Core.Services.Contracts;

namespace Tracker.AspNet.Services;

/// <summary>
/// Basic implementation of <see cref="IRequestHandler"/> which determines if the requested data has not been modified, 
/// allowing a 304 Not Modified status code to be returned.
/// </summary>
public sealed class DefaultRequestHandler(
    IETagProvider eTagService, ISourceOperationsResolver operationsResolver, ITimestampsHasher timestampsHasher,
    ILogger<DefaultRequestHandler> logger) : IRequestHandler
{
    public async Task<bool> IsNotModified(HttpContext ctx, ImmutableGlobalOptions options, CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(ctx, nameof(ctx));
        ArgumentNullException.ThrowIfNull(options, nameof(options));

        logger.LogRequestHandleStarted(ctx.TraceIdentifier, ctx.Request.Path);
        try
        {
            var provider = GetOperationsProvider(ctx, options);
            var lastTimestamp = await GetLastTimestampAsync(ctx, options, provider, token);

            var ifNoneMatch = ctx.Request.Headers.IfNoneMatch.Count > 0 ? ctx.Request.Headers.IfNoneMatch[0] : null;

            var suffix = options.Suffix(ctx);
            if (ifNoneMatch is not null && eTagService.Compare(ifNoneMatch, lastTimestamp, suffix))
            {
                ctx.Response.StatusCode = StatusCodes.Status304NotModified;
                logger.LogNotModified(ctx.TraceIdentifier, ifNoneMatch);
                return true;
            }

            var etag = eTagService.Generate(lastTimestamp, suffix);
            ctx.Response.Headers.CacheControl = options.CacheControl;
            ctx.Response.Headers.ETag = etag;
            logger.LogETagAdded(etag, ctx.TraceIdentifier);
            return false;
        }
        finally
        {
            logger.LogRequestHandleFinished(ctx.TraceIdentifier);
        }
    }

    private async Task<ulong> GetLastTimestampAsync(
        HttpContext context, ImmutableGlobalOptions options, ISourceOperations sourceOperations, CancellationToken token)
    {
        var traceId = context.TraceIdentifier;
        var sourceId = sourceOperations.SourceId;
        var tablesCount = options.Tables.Length;

        logger.LogGettingLastTimestamp(sourceId, tablesCount, traceId);

        switch (tablesCount)
        {
            case 0:
                var timestamp = await sourceOperations.GetLastTimestamp(token);
                var ticks = (ulong)timestamp.Ticks;

                logger.LogRetrievedOverallTimestamp(ticks, sourceId, traceId);
                return ticks;

            case 1:
                var tableName = options.Tables[0];
                var singleTableTimestamp = await sourceOperations.GetLastTimestamp(tableName, token);
                var singleTableTicks = (ulong)singleTableTimestamp.Ticks;

                logger.LogRetrievedTableTimestamp(tableName, singleTableTicks, sourceId, traceId);
                return singleTableTicks;

            default:
                var timestamps = ArrayPool<DateTimeOffset>.Shared.Rent(options.Tables.Length);
                await sourceOperations.GetLastTimestamps(options.Tables, timestamps, token);
                var hash = timestampsHasher.Hash(timestamps.AsSpan(0, options.Tables.Length));
                ArrayPool<DateTimeOffset>.Shared.Return(timestamps);

                logger.LogRetrievedMultipleTablesHash(hash, sourceId, traceId);
                return hash;
        }
    }

    private ISourceOperations GetOperationsProvider(HttpContext context, ImmutableGlobalOptions options)
    {
        var traceId = context.TraceIdentifier;

        var provider = operationsResolver.TryResolve(options.Source);
        if (provider != null)
        {
            logger.LogResolvedFromResolver(options.Source, traceId);
            return provider;
        }

        provider = options.SourceOperations;
        if (provider != null)
        {
            logger.LogResolvedFromOptions(traceId);
            return provider;
        }

        provider = options.SourceOperationsFactory?.Invoke(context);
        if (provider != null)
        {
            logger.LogResolvedFromFactory(traceId);
            return provider;
        }

        provider = operationsResolver.First;
        if (provider != null)
        {
            logger.LogResolvedFromFirstFallback(traceId);
            return provider;
        }

        logger.LogNoProviderFound(traceId);
        throw new InvalidOperationException($"Source operations provider not found. TraceId: '{traceId}'");
    }
}
