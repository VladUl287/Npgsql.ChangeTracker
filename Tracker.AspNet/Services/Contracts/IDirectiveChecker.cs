using Microsoft.Extensions.Primitives;
using System.Diagnostics.CodeAnalysis;

namespace Tracker.AspNet.Services.Contracts;

/// <summary>
/// Validates HTTP request/response directives against a set of invalid values.
/// </summary>
public interface IDirectiveChecker
{
    /// <summary>
    /// Checks if any invalid directive is present in the given headers.
    /// </summary>
    /// <param name="headers">The HTTP header values to check.</param>
    /// <param name="invalidDirectives">A span of directive names that are considered invalid.</param>
    /// <param name="directive">When this method returns <see langword="true"/>, contains the first invalid directive found; otherwise, <see langword="null"/>.</param>
    /// <returns><see langword="true"/> if any invalid directive is found; otherwise, <see langword="false"/>.</returns>
    bool AnyInvalidDirective(StringValues headers, ReadOnlySpan<string> invalidDirectives, [NotNullWhen(true)] out string? directive);
}