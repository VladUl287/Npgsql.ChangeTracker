using Microsoft.AspNetCore.Http;
using Tracker.AspNet.Models;

namespace Tracker.AspNet.Services.Contracts;

public interface IRequestFilter
{
    bool ShouldProcessRequest<TState>(HttpContext context, Func<TState, ImmutableGlobalOptions> optionsProvider, TState state);
}
