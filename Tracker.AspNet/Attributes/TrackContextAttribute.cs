using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using Tracker.AspNet.Models;
using Tracker.AspNet.Services.Contracts;
using Tracker.Core.Extensions;

namespace Tracker.AspNet.Attributes;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class TrackAttribute<TContext>(
    string[]? tables = null, string? sourceId = null, string? cacheControl = null) : Attribute, IAsyncActionFilter 
    where TContext : DbContext
{
    private readonly ImmutableArray<string> _tables = tables?.ToImmutableArray() ?? [];
    private readonly string? _sourceId = sourceId;
    private readonly string? _cacheControl = cacheControl;

    public async Task OnActionExecutionAsync(ActionExecutingContext execContext, ActionExecutionDelegate next)
    {
        var options = GetOrSetOptions(execContext);

        var httpCtx = execContext.HttpContext;
        var reqServices = httpCtx.RequestServices;

        var requestFilter = reqServices.GetRequiredService<IRequestFilter>();
        var shouldProcessRequest = requestFilter.ShouldProcessRequest(httpCtx, options);
        if (!shouldProcessRequest)
        {
            await next();
            return;
        }

        var cancelToken = httpCtx.RequestAborted;
        if (cancelToken.IsCancellationRequested)
            return;

        var etagService = reqServices.GetRequiredService<IETagService>();
        var shouldReturnNotModified = await etagService.TrySetETagAsync(httpCtx, options, cancelToken);
        if (!shouldReturnNotModified)
        {
            await next();
            return;
        }
    }

    private ImmutableGlobalOptions? _actionOptions;
    private readonly Lock _lock = new();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ImmutableGlobalOptions GetOrSetOptions(ActionExecutingContext execContext)
    {
        if (_actionOptions is not null)
            return _actionOptions;

        lock (_lock)
        {
            if (_actionOptions is not null)
                return _actionOptions;

            var baseOptions = execContext.HttpContext.RequestServices.GetRequiredService<ImmutableGlobalOptions>();
            _actionOptions = baseOptions with
            {
                CacheControl = _cacheControl ?? baseOptions.CacheControl,
                Source = _sourceId ?? baseOptions.Source ?? typeof(TContext).GetTypeHashId(),
                Tables = _tables
            };
            return _actionOptions;
        }
    }
}
