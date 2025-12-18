using Microsoft.EntityFrameworkCore;

namespace Tracker.AspNet.Services.Contracts;

/// <summary>
/// Constructs immutable option instances from mutable configuration objects.
/// </summary>
/// <typeparam name="TMutable">The mutable configuration type used for building.</typeparam>
/// <typeparam name="TImmutable">The resulting immutable options type.</typeparam>
/// <remarks>
/// This interface provides a way to create strongly-typed, immutable configuration objects
/// from mutable configuration data, potentially incorporating database context information.
/// </remarks>
public interface IOptionsBuilder<TMutable, TImmutable>
    where TMutable : class
    where TImmutable : class
{
    /// <summary>
    /// Builds an immutable options instance from the provided mutable configuration.
    /// </summary>
    /// <param name="mutable">The mutable configuration object containing option values.</param>
    /// <returns>A new immutable options instance.</returns>
    TImmutable Build(TMutable mutable);

    /// <summary>
    /// Builds an immutable options instance from the provided mutable configuration,
    /// using a specific <see cref="DbContext"/> type for database-aware configuration.
    /// </summary>
    /// <typeparam name="TContext">The type of <see cref="DbContext"/> to use for context-specific configuration.</typeparam>
    /// <param name="mutable">The mutable configuration object containing option values.</param>
    /// <returns>A new immutable options instance configured for the specified context type.</returns>
    /// <remarks>
    /// This overload allows retrieving table names from context entity types.
    /// </remarks>
    TImmutable Build<TContext>(TMutable mutable) where TContext : DbContext;
}