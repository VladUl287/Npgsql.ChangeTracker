using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Tracker.AspNet.Logging;
using Tracker.AspNet.Models;
using Tracker.AspNet.Services.Contracts;

namespace Tracker.AspNet.Services;

public class DefaultRequestFilter(ILogger<DefaultRequestFilter> logger) : IRequestFilter
{
    public virtual bool ShouldProcessRequest<TState>(HttpContext context, Func<TState, GlobalOptions> optionsProvider, TState state)
    {
        var requestPath = context.Request.Path;

        if (!HttpMethods.IsGet(context.Request.Method))
        {
            logger.LogNotGetRequest(context.Request.Method, requestPath);
            return false;
        }

        if (context.Response.Headers.ETag.Count != 0)
        {
            logger.LogEtagHeaderPresent(requestPath);
            return false;
        }

        if (context.Response.Headers.CacheControl.Any(c => c?.Contains("immutable", StringComparison.OrdinalIgnoreCase) is true))
        {
            logger.LogImmutableCacheDetected(requestPath);
            return false;
        }

        var options = optionsProvider(state);
        if (!options.Filter(context))
        {
            logger.LogFilterRejected(requestPath);
            return false;
        }

        logger.LogRequestValidated(requestPath);
        return true;
    }
}
