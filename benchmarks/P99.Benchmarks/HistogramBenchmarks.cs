using BenchmarkDotNet.Attributes;
using P99;

namespace P99.Benchmarks;

[MemoryDiagnoser]
public class HistogramBucketIndexBenchmarks
{
    [Benchmark(Description = "Histogram.BucketIndex(1)")]
    public int BucketIndex_Small() => Histogram.BucketIndex(1);

    [Benchmark(Description = "Histogram.BucketIndex(ulong.MaxValue)")]
    public int BucketIndex_Large() => Histogram.BucketIndex(ulong.MaxValue);
}

[MemoryDiagnoser]
public class HistogramMutationBenchmarks
{
    [Benchmark(Description = "Histogram.PushEventTimeNs()")]
    public bool PushEventTimeNs()
    {
        Histogram histogram = default;
        return histogram.PushEventTimeNs(12_345);
    }

    [Benchmark(Description = "Histogram.Clear()")]
    public void Clear()
    {
        Histogram histogram = default;
        histogram.PushEventTimeNs(100);
        histogram.PushEventTimeNs(200);
        histogram.Clear();
    }
}

[MemoryDiagnoser]
public class HistogramPercentileBenchmarks
{
    private Histogram _sequential;
    private Histogram _wideRange;

    [GlobalSetup]
    public void Setup()
    {
        _sequential = BuildSequentialHistogram();
        _wideRange = BuildWideRangeHistogram();
    }

    [Benchmark(Description = "ValueAtPercentile(99.0) [100k sequential]")]
    public ulong? ValueAtPercentile_P99_Sequential() =>
        _sequential.ValueAtPercentile(99.0);

    [Benchmark(Description = "ValueAtP99() [100k sequential]")]
    public ulong? ValueAtP99_Sequential() =>
        _sequential.ValueAtP99();

    [Benchmark(Description = "ValueAtPercentile(50.0) [100k wide-range]")]
    public ulong? ValueAtPercentile_P50_WideRange() =>
        _wideRange.ValueAtPercentile(50.0);

    [Benchmark(Description = "ValueAtP50() [100k wide-range]")]
    public ulong? ValueAtP50_WideRange() =>
        _wideRange.ValueAtP50();

    [Benchmark(Description = "ValueAtPercentile(75.0) [100k wide-range]")]
    public ulong? ValueAtPercentile_P75_WideRange() =>
        _wideRange.ValueAtPercentile(75.0);

    [Benchmark(Description = "ValueAtP75() [100k wide-range]")]
    public ulong? ValueAtP75_WideRange() =>
        _wideRange.ValueAtP75();

    [Benchmark(Description = "ValueAtPercentile(90.0) [100k wide-range]")]
    public ulong? ValueAtPercentile_P90_WideRange() =>
        _wideRange.ValueAtPercentile(90.0);

    [Benchmark(Description = "ValueAtP90() [100k wide-range]")]
    public ulong? ValueAtP90_WideRange() =>
        _wideRange.ValueAtP90();

    [Benchmark(Description = "ValueAtPercentile(99.0) [100k wide-range]")]
    public ulong? ValueAtPercentile_P99_WideRange() =>
        _wideRange.ValueAtPercentile(99.0);

    [Benchmark(Description = "ValueAtP99() [100k wide-range]")]
    public ulong? ValueAtP99_WideRange() =>
        _wideRange.ValueAtP99();

    [Benchmark(Description = "ValueAtPercentile(99.99) [100k wide-range]")]
    public ulong? ValueAtPercentile_P99_99_WideRange() =>
        _wideRange.ValueAtPercentile(99.99);

    [Benchmark(Description = "ValueAtP99_99() [100k wide-range]")]
    public ulong? ValueAtP99_99_WideRange() =>
        _wideRange.ValueAtP99_99();

    private static Histogram BuildSequentialHistogram()
    {
        Histogram histogram = default;
        for (int i = 1; i <= 100_000; i++)
        {
            histogram.PushEventTimeNs((ulong)i * 10);
        }

        return histogram;
    }

    private static Histogram BuildWideRangeHistogram()
    {
        Histogram histogram = default;
        ulong state = 12_345;
        for (int i = 0; i < 100_000; i++)
        {
            state = unchecked(state * 6_364_136_223_846_793_005 + 1);
            ulong value = (state % 10_000_000_000) + 1;
            histogram.PushEventTimeNs(value);
        }

        return histogram;
    }
}
