using Microsoft.AspNetCore.Http;
using Tracker.AspNet.Models;

namespace Tracker.AspNet.Services.Contracts;

public interface IETagService
{
    Task<bool> NotModified(HttpContext context, ImmutableGlobalOptions options, CancellationToken token);
}
