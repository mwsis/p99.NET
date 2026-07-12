using System.Numerics;
using System.Text;

namespace P99;

/// <summary>
///  Low-cost performance percentile histogram using 64 logarithmic buckets.
/// </summary>
/// <remarks>
///  <para>
///   Tracks event durations with nanosecond precision across power-of-two
///   bucket spacing. The structure is compact, allocation-free, and suited
///   for high-frequency timing measurements.
///  </para>
///  <para>
///   This type is declared <c>unsafe</c> only because C# requires that
///   keyword when a struct embeds a <c>fixed</c> buffer. That keyword is
///   <strong>not</strong> used here to expose raw pointers, skip bounds
///   checks, or invoke undefined behaviour. The public API is entirely safe
///   to use.
///  </para>
///  <para>
///   The embedded <c>fixed ulong _buckets[64]</c> array guarantees a single
///   contiguous, cache-friendly layout (matching the C and Rust
///   implementations): no heap allocation for bucket storage, predictable
///   size, and fast in-place updates. <c>AllowUnsafeBlocks</c> in the
///   project exists solely to enable this performance-oriented layout.
///  </para>
/// </remarks>
public unsafe struct Histogram
{
    /// <summary>Number of logarithmic buckets in a histogram.</summary>
    public const int BucketCount = 64;

    private ulong       _eventCount;
    private ulong       _eventTimeTotal;
    private bool        _hasOverflowed;
    private ulong?      _minEventTime;
    private ulong?      _maxEventTime;
    private fixed ulong _buckets[BucketCount];

    /// <summary>
    ///  Clears the instance, resetting all values to the equivalent of a
    ///  newly constructed instance.
    /// </summary>
    public void Clear()
    {
        this = default;
    }

    /// <summary>Pushes an event with the given duration.</summary>
    /// <param name="duration">
    ///  The event duration. Nanoseconds are truncated to <see
    ///  cref="ulong"/>.
    /// </param>
    /// <returns>
    ///  <see langword="true"/> on success; otherwise <see
    ///  langword="false"/>.
    /// </returns>
    public bool PushEventDuration(TimeSpan duration)
    {
#if NET7_0_OR_GREATER

        return PushEventTimeNs((ulong)duration.TotalNanoseconds);
#else

        return PushEventTimeNs(checked((ulong)duration.Ticks * 100UL));
#endif
    }

    /// <summary>
    ///  Pushes an event with the given number of nanoseconds.
    /// </summary>
    public bool PushEventTimeNs(ulong timeInNs)
    {
        if (!TryAddNsToTotalAndUpdateMinMax(timeInNs))
        {
            return false;
        }

        _eventCount++;
        GetBucketRef(BucketIndex(timeInNs))++;
        return true;
    }

    /// <summary>
    ///  Pushes an event with the given number of microseconds.
    /// </summary>
    public bool PushEventTimeUs(ulong timeInUs)
    {
        if (!TryMultiply(timeInUs, 1_000, out ulong timeInNs))
        {
            _hasOverflowed = true;
            return false;
        }

        return PushEventTimeNs(timeInNs);
    }

    /// <summary>
    ///  Pushes an event with the given number of milliseconds.
    /// </summary>
    public bool PushEventTimeMs(ulong timeInMs)
    {
        if (!TryMultiply(timeInMs, 1_000_000, out ulong timeInNs))
        {
            _hasOverflowed = true;
            return false;
        }

        return PushEventTimeNs(timeInNs);
    }

    /// <summary>Pushes an event with the given number of seconds.</summary>
    public bool PushEventTimeS(ulong timeInS)
    {
        if (!TryMultiply(timeInS, 1_000_000_000, out ulong timeInNs))
        {
            _hasOverflowed = true;
            return false;
        }

        return PushEventTimeNs(timeInNs);
    }

    /// <summary>Returns the count of events in a specific bucket.</summary>
    public readonly ulong? BucketValue(int index) =>
        index < BucketCount ? GetBucket(index) : null;

    /// <summary>Returns the count of events in a specific bucket.</summary>
    public readonly ulong GetBucket(int index)
    {
        if ((uint)index >= BucketCount)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        return _buckets[index];
    }

    /// <summary>
    ///  Copies all bucket counts into <paramref name="destination"/>.
    /// </summary>
    public readonly void CopyBucketsTo(ulong[] destination)
    {
        if (destination is null)
        {
            throw new ArgumentNullException(nameof(destination));
        }
        if (destination.Length < BucketCount)
        {
            throw new ArgumentException($"Destination must have length at least {BucketCount}.", nameof(destination));
        }

        for (int i = 0; i < BucketCount; i++)
        {
            destination[i] = _buckets[i];
        }
    }

    /// <summary>Number of events counted.</summary>
    public readonly ulong EventCount => _eventCount;

    /// <summary>
    ///  Returns the total event time in nanoseconds, if no overflow
    ///  occurred.
    /// </summary>
    public readonly ulong? EventTimeTotal => _hasOverflowed ? null : _eventTimeTotal;

    /// <summary>
    ///  Returns the total event time in nanoseconds, regardless of whether
    ///  overflow has occurred.
    /// </summary>
    public readonly ulong EventTimeTotalRaw => _eventTimeTotal;

    /// <summary>Indicates whether overflow has occurred.</summary>
    public readonly bool HasOverflowed => _hasOverflowed;

    /// <summary>Returns the minimum event time observed, if any.</summary>
    public readonly ulong? MinEventTime => _minEventTime;

    /// <summary>Returns the maximum event time observed, if any.</summary>
    public readonly ulong? MaxEventTime => _maxEventTime;

    /// <summary>
    ///  Returns the approximated duration (in nanoseconds) at the given
    ///  percentile.
    /// </summary>
    /// <param name="percentile">
    ///  Desired percentile (e.g. 50.0 for p50); clamped to [0.0, 100.0].
    /// </param>
    public readonly ulong? ValueAtPercentile(double percentile)
    {
        if (_eventCount == 0)
        {
            return null;
        }

        double p = ClampPercentile(percentile);

        if (p <= 0.0)
        {
            return _minEventTime;
        }

        if (p >= 100.0)
        {
            return _maxEventTime;
        }

        double targetRank = _eventCount * (p / 100.0);
        ulong accumulated = 0;

        for (int i = 0; i < BucketCount; i++)
        {
            ulong count = _buckets[i];
            if (count == 0)
            {
                continue;
            }

            ulong prevAccumulated = accumulated;
            accumulated += count;

            if (accumulated >= targetRank)
            {
                (ulong lower, ulong upper) = BucketRange(i)!.Value;
                double targetOffset = targetRank - prevAccumulated;
                double rangeWidth = i == 63
                    ? (double)(ulong.MaxValue - lower)
                    : upper - lower;
                double fraction = targetOffset / count;
                double interpolated = lower + (rangeWidth * fraction);
                ulong value = (ulong)Math.Round(interpolated);
                return ClampToObservedRange(value);
            }
        }

        return _maxEventTime;
    }

    /// <summary>
    ///  Returns the approximated p50 duration in nanoseconds.
    /// </summary>
    public readonly ulong? ValueAtP50() => ValueAtTargetRank((_eventCount * 1UL) / 2);

    /// <summary>
    ///  Returns the approximated p75 duration in nanoseconds.
    /// </summary>
    public readonly ulong? ValueAtP75() => ValueAtTargetRank((_eventCount * 3UL) / 4);

    /// <summary>
    ///  Returns the approximated p90 duration in nanoseconds.
    /// </summary>
    public readonly ulong? ValueAtP90() => ValueAtTargetRank((_eventCount * 90UL) / 100);

    /// <summary>
    ///  Returns the approximated p95 duration in nanoseconds.
    /// </summary>
    public readonly ulong? ValueAtP95() => ValueAtTargetRank((_eventCount * 95UL) / 100);

    /// <summary>
    ///  Returns the approximated p99 duration in nanoseconds.
    /// </summary>
    public readonly ulong? ValueAtP99() => ValueAtTargetRank((_eventCount * 99UL) / 100);

    /// <summary>
    ///  Returns the approximated p99.5 duration in nanoseconds.
    /// </summary>
    public readonly ulong? ValueAtP99_5() => ValueAtTargetRank((_eventCount * 995UL) / 1_000);

    /// <summary>
    ///  Returns the approximated p99.9 duration in nanoseconds.
    /// </summary>
    public readonly ulong? ValueAtP99_9() => ValueAtTargetRank((_eventCount * 999UL) / 1_000);

    /// <summary>
    ///  Returns the approximated p99.99 duration in nanoseconds.
    /// </summary>
    public readonly ulong? ValueAtP99_99() => ValueAtTargetRank((_eventCount * 9_999UL) / 10_000);

    /// <summary>
    ///  Returns the approximated p99.999 duration in nanoseconds.
    /// </summary>
    public readonly ulong? ValueAtP99_999() => ValueAtTargetRank((_eventCount * 99_999UL) / 100_000);

    /// <summary>
    ///  Returns the approximated p99.9999 duration in nanoseconds.
    /// </summary>
    public readonly ulong? ValueAtP99_999_9() => ValueAtTargetRank((_eventCount * 999_999UL) / 1_000_000);

    /// <summary>
    ///  Calculates the bucket index for a given elapsed time in
    ///  nanoseconds.
    /// </summary>
    public static int BucketIndex(ulong timeInNs)
    {
        if (timeInNs <= 1)
        {
            return 0;
        }

#if NET5_0_OR_GREATER

        return 63 - BitOperations.LeadingZeroCount(timeInNs);
#else

        return FloorLog2(timeInNs);
#endif
    }

    /// <summary>
    ///  Returns the inclusive range of nanoseconds represented by the given
    ///  bucket index.
    /// </summary>
    public static (ulong Lower, ulong Upper)? BucketRange(int index)
    {
        if (index >= BucketCount)
        {
            return null;
        }

        if (index == 0)
        {
            return (0, 1);
        }

        ulong lower = 1UL << index;
        ulong upper = index == 63 ? ulong.MaxValue : (1UL << (index + 1)) - 1;
        return (lower, upper);
    }

    /// <inheritdoc />
    public override readonly string ToString() => ToString(compact: true);

    /// <summary>Returns a debug representation of the histogram.</summary>
    public readonly string ToString(bool compact)
    {
        StringBuilder builder = new();
        builder.Append("Histogram { ");
        if (compact)
        {
            builder.Append("n: ").Append(_eventCount);
            builder.Append(", ∑: ").Append(EventTimeTotal);
            builder.Append(", ∞: ").Append(_hasOverflowed);
            builder.Append(", ↓: ").Append(FormatOptional(_minEventTime));
            builder.Append(", ↑: ").Append(FormatOptional(_maxEventTime));
            builder.Append(", b: ");
            AppendBuckets(builder, powerOfTwoKeys: false);
        }
        else
        {
            builder.Append("event_count: ").Append(_eventCount);
            builder.Append(", event_time_total: ").Append(EventTimeTotal);
            builder.Append(", has_overflowed: ").Append(_hasOverflowed);
            builder.Append(", min_event_time: ").Append(FormatOptional(_minEventTime));
            builder.Append(", max_event_time: ").Append(FormatOptional(_maxEventTime));
            builder.Append(", buckets: ");
            AppendBuckets(builder, powerOfTwoKeys: true);
        }

        builder.Append(" }");
        return builder.ToString();
    }

    private readonly ulong? ValueAtTargetRank(ulong targetRank)
    {
        if (_eventCount == 0)
        {
            return null;
        }

        ulong accumulated = 0;

        for (int i = 0; i < BucketCount; i++)
        {
            ulong count = _buckets[i];
            if (count == 0)
            {
                continue;
            }

            ulong prevAccumulated = accumulated;
            accumulated += count;

            if (accumulated >= targetRank)
            {
                (ulong lower, ulong upper) = BucketRange(i)!.Value;
                ulong targetOffset = targetRank - prevAccumulated;
                ulong interpolated = targetOffset == 0
                    ? lower
                    : Interpolate(lower, upper, i, count, targetOffset);

                return ClampToObservedRange(interpolated);
            }
        }

        return _maxEventTime;
    }

    private static ulong Interpolate(ulong lower, ulong upper, int bucketIndex, ulong count, ulong targetOffset)
    {
        ulong rangeWidth = bucketIndex == 63 ? ulong.MaxValue - lower : upper - lower;

        if (rangeWidth <= ulong.MaxValue / targetOffset)
        {
            return lower + (rangeWidth * targetOffset) / count;
        }

#if NET7_0_OR_GREATER

        return lower + (ulong)(((UInt128)rangeWidth * targetOffset) / count);
#else

        BigInteger numerator = BigInteger.Multiply(rangeWidth, targetOffset);
        return lower + (ulong)(numerator / count);
#endif
    }

    private readonly ulong ClampToObservedRange(ulong value)
    {
        if (_minEventTime is ulong min && value < min)
        {
            value = min;
        }

        if (_maxEventTime is ulong max && value > max)
        {
            value = max;
        }

        return value;
    }

    private bool TryAddNsToTotalAndUpdateMinMax(ulong timeInNs)
    {
        if (_hasOverflowed)
        {
            return false;
        }

        if (!TryAdd(_eventTimeTotal, timeInNs, out ulong newTotal))
        {
            _hasOverflowed = true;
            return false;
        }

        _eventTimeTotal = newTotal;

        if (_minEventTime is ulong minEventTime)
        {
            if (timeInNs < minEventTime)
            {
                _minEventTime = timeInNs;
            }
        }
        else
        {
            _minEventTime = timeInNs;
        }

        if (_maxEventTime is ulong maxEventTime)
        {
            if (timeInNs > maxEventTime)
            {
                _maxEventTime = timeInNs;
            }
        }
        else
        {
            _maxEventTime = timeInNs;
        }

        return true;
    }

    private ref ulong GetBucketRef(int index) => ref _buckets[index];

    private readonly void AppendBuckets(StringBuilder builder, bool powerOfTwoKeys)
    {
        builder.Append('{');
        bool first = true;
        for (int i = 0; i < BucketCount; i++)
        {
            ulong count = _buckets[i];
            if (count == 0)
            {
                continue;
            }

            if (!first)
            {
                builder.Append(", ");
            }

            first = false;
            if (powerOfTwoKeys)
            {
                builder.Append("\"2^").Append(i).Append("\": ").Append(count);
            }
            else
            {
                builder.Append(i).Append(": ").Append(count);
            }
        }

        builder.Append('}');
    }

    private static string FormatOptional(ulong? value) =>
        value is ulong v ? v.ToString() : "None";

    private static double ClampPercentile(double value)
    {
        if (value < 0.0)
        {
            return 0.0;
        }

        if (value > 100.0)
        {
            return 100.0;
        }

        return value;
    }

    private static bool TryAdd(ulong left, ulong right, out ulong result)
    {
        result = left + right;
        return result >= left;
    }

    private static bool TryMultiply(ulong left, ulong right, out ulong result)
    {
        if (left == 0 || right == 0)
        {
            result = 0;
            return true;
        }

        if (left > ulong.MaxValue / right)
        {
            result = 0;
            return false;
        }

        result = left * right;
        return true;
    }

#if !NET5_0_OR_GREATER

    private static int FloorLog2(ulong value)
    {
        int result = 0;
        if (value >= 1UL << 32) { value >>= 32; result += 32; }
        if (value >= 1UL << 16) { value >>= 16; result += 16; }
        if (value >= 1UL << 8) { value >>= 8; result += 8; }
        if (value >= 1UL << 4) { value >>= 4; result += 4; }
        if (value >= 1UL << 2) { value >>= 2; result += 2; }
        if (value >= 1UL << 1) { result += 1; }
        return result;
    }
#endif
}
