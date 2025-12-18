using System.Collections.Immutable;

namespace Tracker.Core.Services.Contracts;

/// <summary>
/// Defines operations for managing source data tracking and timestamp management.
/// </summary>
public interface ISourceOperations
{
    /// <summary>
    /// Gets the unique identifier for the data source.
    /// </summary>
    /// <value>
    /// A string representing the source identifier.
    /// </value>
    string SourceId { get; }

    /// <summary>
    /// Retrieves the last recorded timestamp for a specific key from the data source.
    /// </summary>
    /// <param name="key">The unique identifier for the tracked item.</param>
    /// <param name="token">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>
    /// A <see cref="ValueTask{TResult}"/> that represents the asynchronous operation.
    /// The task result contains the <see cref="DateTimeOffset"/> of the last recorded timestamp for the specified key.
    /// Returns <see cref="DateTimeOffset.MinValue"/> if no timestamp is found.
    /// </returns>
    ValueTask<DateTimeOffset> GetLastTimestamp(string key, CancellationToken token = default);

    /// <summary>
    /// Retrieves the last recorded timestamps for multiple keys in a batch operation.
    /// </summary>
    /// <param name="keys">An immutable array of keys to retrieve timestamps for.</param>
    /// <param name="timestamps">
    /// A pre-allocated array where the retrieved timestamps will be stored.
    /// The array must have the same length as <paramref name="keys"/>.
    /// </param>
    /// <param name="token">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>
    /// A <see cref="ValueTask"/> that represents the asynchronous operation.
    /// </returns>
    ValueTask GetLastTimestamps(ImmutableArray<string> keys, DateTimeOffset[] timestamps, CancellationToken token = default);

    /// <summary>
    /// Retrieves the overall last timestamp from the data source across all tracked items.
    /// </summary>
    /// <param name="token">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>
    /// A <see cref="ValueTask{TResult}"/> that represents the asynchronous operation.
    /// The task result contains the most recent <see cref="DateTimeOffset"/> across all tracked items.
    /// Returns <see cref="DateTimeOffset.MinValue"/> if no timestamps are found.
    /// </returns>
    ValueTask<DateTimeOffset> GetLastTimestamp(CancellationToken token = default);

    /// <summary>
    /// Enables tracking for a specific key in the data source.
    /// </summary>
    /// <param name="key">The unique identifier for the item to start tracking.</param>
    /// <param name="token">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>
    /// A <see cref="ValueTask{TResult}"/> that represents the asynchronous operation.
    /// The task result is <c>true</c> if tracking was successfully enabled; <c>false</c> if tracking was already enabled or the operation failed.
    /// </returns>
    ValueTask<bool> EnableTracking(string key, CancellationToken token = default);

    /// <summary>
    /// Disables tracking for a specific key in the data source.
    /// </summary>
    /// <param name="key">The unique identifier for the item to stop tracking.</param>
    /// <param name="token">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>
    /// A <see cref="ValueTask{TResult}"/> that represents the asynchronous operation.
    /// The task result is <c>true</c> if tracking was successfully disabled; <c>false</c> if tracking was already disabled or the operation failed.
    /// </returns>
    ValueTask<bool> DisableTracking(string key, CancellationToken token = default);

    /// <summary>
    /// Checks whether tracking is currently enabled for a specific key.
    /// </summary>
    /// <param name="key">The unique identifier for the item to check.</param>
    /// <param name="token">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>
    /// A <see cref="ValueTask{TResult}"/> that represents the asynchronous operation.
    /// The task result is <c>true</c> if tracking is enabled for the specified key; otherwise, <c>false</c>.
    /// </returns>
    ValueTask<bool> IsTracking(string key, CancellationToken token = default);

    /// <summary>
    /// Sets or updates the last recorded timestamp for a specific key.
    /// </summary>
    /// <param name="key">The unique identifier for the tracked item.</param>
    /// <param name="value">The timestamp to record for the specified key.</param>
    /// <param name="token">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>
    /// A <see cref="ValueTask{TResult}"/> that represents the asynchronous operation.
    /// The task result is <c>true</c> if the timestamp was successfully set; otherwise, <c>false</c>.
    /// </returns>
    ValueTask<bool> SetLastTimestamp(string key, DateTimeOffset value, CancellationToken token = default);
}
