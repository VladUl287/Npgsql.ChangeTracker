using Microsoft.AspNetCore.Http;
using Tracker.AspNet.Extensions;
using Tracker.AspNet.Models;
using Tracker.AspNet.Services.Contracts;

namespace Tracker.AspNet.Filters;

public sealed class ETagEndpointFilter : IEndpointFilter
{
    private readonly IETagService _eTagService;

    public ETagEndpointFilter(IETagService eTagService, ImmutableGlobalOptions options)
    {
        ArgumentNullException.ThrowIfNull(eTagService, nameof(eTagService));
        ArgumentNullException.ThrowIfNull(options, nameof(options));

        _eTagService = eTagService;
        Options = options;
    }
    
    public ImmutableGlobalOptions Options { get; }

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        if (!context.HttpContext.IsGetRequest())
            return await next(context);

        var token = context.HttpContext.RequestAborted;

        var shouldReturnNotModified = await _eTagService.TrySetETagAsync(context.HttpContext, Options, token);
        if (shouldReturnNotModified)
            return Results.StatusCode(StatusCodes.Status304NotModified);

        return await next(context);
    }
}
