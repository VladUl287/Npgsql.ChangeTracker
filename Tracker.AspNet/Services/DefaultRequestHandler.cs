using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Buffers;
using Tracker.AspNet.Logging;
using Tracker.AspNet.Models;
using Tracker.AspNet.Services.Contracts;
using Tracker.Core.Services.Contracts;

namespace Tracker.AspNet.Services;

public sealed class DefaultRequestHandler(
    IETagProvider eTagService, ISourceOperationsResolver operationsResolver, ITrackerHasher timestampsHasher,
    ILogger<DefaultRequestHandler> logger) : IRequestHandler
{
    public async ValueTask<bool> IsNotModified(HttpContext ctx, ImmutableGlobalOptions options, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(ctx, nameof(ctx));
        ArgumentNullException.ThrowIfNull(options, nameof(options));

        var traceId = new TraceId(ctx);

        logger.LogRequestHandleStarted(traceId, ctx.Request.Path);
        try
        {
            var operationProvider = GetOperationsProvider(ctx, options);
            logger.LogSourceProviderResolved(traceId, operationProvider.SourceId);

            var lastTimestamp = await GetLastVersionAsync(options, operationProvider, token);

            var notModified = NotModified(ctx, options, traceId, lastTimestamp, out var suffix);
            if (notModified)
                return true;

            var etag = eTagService.Generate(lastTimestamp, suffix);
            ctx.Response.Headers.CacheControl = options.CacheControl;
            ctx.Response.Headers.ETag = etag;
            logger.LogETagAdded(etag, traceId);
            return false;
        }
        finally
        {
            logger.LogRequestHandleFinished(traceId);
        }
    }

    private bool NotModified(HttpContext ctx, ImmutableGlobalOptions options, TraceId traceId, ulong lastTimestamp, out string suffix)
    {
        suffix = string.Empty;

        if (ctx.Request.Headers.IfNoneMatch.Count == 0)
            return false;

        var ifNoneMatch = ctx.Request.Headers.IfNoneMatch[0];
        if (ifNoneMatch is null)
            return false;

        suffix = options.Suffix(ctx);
        if (!eTagService.Compare(ifNoneMatch, lastTimestamp, suffix))
            return false;

        ctx.Response.StatusCode = StatusCodes.Status304NotModified;
        logger.LogNotModified(traceId, ifNoneMatch);
        return true;
    }

    private async ValueTask<ulong> GetLastVersionAsync(ImmutableGlobalOptions options, ISourceOperations sourceOperations, CancellationToken token)
    {
        switch (options.Tables.Length)
        {
            case 0:
                var timestamp = await sourceOperations.GetLastVersion(token);
                return (ulong)timestamp;
            case 1:
                var tableName = options.Tables[0];
                var singleTableTimestamp = await sourceOperations.GetLastVersion(tableName, token);
                return (ulong)singleTableTimestamp;
            default:
                var timestamps = ArrayPool<long>.Shared.Rent(options.Tables.Length);
                await sourceOperations.GetLastVersions(options.Tables, timestamps, token);
                var hash = timestampsHasher.Hash(timestamps.AsSpan(0, options.Tables.Length));
                ArrayPool<long>.Shared.Return(timestamps);
                return hash;
        }
    }

    private ISourceOperations GetOperationsProvider(HttpContext ctx, ImmutableGlobalOptions opt)
    {
        var traceId = new TraceId(ctx);

        if (opt.Source is not null)
        {
            if (operationsResolver.TryResolve(opt.Source, out var provider))
                return provider;

            logger.LogSourceProviderNotRegistered(opt.Source, traceId);
        }

        return
            opt.SourceOperations ??
            opt.SourceOperationsFactory?.Invoke(ctx) ??
            operationsResolver.First ??
            throw new NullReferenceException($"Source operations provider not found. TraceId - '{ctx.TraceIdentifier}'");
    }
}
