using Microsoft.Extensions.Logging;

namespace Tracker.AspNet.Logging;

public static partial class TrackMiddlewareLogger
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Action descriptor not found for the current request path")]
    public static partial void LogDescriptorNotFound(this ILogger logger);

    [LoggerMessage(Level = LogLevel.Warning, Message = "ETag header already exists in response")]
    public static partial void LogETagAlreadyExists(this ILogger logger);

    [LoggerMessage(Level = LogLevel.Error, Message = "DbContext {DbContextType} not found in request services")]
    public static partial void LogDbContextNotFound(this ILogger logger, string dbContextType);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Last timestamp not found for the specified tables")]
    public static partial void LogLastTimestampNotFound(this ILogger logger);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Invalid timestamp format: {Timestamp}")]
    public static partial void LogInvalidTimestampFormat(this ILogger logger, string timestamp);

    [LoggerMessage(Level = LogLevel.Information, Message = "Request was cancelled")]
    public static partial void LogOperationCancelled(this ILogger logger);

    [LoggerMessage(Level = LogLevel.Error, Message = "An error occurred while processing ETag validation")]
    public static partial void LogException(this ILogger logger, Exception ex);
}
