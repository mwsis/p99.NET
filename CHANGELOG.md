# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).


## [Unreleased]


## [0.1.0] - 2026-07-07

### Initial release

* .NET implementation of the p99 performance percentile histogram as `P99::Histogram`;
* 64-bucket logarithmic histogram with nanosecond precision;
* percentile queries (p50, p75, p90, p95, p99, p99.5, p99.9, p99.99, p99.999, p99.9999, and arbitrary floating-point percentiles);
* `Histogram` as an `unsafe struct` with embedded bucket storage (`fixed ulong _buckets[64]`) for cache-friendly, heap-free layout aligned with the C and Rust implementations;
* push APIs for event times (nanoseconds, microseconds, milliseconds, seconds) and durations;
* overflow tracking, min/max event values, and bucket inspection helpers;
* multi-targeting `net8.0` and `netstandard2.0` (NuGet package id: `p99`);
* xUnit test suite and QuickStart sample;
* BenchmarkDotNet benchmarks mirroring the Rust criterion suite;
* NuGet packaging with portable PDB symbol packages (`.snupkg`) and Source Link;
* GitHub Actions CI (Ubuntu, Windows, macOS) and release publishing workflow;
* BSD-3-Clause license;
* Synesis-standard build scripts (`build.sh`, `build.ps1`);
