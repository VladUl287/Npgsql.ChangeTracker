namespace Tracker.Core.Services.Contracts;

/// <summary>
/// Provides functionality to generate and compare ETags based on assembly timestamp, 
/// entity timestamp, and an optional suffix.
/// </summary>
/// <remarks>
/// The ETag format is: {assemblyTimestamp}-{entityTimestamp}[-{suffix}]
/// where:
/// - assemblyTimestamp is obtained from <see cref="IAssemblyTimestampProvider"/>
/// - entityTimestamp is the last modified timestamp of the entity
/// - suffix is an optional identifier (e.g., content hash, version identifier)
/// 
/// The suffix is optional and only included when provided.
/// </remarks>
/// <param name="assemblyTimestampProvider">Provider for the assembly timestamp used in ETag generation.</param>
public interface IETagProvider
{
    /// <summary>
    /// Compares a provided ETag against expected values to determine if they match.
    /// </summary>
    /// <param name="etag">The ETag to compare.</param>
    /// <param name="lastTimestamp">The last modified timestamp of the entity.</param>
    /// <param name="suffix">Optional suffix used in ETag generation (e.g., content hash).</param>
    /// <returns>
    /// <c>true</c> if the provided ETag matches the expected format and values;
    /// otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    /// The comparison should perform the following checks in order:
    /// 1. Length validation
    /// 2. Assembly timestamp segment comparison
    /// 3. Format separator validation ('-' characters)
    /// 4. Entity timestamp segment comparison
    /// 5. Suffix comparison (if present)
    /// 
    /// Should return <c>false</c> at the first mismatch.
    /// </remarks>
    bool Compare(string etag, ulong lastTimestamp, string suffix);

    /// <summary>
    /// Generates an ETag based on the entity's last modified timestamp and an optional suffix.
    /// </summary>
    /// <param name="lastTimestamp">The last modified timestamp of the entity.</param>
    /// <param name="suffix">Optional suffix to include in the ETag (e.g., content hash).</param>
    /// <returns>
    /// A string ETag in the format: {assemblyTimestamp}-{lastTimestamp}[-{suffix}]
    /// </returns>
    /// <remarks>
    /// The generated ETag should follow the pattern:
    /// - Assembly timestamp
    /// - Hyphen separator
    /// - Entity timestamp
    /// - Optional hyphen separator and suffix (if suffix is not empty)
    /// </remarks>
    /// <example>
    /// <code>
    /// // With suffix
    /// var etag = provider.Generate(123456789, "abc123");
    /// // Returns format: "637976832000000000-123456789-abc123"
    /// 
    /// // Without suffix
    /// var etag = provider.Generate(123456789, "");
    /// // Returns format: "637976832000000000-123456789"
    /// </code>
    /// </example>
    string Generate(ulong lastTimestamp, string suffix);
}
