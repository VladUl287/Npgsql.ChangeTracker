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
            var operationProvider = GetOperationsProvider(ctx, options);
            logger.LogSourceProviderResolved(ctx.TraceIdentifier, operationProvider.SourceId);

            var lastTimestamp = await GetLastTimestampAsync(options, operationProvider, token);

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

    private async Task<ulong> GetLastTimestampAsync(ImmutableGlobalOptions options, ISourceOperations sourceOperations, CancellationToken token)
    {
        switch (options.Tables.Length)
        {
            case 0:
                var timestamp = await sourceOperations.GetLastTimestamp(token);
                return (ulong)timestamp.Ticks;
            case 1:
                var tableName = options.Tables[0];
                var singleTableTimestamp = await sourceOperations.GetLastTimestamp(tableName, token);
                return (ulong)singleTableTimestamp.Ticks;
            default:
                var timestamps = ArrayPool<DateTimeOffset>.Shared.Rent(options.Tables.Length);
                await sourceOperations.GetLastTimestamps(options.Tables, timestamps, token);
                var hash = timestampsHasher.Hash(timestamps.AsSpan(0, options.Tables.Length));
                ArrayPool<DateTimeOffset>.Shared.Return(timestamps);
                return hash;
        }
    }

    private ISourceOperations GetOperationsProvider(HttpContext ctx, ImmutableGlobalOptions opt)
    {
        if (opt.Source is not null)
        {
            if (operationsResolver.TryResolve(opt.Source, out var provider))
                return provider;

            logger.LogSourceProviderNotRegistered(opt.Source, ctx.TraceIdentifier);
        }

        return
            opt.SourceOperations ??
            opt.SourceOperationsFactory?.Invoke(ctx) ??
            operationsResolver.First ??
            throw new NullReferenceException($"Source operations provider not found. TraceId - '{ctx.TraceIdentifier}'");
    }
}
