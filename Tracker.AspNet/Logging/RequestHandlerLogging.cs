using Microsoft.Extensions.Logging;

namespace Tracker.AspNet.Logging;

public static partial class RequestHandlerLogging
{
    [LoggerMessage(Level = LogLevel.Debug, Message = "Request handler started. TraceId: {TraceId}. Path - {Path}")]
    public static partial void LogRequestHandleStarted(this ILogger logger, string traceId, string path);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Request handler finished - TraceId: {TraceId}")]
    public static partial void LogRequestHandleFinished(this ILogger logger, string traceId);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Resource not modified. TraceId: '{TraceId}'. ETag: {ETag}")]
    public static partial void LogNotModified(this ILogger logger, string traceId, string etag);

    [LoggerMessage(Level = LogLevel.Debug, Message = "ETag added to response: {ETag}. TraceId: '{TraceId}'")]
    public static partial void LogETagAdded(this ILogger logger, string etag, string traceId);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Source operations provider resolved for request: TraceId - {TraceId}, SourceId - {SourceId}")]
    public static partial void LogSourceProviderResolved(this ILogger logger, string traceId, string sourceId);

    [LoggerMessage(Level = LogLevel.Debug,
        Message = "Getting last timestamp for source: '{SourceId}', tables count: {TablesCount}. TraceId: '{TraceId}'")]
    public static partial void LogGettingLastTimestamp(this ILogger logger, string sourceId, int tablesCount, string traceId);

    [LoggerMessage(Level = LogLevel.Debug,
        Message = "Retrieved overall last timestamp: {Ticks} ticks for source: '{SourceId}'. TraceId: '{TraceId}'")]
    public static partial void LogRetrievedOverallTimestamp(this ILogger logger, ulong ticks, string sourceId, string traceId);

    [LoggerMessage(Level = LogLevel.Debug,
        Message = "Retrieved timestamp for table '{TableName}': {Ticks} ticks for source: '{SourceId}'. TraceId: '{TraceId}'")]
    public static partial void LogRetrievedTableTimestamp(this ILogger logger, string tableName, ulong ticks, string sourceId, string traceId);

    [LoggerMessage(Level = LogLevel.Debug,
        Message = "Retrieved hash of timestamps for multiple tables: {Hash} ticks hash for source: '{SourceId}'. TraceId: '{TraceId}'")]
    public static partial void LogRetrievedMultipleTablesHash(this ILogger logger, ulong hash, string sourceId, string traceId);

    [LoggerMessage(Level = LogLevel.Debug,
        Message = "Source operations resolved from ISourceOperationsResolver for source: '{Source}'. TraceId: '{TraceId}'")]
    public static partial void LogResolvedFromResolver(this ILogger logger, string? source, string traceId);

    [LoggerMessage(Level = LogLevel.Debug,
        Message = "Source operations resolved from options.SourceOperations. TraceId: '{TraceId}'")]
    public static partial void LogResolvedFromOptions(this ILogger logger, string traceId);

    [LoggerMessage(Level = LogLevel.Debug,
        Message = "Source operations resolved from options.SourceOperationsFactory. TraceId: '{TraceId}'")]
    public static partial void LogResolvedFromFactory(this ILogger logger, string traceId);

    [LoggerMessage(Level = LogLevel.Debug,
        Message = "Source operations resolved from ISourceOperationsResolver.First (fallback). TraceId: '{TraceId}'")]
    public static partial void LogResolvedFromFirstFallback(this ILogger logger, string traceId);

    [LoggerMessage(Level = LogLevel.Error,
        Message = "No source operations provider found after checking all sources. TraceId: '{TraceId}'")]
    public static partial void LogNoProviderFound(this ILogger logger, string traceId);
}
