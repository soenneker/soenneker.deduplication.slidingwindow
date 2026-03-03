using AwesomeAssertions;
using Soenneker.Deduplication.SlidingWindow.Abstract;
using Soenneker.Tests.Unit;
using System;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Soenneker.Deduplication.SlidingWindow.Tests;

public sealed class SlidingWindowDedupeTests : UnitTest
{
    private static ISlidingWindowDedupe CreateDedupe(TimeSpan? window = null, TimeSpan? rotationInterval = null, int capacityHint = 0, long seed = 0)
    {
        return new SlidingWindowXxHashDedupe(window ?? TimeSpan.FromMinutes(1), rotationInterval ?? TimeSpan.FromSeconds(10), capacityHint, seed);
    }

    [Fact]
    public void TryMarkSeen_string_first_time_returns_true()
    {
        using ISlidingWindowDedupe dedupe = CreateDedupe();
        dedupe.TryMarkSeen("hello")
              .Should()
              .BeTrue();
    }

    [Fact]
    public void TryMarkSeen_string_duplicate_returns_false()
    {
        using ISlidingWindowDedupe dedupe = CreateDedupe();
        dedupe.TryMarkSeen("hello");
        dedupe.TryMarkSeen("hello")
              .Should()
              .BeFalse();
    }

    [Fact]
    public void TryMarkSeen_string_null_throws()
    {
        using ISlidingWindowDedupe dedupe = CreateDedupe();
        Action act = () => dedupe.TryMarkSeen(null!);
        act.Should()
           .Throw<ArgumentNullException>();
    }

    [Fact]
    public void TryMarkSeen_span_first_time_returns_true()
    {
        using ISlidingWindowDedupe dedupe = CreateDedupe();
        dedupe.TryMarkSeen("world".AsSpan())
              .Should()
              .BeTrue();
    }

    [Fact]
    public void TryMarkSeen_span_duplicate_returns_false()
    {
        using ISlidingWindowDedupe dedupe = CreateDedupe();
        dedupe.TryMarkSeen("world".AsSpan());
        dedupe.TryMarkSeen("world".AsSpan())
              .Should()
              .BeFalse();
    }

    [Fact]
    public void TryMarkSeenUtf8_first_time_returns_true()
    {
        using ISlidingWindowDedupe dedupe = CreateDedupe();
        byte[] utf8 = Encoding.UTF8.GetBytes("utf8-value");
        dedupe.TryMarkSeenUtf8(utf8)
              .Should()
              .BeTrue();
    }

    [Fact]
    public void TryMarkSeenUtf8_duplicate_returns_false()
    {
        using ISlidingWindowDedupe dedupe = CreateDedupe();
        byte[] utf8 = Encoding.UTF8.GetBytes("utf8-value");
        dedupe.TryMarkSeenUtf8(utf8);
        dedupe.TryMarkSeenUtf8(utf8)
              .Should()
              .BeFalse();
    }

    [Fact]
    public void Same_content_via_string_and_utf8_is_deduplicated()
    {
        using ISlidingWindowDedupe dedupe = CreateDedupe();
        dedupe.TryMarkSeen("same")
              .Should()
              .BeTrue();
        byte[] utf8 = Encoding.UTF8.GetBytes("same");
        dedupe.TryMarkSeenUtf8(utf8)
              .Should()
              .BeFalse();
    }

    [Fact]
    public void Contains_string_returns_true_after_TryMarkSeen()
    {
        using ISlidingWindowDedupe dedupe = CreateDedupe();
        dedupe.TryMarkSeen("contained");
        dedupe.Contains("contained")
              .Should()
              .BeTrue();
    }

    [Fact]
    public void Contains_string_returns_false_when_not_seen()
    {
        using ISlidingWindowDedupe dedupe = CreateDedupe();
        dedupe.Contains("not-seen")
              .Should()
              .BeFalse();
    }

    [Fact]
    public void Contains_string_null_throws()
    {
        using ISlidingWindowDedupe dedupe = CreateDedupe();
        Action act = () => dedupe.Contains(null!);
        act.Should()
           .Throw<ArgumentNullException>();
    }

    [Fact]
    public void Contains_span_returns_true_after_TryMarkSeen()
    {
        using ISlidingWindowDedupe dedupe = CreateDedupe();
        dedupe.TryMarkSeen("span-contained".AsSpan());
        dedupe.Contains("span-contained".AsSpan())
              .Should()
              .BeTrue();
    }

    [Fact]
    public void ContainsUtf8_returns_true_after_TryMarkSeenUtf8()
    {
        using ISlidingWindowDedupe dedupe = CreateDedupe();
        byte[] utf8 = Encoding.UTF8.GetBytes("utf8-contained");
        dedupe.TryMarkSeenUtf8(utf8);
        dedupe.ContainsUtf8(utf8)
              .Should()
              .BeTrue();
    }

    [Fact]
    public void TryRemove_string_returns_true_when_present()
    {
        using ISlidingWindowDedupe dedupe = CreateDedupe();
        dedupe.TryMarkSeen("to-remove");
        dedupe.TryRemove("to-remove")
              .Should()
              .BeTrue();
        dedupe.Contains("to-remove")
              .Should()
              .BeFalse();
    }

    [Fact]
    public void TryRemove_string_returns_false_when_not_present()
    {
        using ISlidingWindowDedupe dedupe = CreateDedupe();
        dedupe.TryRemove("never-added")
              .Should()
              .BeFalse();
    }

    [Fact]
    public void TryRemove_string_null_throws()
    {
        using ISlidingWindowDedupe dedupe = CreateDedupe();
        Action act = () => dedupe.TryRemove(null!);
        act.Should()
           .Throw<ArgumentNullException>();
    }

    [Fact]
    public void TryRemove_span_removes_and_allow_re_add()
    {
        using ISlidingWindowDedupe dedupe = CreateDedupe();
        dedupe.TryMarkSeen("span-remove".AsSpan());
        dedupe.TryRemove("span-remove".AsSpan())
              .Should()
              .BeTrue();
        dedupe.TryMarkSeen("span-remove".AsSpan())
              .Should()
              .BeTrue();
    }

    [Fact]
    public void TryRemoveUtf8_removes_entry()
    {
        using ISlidingWindowDedupe dedupe = CreateDedupe();
        byte[] utf8 = Encoding.UTF8.GetBytes("utf8-remove");
        dedupe.TryMarkSeenUtf8(utf8);
        dedupe.TryRemoveUtf8(utf8)
              .Should()
              .BeTrue();
        dedupe.ContainsUtf8(utf8)
              .Should()
              .BeFalse();
    }

    [Fact]
    public void Count_increases_on_TryMarkSeen_new_and_decreases_on_TryRemove()
    {
        using ISlidingWindowDedupe dedupe = CreateDedupe();
        dedupe.Count.Should()
              .Be(0);
        dedupe.TryMarkSeen("a");
        dedupe.Count.Should()
              .Be(1);
        dedupe.TryMarkSeen("b");
        dedupe.Count.Should()
              .Be(2);
        dedupe.TryMarkSeen("a");
        dedupe.Count.Should()
              .Be(2);
        dedupe.TryRemove("a");
        dedupe.Count.Should()
              .Be(1);
        dedupe.TryRemove("b");
        dedupe.Count.Should()
              .Be(0);
    }

    [Fact]
    public void Different_seed_produces_different_buckets()
    {
        using ISlidingWindowDedupe dedupe0 = CreateDedupe(seed: 0);
        using ISlidingWindowDedupe dedupe1 = CreateDedupe(seed: 1);
        dedupe0.TryMarkSeen("same-key");
        dedupe1.TryMarkSeen("same-key");
        dedupe0.Contains("same-key")
               .Should()
               .BeTrue();
        dedupe1.Contains("same-key")
               .Should()
               .BeTrue();
        dedupe0.TryRemove("same-key");
        dedupe0.Contains("same-key")
               .Should()
               .BeFalse();
        dedupe1.Contains("same-key")
               .Should()
               .BeTrue();
    }

    [Fact]
    public void Empty_span_hashes_consistently()
    {
        using ISlidingWindowDedupe dedupe = CreateDedupe();
        dedupe.TryMarkSeen(ReadOnlySpan<char>.Empty)
              .Should()
              .BeTrue();
        dedupe.TryMarkSeen(ReadOnlySpan<char>.Empty)
              .Should()
              .BeFalse();
        dedupe.Contains(ReadOnlySpan<char>.Empty)
              .Should()
              .BeTrue();
    }

    [Fact]
    public void Empty_utf8_hashes_consistently()
    {
        using ISlidingWindowDedupe dedupe = CreateDedupe();
        dedupe.TryMarkSeenUtf8(ReadOnlySpan<byte>.Empty)
              .Should()
              .BeTrue();
        dedupe.TryMarkSeenUtf8(ReadOnlySpan<byte>.Empty)
              .Should()
              .BeFalse();
        dedupe.ContainsUtf8(ReadOnlySpan<byte>.Empty)
              .Should()
              .BeTrue();
    }

    [Fact]
    public void Dispose_can_be_called()
    {
        ISlidingWindowDedupe dedupe = CreateDedupe();
        dedupe.Dispose();
    }

    [Fact]
    public async Task DisposeAsync_can_be_called()
    {
        ISlidingWindowDedupe dedupe = CreateDedupe();
        await dedupe.DisposeAsync();
    }
}