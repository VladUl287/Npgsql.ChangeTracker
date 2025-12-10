using Microsoft.Extensions.Logging;

namespace Tracker.AspNet.Logging;

public static partial class RequestHandlerLogging
{
    [LoggerMessage(Level = LogLevel.Debug, Message = "Request handler started - TraceId: {TraceId}")]
    public static partial void LogRequestHandleStarted(this ILogger logger, string traceId);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Request handler finished - TraceId: {TraceId}")]
    public static partial void LogRequestHandleFinished(this ILogger logger, string traceId);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Resource not modified. TraceId: '{traceId}'. ETag: {ETag}")]
    public static partial void LogNotModified(this ILogger logger, string traceId, string etag);

    [LoggerMessage(Level = LogLevel.Debug, Message = "ETag added to response: {ETag}. TraceId: '{TraceId}'")]
    public static partial void LogETagAdded(this ILogger logger, string etag, string traceId);
}
