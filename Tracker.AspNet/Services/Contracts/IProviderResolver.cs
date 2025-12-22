using Tracker.AspNet.Models;
using Microsoft.EntityFrameworkCore;
using Tracker.Core.Services.Contracts;

namespace Tracker.AspNet.Services.Contracts;

public interface IProviderResolver
{
    ISourceProvider? SelectProvider(string? sourceId, ImmutableGlobalOptions options);
    ISourceProvider? SelectProvider(GlobalOptions options);

    ISourceProvider? SelectProvider<TContext>(string? sourceId, ImmutableGlobalOptions options) where TContext : DbContext;
    ISourceProvider? SelectProvider<TContext>(GlobalOptions options) where TContext : DbContext;
}
