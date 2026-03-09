[![](https://img.shields.io/nuget/v/soenneker.deduplication.slidingwindow.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.deduplication.slidingwindow/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.deduplication.slidingwindow/publish-package.yml?style=for-the-badge)](https://github.com/soenneker/soenneker.deduplication.slidingwindow/actions/workflows/publish-package.yml)
[![](https://img.shields.io/nuget/dt/soenneker.deduplication.slidingwindow.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.deduplication.slidingwindow/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.deduplication.slidingwindow/codeql.yml?label=CodeQL&style=for-the-badge)](https://github.com/soenneker/soenneker.deduplication.slidingwindow/actions/workflows/codeql.yml)

# ![](https://user-images.githubusercontent.com/4441470/224455560-91ed3ee7-f510-4041-a8d2-3fc093025112.png) Soenneker.Deduplication.SlidingWindow

### High-performance sliding-window deduplication for .NET.

## Installation

```bash
dotnet add package Soenneker.Deduplication.SlidingWindow
```

---

# Overview

`Soenneker.Deduplication.SlidingWindow` provides a **thread-safe sliding time window deduplication utility** designed for extremely high throughput workloads.

It allows you to efficiently determine whether a value has been **seen recently within a time window** without storing the original input values.

Inputs are hashed using **XXH3 (XxHash3)** and only the resulting `ulong` is stored internally, keeping memory usage low while maintaining high performance.

Typical usage pattern:

* First time value appears → **`TryMarkSeen()` returns `true`**
* Value appears again within the window → **returns `false`**
* Value appears after the window expires → **returns `true` again**

---

# Key Features

* **Sliding time window deduplication**
* **Thread-safe concurrent access**
* **High-throughput design**
* **Allocation-free span APIs**
* **XXH3 hashing for speed**
* **UTF8 + UTF16 support**
* **Optional hashing seed**
* **Async disposal support**

Internally it uses a **bucketed concurrent set with rotating expiration**, allowing expired entries to fall out automatically as the window advances.

---

# Quick Start

```csharp
using Soenneker.Deduplication.SlidingWindow;

var dedupe = new SlidingWindowXxHashDedupe(
    window: TimeSpan.FromMinutes(5),
    rotationInterval: TimeSpan.FromSeconds(10)
);

if (dedupe.TryMarkSeen("user:123"))
{
    // First occurrence in the last 5 minutes
}
else
{
    // Duplicate within the window
}
```

After the window expires, the same value will again return `true`.

---

# API

## TryMarkSeen

Checks if the value was seen recently and records it if not.

```csharp
bool added = dedupe.TryMarkSeen("value");
bool added2 = dedupe.TryMarkSeen("value".AsSpan());
bool added3 = dedupe.TryMarkSeenUtf8(utf8Bytes);
```

Return value:

| Result  | Meaning                                        |
| ------- | ---------------------------------------------- |
| `true`  | Value was not seen recently and was added      |
| `false` | Value already exists within the sliding window |

---

## Contains

Checks if a value exists within the current window.

```csharp
bool exists = dedupe.Contains("value");
bool exists2 = dedupe.Contains("value".AsSpan());
bool exists3 = dedupe.ContainsUtf8(utf8Bytes);
```

These methods are **pure checks** and do not modify the set.

---

## TryRemove

Manually removes a value if present.

```csharp
bool removed = dedupe.TryRemove("value");
bool removed2 = dedupe.TryRemove("value".AsSpan());
bool removed3 = dedupe.TryRemoveUtf8(utf8Bytes);
```

---

## Count

Approximate number of items currently in the window.

```csharp
int count = dedupe.Count;
```

This value is intended for **diagnostics/monitoring**, not strict accounting.

---

# Configuration

```csharp
var dedupe = new SlidingWindowXxHashDedupe(
    window: TimeSpan.FromMinutes(10),
    rotationInterval: TimeSpan.FromSeconds(30),
    capacityHint: 100_000,
    seed: 12345
);
```

| Parameter          | Description                              |
| ------------------ | ---------------------------------------- |
| `window`           | Total deduplication duration             |
| `rotationInterval` | How frequently buckets rotate            |
| `capacityHint`     | Optional size hint to reduce resizing    |
| `seed`             | Optional XXH3 seed for hash partitioning |

### Window behavior

The sliding window works by **rotating buckets** at the specified `rotationInterval`.

Example:

```
window = 10 minutes
rotationInterval = 30 seconds
```

Results in ~20 rotating buckets.

Expired buckets are automatically cleared as the window advances.

---

# Memory Efficiency

Values are stored as **64-bit hashes** instead of full strings.

Example approximate memory usage:

| Entries | Approx Memory |
| ------- | ------------- |
| 1,000   | ~8 KB         |
| 10,000  | ~80 KB        |
| 100,000 | ~800 KB       |

Actual usage depends on dictionary overhead.

---

# Hashing & Collisions

Inputs are deduplicated using **64-bit XXH3 hashes**.

This provides extremely fast hashing with a very low collision probability.

However, collisions are theoretically possible since only hashes are stored.
For most event deduplication, ingestion pipelines, and telemetry scenarios, this is more than sufficient.

---

# Disposal

`SlidingWindowXxHashDedupe` maintains an internal background rotation timer and therefore supports disposal.

```csharp
dedupe.Dispose();
```

or

```csharp
await dedupe.DisposeAsync();
```

Disposing stops the internal rotation loop and releases resources.

---

# When to Use

Ideal for:

* Event stream deduplication
* Message processing pipelines
* API request suppression
* Preventing duplicate webhook processing
* Temporary ID or phone number dedupe
* High-volume ingestion systems

---

# When Not to Use

Not recommended if:

* You require **permanent deduplication**
* You need **exact storage of original values**
* Collision risk must be absolutely zero
