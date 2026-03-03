using System;
using System.Diagnostics.Contracts;

namespace Soenneker.Deduplication.SlidingWindow.Abstract;

/// <summary>
/// Represents a high-throughput, thread-safe sliding-window deduplication store.
/// </summary>
/// <remarks>
/// <para>
/// Implementations track whether values have been observed within a configurable time window.
/// Entries automatically expire after the configured duration.
/// </para>
/// <para>
/// This interface is designed for scenarios such as:
/// </para>
/// <list type="bullet">
/// <item>
/// <description>Idempotent request handling</description>
/// </item>
/// <item>
/// <description>Lead or event ingestion deduplication</description>
/// </item>
/// <item>
/// <description>Reducing database existence checks under high load</description>
/// </item>
/// </list>
/// <para>
/// Implementations are expected to be:
/// </para>
/// <list type="bullet">
/// <item>
/// <description>Thread-safe</description>
/// </item>
/// <description>Optimized for minimal allocations</description>
/// <item>
/// <description>Suitable for very high write throughput</description>
/// </item>
/// </list>
/// <para>
/// Values are typically hashed internally to reduce memory footprint and improve performance.
/// </para>
/// </remarks>
public interface ISlidingWindowDedupe : IDisposable, IAsyncDisposable
{
    /// <summary>
    /// Gets the number of currently tracked (non-expired) entries.
    /// </summary>
    int Count { get; }

    /// <summary>
    /// Attempts to mark the specified string as seen.
    /// </summary>
    /// <param name="value">The value to track.</param>
    /// <returns>
    /// <c>true</c> if the value was not previously observed within the active window;
    /// otherwise <c>false</c>.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    bool TryMarkSeen(string value);

    /// <summary>
    /// Attempts to mark the specified character span as seen.
    /// </summary>
    /// <param name="value">The value to track.</param>
    /// <returns>
    /// <c>true</c> if the value was not previously observed within the active window;
    /// otherwise <c>false</c>.
    /// </returns>
    bool TryMarkSeen(ReadOnlySpan<char> value);

    /// <summary>
    /// Attempts to mark the specified UTF-8 payload as seen.
    /// </summary>
    /// <param name="utf8">The UTF-8 encoded value to track.</param>
    /// <returns>
    /// <c>true</c> if the value was not previously observed within the active window;
    /// otherwise <c>false</c>.
    /// </returns>
    bool TryMarkSeenUtf8(ReadOnlySpan<byte> utf8);

    /// <summary>
    /// Determines whether the specified string has already been observed
    /// within the active window.
    /// </summary>
    /// <param name="value">The value to check.</param>
    /// <returns>
    /// <c>true</c> if the value exists within the active window; otherwise <c>false</c>.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    [Pure]
    bool Contains(string value);

    /// <summary>
    /// Determines whether the specified character span has already been observed
    /// within the active window.
    /// </summary>
    /// <param name="value">The value to check.</param>
    /// <returns>
    /// <c>true</c> if the value exists within the active window; otherwise <c>false</c>.
    /// </returns>
    [Pure]
    bool Contains(ReadOnlySpan<char> value);

    /// <summary>
    /// Determines whether the specified UTF-8 payload has already been observed
    /// within the active window.
    /// </summary>
    /// <param name="utf8">The UTF-8 encoded value to check.</param>
    /// <returns>
    /// <c>true</c> if the value exists within the active window; otherwise <c>false</c>.
    /// </returns>
    [Pure]
    bool ContainsUtf8(ReadOnlySpan<byte> utf8);

    /// <summary>
    /// Attempts to remove the specified string from the active window.
    /// </summary>
    /// <param name="value">The value to remove.</param>
    /// <returns>
    /// <c>true</c> if the value existed and was removed; otherwise <c>false</c>.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    bool TryRemove(string value);

    /// <summary>
    /// Attempts to remove the specified character span from the active window.
    /// </summary>
    /// <param name="value">The value to remove.</param>
    /// <returns>
    /// <c>true</c> if the value existed and was removed; otherwise <c>false</c>.
    /// </returns>
    bool TryRemove(ReadOnlySpan<char> value);

    /// <summary>
    /// Attempts to remove the specified UTF-8 payload from the active window.
    /// </summary>
    /// <param name="utf8">The UTF-8 encoded value to remove.</param>
    /// <returns>
    /// <c>true</c> if the value existed and was removed; otherwise <c>false</c>.
    /// </returns>
    bool TryRemoveUtf8(ReadOnlySpan<byte> utf8);
}