using P99;

Histogram histogram = default;

for (int i = 0; i < 10_000; i++)
{
    histogram.PushEventTimeNs((ulong)((i * i) % 1_000_000));
}

Console.WriteLine($"events: {histogram.EventCount}");
Console.WriteLine($"min (ns): {histogram.MinEventTime}");
Console.WriteLine($"max (ns): {histogram.MaxEventTime}");
Console.WriteLine($"p50 (ns): {histogram.ValueAtP50()}");
Console.WriteLine($"p90 (ns): {histogram.ValueAtP90()}");
Console.WriteLine($"p99 (ns): {histogram.ValueAtP99()}");
Console.WriteLine($"p99.9 (ns): {histogram.ValueAtP99_9()}");
