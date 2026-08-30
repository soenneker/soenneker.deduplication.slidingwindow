[![](https://img.shields.io/nuget/v/soenneker.deduplication.slidingwindow.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.deduplication.slidingwindow/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.deduplication.slidingwindow/publish-package.yml?style=for-the-badge)](https://github.com/soenneker/soenneker.deduplication.slidingwindow/actions/workflows/publish-package.yml)
[![](https://img.shields.io/nuget/dt/soenneker.deduplication.slidingwindow.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.deduplication.slidingwindow/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.deduplication.slidingwindow/codeql.yml?label=CodeQL&style=for-the-badge)](https://github.com/soenneker/soenneker.deduplication.slidingwindow/actions/workflows/codeql.yml)

# Soenneker.Deduplication.SlidingWindow

A thread-safe, in-memory “seen recently” set with bucketed expiration and compact XXH3-64 keys.

## Installation

```bash
dotnet add package Soenneker.Deduplication.SlidingWindow
```

## Usage

```csharp
using Soenneker.Deduplication.SlidingWindow;

await using var dedupe = new SlidingWindowXxHashDedupe(
    window: TimeSpan.FromMinutes(5),
    rotationInterval: TimeSpan.FromSeconds(10),
    capacityHint: 100_000);

if (dedupe.TryMarkSeen("event:01J..."))
{
    // First observation inside the active window.
}
```

`TryMarkSeen` returns `true` when the hash was absent and adds it. A repeat returns `false` and refreshes that hash into the current time bucket, so it remains present until a full window passes without another observation.

Use `Contains` for a non-refreshing lookup and `TryRemove` to make a value immediately eligible again:

```csharp
bool recentlySeen = dedupe.Contains(eventId);
bool removed = dedupe.TryRemove(eventId);
```

## Window precision

Expiration is bucketed rather than per-item. The bucket count is `Ceiling(window / rotationInterval)`, with a minimum of two buckets. A key therefore expires on a rotation near the configured window boundary, not at an exact timestamp.

Choose a shorter rotation interval for finer expiry precision, at the cost of more buckets and more frequent cleanup. `capacityHint` sizes the internal dictionary initially; it is not a hard memory limit.

Repeated observations are queued so the implementation can preserve the latest bucket safely. Memory use therefore depends on both unique keys and observation rate; it is not simply eight bytes per unique hash.

## Hash representation

Original values are not retained. The string and `ReadOnlySpan<char>` APIs hash UTF-16 characters, while the `Utf8` APIs hash the supplied bytes. Use one representation consistently for mark, lookup, and removal.

Different inputs can theoretically share a 64-bit hash. Use exact keys when collision tolerance is unacceptable.

## Lifetime and scope

The instance owns a background rotation task and must be disposed. It is process-local: restarts lose history, and separate application instances do not share it. It is useful for best-effort suppression and reducing repeated work, but it is not an exactly-once or durable idempotency boundary.
