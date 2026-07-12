using Xunit;

namespace P99.Tests;

public sealed class HistogramTests
{
    private static void AssertScalarEqApprox(double expected, double actual, double toleranceFraction)
    {
        double tolerance = Math.Max(Math.Abs(expected), Math.Abs(actual)) * toleranceFraction;
        Assert.True(Math.Abs(expected - actual) <= tolerance);
    }

    [Fact]
    public void TEST_Histogram_ToString_empty()
    {
        Histogram h = default;
        const string expected = "Histogram { n: 0, ∑: 0, ∞: False, ↓: None, ↑: None, b: {} }";
        Assert.Equal(expected, h.ToString());
    }

    [Fact]
    public void TEST_Histogram_ToString_populated()
    {
        Histogram h = default;
        const string expected = "Histogram { n: 3, ∑: 500, ∞: False, ↓: 100, ↑: 200, b: {6: 1, 7: 2} }";

        Assert.True(h.PushEventTimeNs(100));
        Assert.True(h.PushEventTimeNs(200));
        Assert.True(h.PushEventTimeNs(200));

        Assert.Equal(expected, h.ToString());
    }

    [Fact]
    public void TEST_Histogram_ToString_alternate_empty()
    {
        Histogram h = default;
        const string expected = "Histogram { event_count: 0, event_time_total: 0, has_overflowed: False, min_event_time: None, max_event_time: None, buckets: {} }";
        Assert.Equal(expected, h.ToString(compact: false));
    }

    [Fact]
    public void TEST_Histogram_ToString_alternate_populated()
    {
        Histogram h = default;

        Assert.True(h.PushEventTimeNs(100));
        Assert.True(h.PushEventTimeNs(10_000));
        Assert.True(h.PushEventTimeNs(10_001));

        const string expected = "Histogram { event_count: 3, event_time_total: 20101, has_overflowed: False, min_event_time: 100, max_event_time: 10001, buckets: {\"2^6\": 1, \"2^13\": 2} }";
        Assert.Equal(expected, h.ToString(compact: false));
    }

    [Fact]
    public void TEST_Histogram_Default()
    {
        Histogram h = default;

        Assert.Equal(0UL, h.EventCount);
        Assert.Equal(0UL, h.EventTimeTotal);
        Assert.Equal(0UL, h.EventTimeTotalRaw);
        Assert.False(h.HasOverflowed);
        Assert.Null(h.MinEventTime);
        Assert.Null(h.MaxEventTime);

        for (int i = 0; i < Histogram.BucketCount; i++)
        {
            Assert.Equal(0UL, h.GetBucket(i));
            Assert.Equal(0UL, h.BucketValue(i));
        }

        Assert.Null(h.BucketValue(64));
    }

    [Fact]
    public void TEST_Histogram_bucket_index()
    {
        Assert.Equal(0, Histogram.BucketIndex(0));
        Assert.Equal(0, Histogram.BucketIndex(1));
        Assert.Equal(1, Histogram.BucketIndex(2));
        Assert.Equal(1, Histogram.BucketIndex(3));
        Assert.Equal(2, Histogram.BucketIndex(4));
        Assert.Equal(2, Histogram.BucketIndex(7));
        Assert.Equal(3, Histogram.BucketIndex(8));
        Assert.Equal(3, Histogram.BucketIndex(15));
        Assert.Equal(4, Histogram.BucketIndex(16));
        Assert.Equal(4, Histogram.BucketIndex(31));
        Assert.Equal(10, Histogram.BucketIndex(1024));
        Assert.Equal(10, Histogram.BucketIndex(2047));
        Assert.Equal(63, Histogram.BucketIndex(1UL << 63));
        Assert.Equal(63, Histogram.BucketIndex(ulong.MaxValue));
    }

    [Fact]
    public void TEST_Histogram_bucket_range()
    {
        Assert.Equal((0UL, 1UL), Histogram.BucketRange(0));
        Assert.Equal((2UL, 3UL), Histogram.BucketRange(1));
        Assert.Equal((4UL, 7UL), Histogram.BucketRange(2));
        Assert.Equal((8UL, 15UL), Histogram.BucketRange(3));
        Assert.Equal((16UL, 31UL), Histogram.BucketRange(4));
        Assert.Equal((1024UL, 2047UL), Histogram.BucketRange(10));
        Assert.Equal((1UL << 63, ulong.MaxValue), Histogram.BucketRange(63));
        Assert.Null(Histogram.BucketRange(64));
    }

    [Fact]
    public void TEST_Histogram_PUSH_EVENTS()
    {
        Histogram h = default;

        Assert.True(h.PushEventTimeNs(1));
        Assert.True(h.PushEventTimeNs(3));
        Assert.True(h.PushEventTimeUs(10));
        Assert.True(h.PushEventTimeMs(5));
        Assert.True(h.PushEventTimeS(2));
        Assert.True(h.PushEventTimeNs(100));

        Assert.Equal(6UL, h.EventCount);
        Assert.False(h.HasOverflowed);
        Assert.Equal(1UL, h.MinEventTime);
        Assert.Equal(2_000_000_000UL, h.MaxEventTime);
        Assert.Equal(2_005_010_104UL, h.EventTimeTotal);

        Assert.Equal(1UL, h.GetBucket(0));
        Assert.Equal(1UL, h.GetBucket(1));
        Assert.Equal(1UL, h.GetBucket(6));
        Assert.Equal(1UL, h.GetBucket(13));
        Assert.Equal(1UL, h.GetBucket(22));
        Assert.Equal(1UL, h.GetBucket(30));

        h.Clear();

        Assert.Equal(0UL, h.EventCount);
        Assert.Equal(0UL, h.EventTimeTotal);
    }

    [Fact]
    public void TEST_Histogram_OVERFLOW()
    {
        Histogram h = default;
        Assert.True(h.PushEventTimeNs(ulong.MaxValue));
        Assert.Equal(ulong.MaxValue, h.EventTimeTotal);
        Assert.False(h.HasOverflowed);

        Assert.False(h.PushEventTimeNs(1));
        Assert.True(h.HasOverflowed);
        Assert.Null(h.EventTimeTotal);
        Assert.Equal(ulong.MaxValue, h.EventTimeTotalRaw);
    }

    [Fact]
    public void TEST_Histogram_PERCENTILES_EMPTY()
    {
        Histogram h = default;

        Assert.Null(h.ValueAtPercentile(50.0));
        Assert.Null(h.ValueAtP50());
        Assert.Null(h.ValueAtP99());
    }

    [Fact]
    public void TEST_Histogram_PERCENTILES_SINGLE_EVENT()
    {
        Histogram h = default;
        Assert.True(h.PushEventTimeNs(100));

        Assert.Equal(100UL, h.ValueAtPercentile(0.0));
        Assert.Equal(100UL, h.ValueAtPercentile(50.0));
        Assert.Equal(100UL, h.ValueAtPercentile(99.0));
        Assert.Equal(100UL, h.ValueAtPercentile(100.0));
        Assert.Equal(100UL, h.ValueAtP50());
        Assert.Equal(100UL, h.ValueAtP90());
        Assert.Equal(100UL, h.ValueAtP99());
        Assert.Equal(100UL, h.ValueAtP99_999_9());
    }

    [Fact]
    public void TEST_Histogram_PERCENTILES_INTERPOLATION()
    {
        Histogram h = default;
        Assert.True(h.PushEventTimeNs(100));
        Assert.True(h.PushEventTimeNs(200));

        ulong p50 = h.ValueAtPercentile(50.0)!.Value;
        ulong p99 = h.ValueAtPercentile(99.0)!.Value;

        Assert.InRange<ulong>(p50, 100, 200);
        Assert.InRange<ulong>(p99, 100, 200);
        Assert.Equal(100UL, h.ValueAtPercentile(0.0));
        Assert.Equal(200UL, h.ValueAtPercentile(100.0));
        Assert.InRange<ulong>(h.ValueAtP50()!.Value, 100, 200);
        Assert.InRange<ulong>(h.ValueAtP99()!.Value, 100, 200);
    }

    [Fact]
    public void TEST_Histogram_PERCENTILES_WIDE_RANGE()
    {
        Histogram h = default;
        ulong[] values =
        [
            1,
            10,
            100,
            1_000,
            10_000,
            100_000,
            1_000_000,
            10_000_000,
            100_000_000,
            1_000_000_000,
            10_000_000_000,
        ];

        foreach (ulong v in values)
        {
            Assert.True(h.PushEventTimeNs(v));
        }

        Assert.Equal((ulong)values.Length, h.EventCount);
        Assert.Equal(1UL, h.MinEventTime);
        Assert.Equal(10_000_000_000UL, h.MaxEventTime);

        ulong p50 = h.ValueAtP50()!.Value;
        ulong p75 = h.ValueAtP75()!.Value;
        ulong p90 = h.ValueAtP90()!.Value;
        ulong p95 = h.ValueAtP95()!.Value;
        ulong p99 = h.ValueAtP99()!.Value;
        ulong p99_5 = h.ValueAtP99_5()!.Value;
        ulong p99_9 = h.ValueAtP99_9()!.Value;
        ulong p99_99 = h.ValueAtP99_99()!.Value;
        ulong p99_999 = h.ValueAtP99_999()!.Value;
        ulong p99_999_9 = h.ValueAtP99_999_9()!.Value;

        Assert.True(p50 <= p75);
        Assert.True(p75 <= p90);
        Assert.True(p90 <= p95);
        Assert.True(p95 <= p99);
        Assert.True(p99 <= p99_5);
        Assert.True(p99_5 <= p99_9);
        Assert.True(p99_9 <= p99_99);
        Assert.True(p99_99 <= p99_999);
        Assert.True(p99_999 <= p99_999_9);
        Assert.True(p50 >= 1);
        Assert.True(p99_999_9 <= 10_000_000_000);
    }

    [Fact]
    public void TEST_Histogram_PERCENTILES_MANY_EVENTS()
    {
        Histogram h = default;
        const int count = 100_000;

        for (int i = 1; i <= count; i++)
        {
            Assert.True(h.PushEventTimeNs((ulong)i));
        }

        Assert.Equal((ulong)count, h.EventCount);
        Assert.Equal(1UL, h.MinEventTime);
        Assert.Equal((ulong)count, h.MaxEventTime);

        ulong p50 = h.ValueAtP50()!.Value;
        ulong p90 = h.ValueAtP90()!.Value;
        ulong p99 = h.ValueAtP99()!.Value;
        ulong p99_9 = h.ValueAtP99_9()!.Value;

        Assert.Equal(50_000UL, p50);
        Assert.Equal(100_000UL, p90);
        Assert.Equal(100_000UL, p99);
        Assert.Equal(100_000UL, p99_9);

        ulong p75 = h.ValueAtP75()!.Value;
        ulong p95 = h.ValueAtP95()!.Value;
        ulong p99_5 = h.ValueAtP99_5()!.Value;
        ulong p99_99 = h.ValueAtP99_99()!.Value;
        ulong p99_999 = h.ValueAtP99_999()!.Value;
        ulong p99_999_9 = h.ValueAtP99_999_9()!.Value;

        Assert.True(p50 <= p75);
        Assert.True(p75 <= p90);
        Assert.True(p90 <= p95);
        Assert.True(p95 <= p99);
        Assert.True(p99 <= p99_5);
        Assert.True(p99_5 <= p99_9);
        Assert.True(p99_9 <= p99_99);
        Assert.True(p99_99 <= p99_999);
        Assert.True(p99_999 <= p99_999_9);
    }

    [Fact]
    public void TEST_Histogram_COMPARE_FLOAT_AND_INT_PERCENTILES()
    {
        Histogram h = default;

        for (int i = 1; i <= 10_000; i++)
        {
            ulong val = (ulong)((i * i) % 1_000_000);
            Assert.True(h.PushEventTimeNs(val));
        }

        AssertScalarEqApprox(h.ValueAtPercentile(50.0)!.Value, h.ValueAtP50()!.Value, 0.01);
        AssertScalarEqApprox(h.ValueAtPercentile(75.0)!.Value, h.ValueAtP75()!.Value, 0.01);
        AssertScalarEqApprox(h.ValueAtPercentile(90.0)!.Value, h.ValueAtP90()!.Value, 0.01);
        AssertScalarEqApprox(h.ValueAtPercentile(95.0)!.Value, h.ValueAtP95()!.Value, 0.01);
        AssertScalarEqApprox(h.ValueAtPercentile(99.0)!.Value, h.ValueAtP99()!.Value, 0.01);
        AssertScalarEqApprox(h.ValueAtPercentile(99.5)!.Value, h.ValueAtP99_5()!.Value, 0.01);
        AssertScalarEqApprox(h.ValueAtPercentile(99.9)!.Value, h.ValueAtP99_9()!.Value, 0.01);
        AssertScalarEqApprox(h.ValueAtPercentile(99.99)!.Value, h.ValueAtP99_99()!.Value, 0.01);
        AssertScalarEqApprox(h.ValueAtPercentile(99.999)!.Value, h.ValueAtP99_999()!.Value, 0.01);
        AssertScalarEqApprox(h.ValueAtPercentile(99.9999)!.Value, h.ValueAtP99_999_9()!.Value, 0.01);
    }
}
