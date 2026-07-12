# p99.NET <!-- omit in toc -->

![Language](https://img.shields.io/badge/.NET-512BD4?style=flat&logo=dotnet&logoColor=white)
[![License](https://img.shields.io/badge/License-BSD_3--Clause-blue.svg)](https://opensource.org/licenses/BSD-3-Clause)
[![GitHub release](https://img.shields.io/github/v/release/synesissoftware/p99.NET.svg)](https://github.com/synesissoftware/p99.NET/releases/latest)
[![Last Commit](https://img.shields.io/github/last-commit/synesissoftware/p99.NET)](https://github.com/synesissoftware/p99.NET/commits/master)
[![CI](https://github.com/synesissoftware/p99.NET/actions/workflows/ci.yml/badge.svg)](https://github.com/synesissoftware/p99.NET/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/p99.svg)](https://www.nuget.org/packages/p99/)
![TFM](https://img.shields.io/badge/TFM-net8.0%20%7C%20netstandard2.0-lightgrey)

Low-cost generation of performance percentiles (p50, p90, p99, p99.9, etc.) for
cross-platform **.NET** workloads.


## Table of Contents <!-- omit in toc -->

- [Introduction](#introduction)
- [Installation](#installation)
- [Quick start](#quick-start)
- [Platform support](#platform-support)
- [Design notes](#design-notes)
- [Repository layout](#repository-layout)
- [Building from source](#building-from-source)
	- [Pack locally](#pack-locally)
- [Benchmarks](#benchmarks)
- [Packaging and distribution](#packaging-and-distribution)
- [Project information](#project-information)
	- [Where to get help](#where-to-get-help)
	- [Contribution guidelines](#contribution-guidelines)
	- [Related projects](#related-projects)
	- [License](#license)


## Introduction

**p99** is a lightweight, low-overhead library designed for generating
real-time performance percentiles in high-frequency or latency-sensitive
environments.

**p99.NET** is the **.NET** implementation. Companion implementations are
available as [p99](https://github.com/synesissoftware/p99) (**C**) and
[p99.Rust](https://github.com/synesissoftware/p99.Rust) (**Rust**).


## Installation

```bash
dotnet add package p99
```


## Quick start

```csharp
using P99;

Histogram histogram = default;

histogram.PushEventTimeNs(120);
histogram.PushEventTimeNs(450);
histogram.PushEventTimeNs(980);

Console.WriteLine($"p50: {histogram.ValueAtP50()} ns");
Console.WriteLine($"p99: {histogram.ValueAtP99()} ns");
```

See [`samples/P99.QuickStart`](samples/P99.QuickStart) for a runnable example.


## Platform support

The library multi-targets:

| Target | Rationale |
|--------|-----------|
| `net8.0` | Modern .NET on desktop, server, cloud, mobile (MAUI), and IoT |
| `netstandard2.0` | Broad compatibility with .NET Framework 4.6.1+, Xamarin, Unity, and constrained runtimes |

NuGet selects the best-matching assembly for each consumer automatically.


## Design notes

`Histogram` is declared `unsafe` in C# _**only**_ to embed its 64 bucket
counts in a single contiguous block (`fixed ulong _buckets[64]`). That
matches the C and Rust layouts: no heap allocation for buckets, predictable
size, and cache-friendly access. It is _**not**_ used due to any use of raw
pointers, unchecked memory, or any behaviour that callers need to treat as
dangerous — the public API is fully safe.

The project sets `AllowUnsafeBlocks` solely for this layout; consumers do not
need to enable unsafe code in their own projects.


## Repository layout

```
p99.NET/
├── src/P99/              # Main library (NuGet package: p99)
├── tests/P99.Tests/      # Unit tests (xUnit)
├── benchmarks/           # Performance benchmarks (BenchmarkDotNet)
├── samples/              # Consumer examples
├── .github/workflows/    # CI and release automation
├── Directory.Build.props   # Shared build and Source Link settings
├── Directory.Packages.props
├── global.json             # Pinned SDK version
├── build.sh / build.ps1    # Local build, test, and pack scripts
└── p99.NET.sln
```


## Building from source

Requires the [.NET SDK](https://dotnet.microsoft.com/download) version specified in [`global.json`](global.json).

```bash
git clone https://github.com/synesissoftware/p99.NET.git
cd p99.NET
dotnet restore
dotnet build
dotnet test
```

### Pack locally

```bash
dotnet pack src/P99/P99.csproj --configuration Release --output artifacts/packages
```

Or:

```bash
./build.sh
```


## Benchmarks

Performance benchmarks use [BenchmarkDotNet](https://benchmarkdotnet.org/),
mirroring the **p99.Rust** criterion suite:

```bash
dotnet run --project benchmarks/P99.Benchmarks --configuration Release -- --filter '*'
```

Run a subset:

```bash
dotnet run --project benchmarks/P99.Benchmarks -c Release -- --filter '*PushEventTimeNs*'
```

Use `--job short` for a quicker local run:

```bash
dotnet run --project benchmarks/P99.Benchmarks -c Release -- --job short --filter '*'
```


## Packaging and distribution

This project is designed for adoption as a NuGet package:

- **SDK-style project** with `dotnet pack` output;
- **Package metadata** (`PackageId`, license expression, readme, tags);
- **Symbol packages** (`.snupkg`) for debugging;
- **Source Link** for stepping into source from consuming projects;
- **CI** builds on Windows, Linux, and macOS;
- **Release workflow** publishes to NuGet.org on GitHub Release;


## Project information

### Where to get help

- [GitHub repository](https://github.com/synesissoftware/p99.NET);
- [Issue tracker](https://github.com/synesissoftware/p99.NET/issues);
- [NuGet package](https://www.nuget.org/packages/p99/);


### Contribution guidelines

See [CONTRIBUTING.md](CONTRIBUTING.md). Defect reports, feature requests, and
pull requests are welcome.


### Related projects

- [p99](https://github.com/synesissoftware/p99) — **C** implementation;
- [p99.Python](https://github.com/synesissoftware/p99.Python) — **Python** implementation;
- [p99.Rust](https://github.com/synesissoftware/p99.Rust) — **Rust** implementation;


### License

This project is licensed under the 3-clause BSD license. See [LICENSE](LICENSE).
