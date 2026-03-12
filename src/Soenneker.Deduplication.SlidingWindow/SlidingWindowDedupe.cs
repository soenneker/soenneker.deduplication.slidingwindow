using Soenneker.Deduplication.SlidingWindow.Abstract;
using Soenneker.Hashing.XxHash;
using System;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Soenneker.Sets.Concurrent.SlidingWindow;

namespace Soenneker.Deduplication.SlidingWindow;

///<inheritdoc cref="ISlidingWindowDedupe"/>
public sealed class SlidingWindowXxHashDedupe : ISlidingWindowDedupe
{
    private readonly SlidingWindowConcurrentSet<ulong> _set;

    private readonly long _seed;

    public SlidingWindowXxHashDedupe(TimeSpan window, TimeSpan rotationInterval, int capacityHint = 0, long seed = 0)
    {
        _seed = seed;
        _set = new SlidingWindowConcurrentSet<ulong>(window, rotationInterval, capacityHint);
    }

    public int Count => _set.Count;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryMarkSeen(string value)
    {
        if (value is null)
            throw new ArgumentNullException(nameof(value));
        return TryMarkSeen(value.AsSpan());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryMarkSeen(ReadOnlySpan<char> value) => _set.TryAdd(XxHash3Util.HashCharsToUInt64(value, _seed));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryMarkSeenUtf8(ReadOnlySpan<byte> utf8) => _set.TryAdd(XxHash3Util.HashUtf8ToUInt64(utf8, _seed));

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Contains(string value)
    {
        if (value is null)
            throw new ArgumentNullException(nameof(value));

        return Contains(value.AsSpan());
    }

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Contains(ReadOnlySpan<char> value) => _set.Contains(XxHash3Util.HashCharsToUInt64(value, _seed));

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ContainsUtf8(ReadOnlySpan<byte> utf8) => _set.Contains(XxHash3Util.HashUtf8ToUInt64(utf8, _seed));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryRemove(string value)
    {
        if (value is null)
            throw new ArgumentNullException(nameof(value));

        return TryRemove(value.AsSpan());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryRemove(ReadOnlySpan<char> value) => _set.TryRemove(XxHash3Util.HashCharsToUInt64(value, _seed));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryRemoveUtf8(ReadOnlySpan<byte> utf8) => _set.TryRemove(XxHash3Util.HashUtf8ToUInt64(utf8, _seed));

    public void Dispose() => _set.Dispose();

    public ValueTask DisposeAsync() => _set.DisposeAsync();
}