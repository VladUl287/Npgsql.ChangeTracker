using Microsoft.EntityFrameworkCore;

namespace Tracker.Core.Services.Contracts;

/// <summary>
/// Interface for generating source identifiers in Entity Framework Core applications.
/// </summary>
public interface IProviderIdGenerator
{
    /// <summary>
    /// Generates a unique identifier for a data source.
    /// </summary>
    /// <typeparam name="TContext">
    /// The type of <see cref="DbContext"/> used for identifier generation.
    /// Must be a subclass of <see cref="DbContext"/>.
    /// </typeparam>
    /// <returns>
    /// A string representing the generated source identifier.
    /// </returns>
    string GenerateId<TContext>() where TContext : DbContext;
}