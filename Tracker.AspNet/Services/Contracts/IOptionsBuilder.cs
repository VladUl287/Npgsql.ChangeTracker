using Microsoft.EntityFrameworkCore;

namespace Tracker.AspNet.Services.Contracts;

public interface IOptionsBuilder<TMutalbe, TImmutable> 
    where TMutalbe : class
    where TImmutable : class
{
    TImmutable Build<TContext>(TMutalbe mutalbe) where TContext : DbContext;
}
