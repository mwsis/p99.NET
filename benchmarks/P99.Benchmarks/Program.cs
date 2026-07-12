using BenchmarkDotNet.Running;
using P99.Benchmarks;

BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
