using Soenneker.Deduplication.SlidingWindow.Abstract;
using Soenneker.Hashing.XxHash;
using System;
using System.Buffers;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Soenneker.Sets.Concurrent.SlidingWindow;

namespace Soenneker.Deduplication.SlidingWindow;

///<inheritdoc cref="ISlidingWindowDedupe"/>
public sealed class SlidingWindowXxHashDedupe : ISlidingWindowDedupe
{
    private const int _stackAllocUtf8Threshold = 256;

    private readonly SlidingWindowConcurrentSet<ulong> _set;

    // Optional seed so you can rotate/partition if you want.
    private readonly long _seed;

    private static readonly Encoding _utf8 = Encoding.UTF8;

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
    public bool TryMarkSeen(ReadOnlySpan<char> value) => _set.TryAdd(HashChars(value, _seed));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryMarkSeenUtf8(ReadOnlySpan<byte> utf8) => _set.TryAdd(HashUtf8(utf8, _seed));

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Contains(string value)
    {
        if (value is null)
            throw new ArgumentNullException(nameof(value));

        return Contains(value.AsSpan());
    }

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Contains(ReadOnlySpan<char> value) => _set.Contains(HashChars(value, _seed));

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ContainsUtf8(ReadOnlySpan<byte> utf8) => _set.Contains(HashUtf8(utf8, _seed));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryRemove(string value)
    {
        if (value is null)
            throw new ArgumentNullException(nameof(value));

        return TryRemove(value.AsSpan());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryRemove(ReadOnlySpan<char> value) => _set.TryRemove(HashChars(value, _seed));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryRemoveUtf8(ReadOnlySpan<byte> utf8) => _set.TryRemove(HashUtf8(utf8, _seed));


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong HashUtf8(ReadOnlySpan<byte> utf8, long seed)
    {
        return seed == 0 ? XxHash3Util.HashToUInt64(utf8) : XxHash3Util.HashToUInt64(utf8, seed);
    }

    private static ulong HashChars(ReadOnlySpan<char> chars, long seed)
    {
        if (chars.IsEmpty)
            return seed == 0 ? XxHash3Util.HashToUInt64(ReadOnlySpan<byte>.Empty) : XxHash3Util.HashToUInt64(ReadOnlySpan<byte>.Empty, seed);

        int byteCount = _utf8.GetByteCount(chars);

        if (byteCount <= _stackAllocUtf8Threshold)
        {
            Span<byte> tmp = stackalloc byte[_stackAllocUtf8Threshold];
            int written = _utf8.GetBytes(chars, tmp);

            ReadOnlySpan<byte> payload = tmp.Slice(0, written);
            return seed == 0 ? XxHash3Util.HashToUInt64(payload) : XxHash3Util.HashToUInt64(payload, seed);
        }

        byte[] rented = ArrayPool<byte>.Shared.Rent(byteCount);
        try
        {
            int written = _utf8.GetBytes(chars, rented);

            ReadOnlySpan<byte> payload = new ReadOnlySpan<byte>(rented, 0, written);
            return seed == 0 ? XxHash3Util.HashToUInt64(payload) : XxHash3Util.HashToUInt64(payload, seed);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(rented);
        }
    }

    public void Dispose() => _set.Dispose();

    public ValueTask DisposeAsync() => _set.DisposeAsync();
}