using Microsoft.AspNetCore.Http;
using Tracker.AspNet.Models;

namespace Tracker.AspNet.Services.Contracts;

public interface IRequestFilter
{
    bool RequestValid(HttpContext context, ImmutableGlobalOptions options);
}
