namespace Tracker.Core.Services.Contracts;

/// <summary>
/// Provides a functionality for work with hash values from version numbers.
/// </summary>
public interface ITrackerHasher
{
    /// <summary>
    /// Computes a 64-bit hash value from the specified version sequence.
    /// </summary>
    /// <param name="versions">
    /// A read-only span of 64-bit integers representing version numbers.
    /// The span should contain the sequence of versions to be hashed.
    /// </param>
    /// <returns>
    /// A 64-bit unsigned integer representing the computed hash value.
    /// </returns>
    ulong Hash(ReadOnlySpan<long> versions);
}