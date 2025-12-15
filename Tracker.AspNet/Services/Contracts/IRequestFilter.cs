using Microsoft.AspNetCore.Http;
using Tracker.AspNet.Models;

namespace Tracker.AspNet.Services.Contracts;

/// <summary>
/// Determines whether ETag generation and comparison are permitted based on context data, 
/// configured options, and HTTP specification compliance requirements.
/// </summary>
public interface IRequestFilter
{
    /// <summary>
    /// Determines if the HTTP request qualifies for ETag operations according to
    /// protocol rules and application configuration.    
    /// </summary>
    /// <param name="context">The HTTP context containing request and response details to evaluate.</param>
    /// <param name="options">The immutable global configuration options defining ETag behavior rules.</param>
    /// <returns>
    /// <c>true</c> if the provided context matches the expected validation rules; otherwise, <c>false</c>.
    /// </returns>
    bool RequestValid(HttpContext context, ImmutableGlobalOptions options);
}
