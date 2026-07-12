# Contributing to p99.NET <!-- omit in toc -->

Thank you for your interest in contributing.


## Table of Contents <!-- omit in toc -->

- [Code of conduct](#code-of-conduct)
- [Reporting issues](#reporting-issues)
- [Getting started](#getting-started)
- [Project layout](#project-layout)
- [Coding standards](#coding-standards)
- [Documentation](#documentation)
- [Pull requests](#pull-requests)
- [Releases](#releases)
- [License](#license)


## Code of conduct

Be respectful and constructive. Defect reports, feature requests, and pull
requests are welcome on [GitHub](https://github.com/synesissoftware/p99.NET).


## Reporting issues

Use [GitHub Issues](https://github.com/synesissoftware/p99.NET/issues). Include:

- **p99.NET** version (NuGet package version or release tag);
- Target framework (`net8.0`, `netstandard2.0`, or consumer TFM);
- Operating system and architecture;
- .NET runtime or SDK version;
- Minimal reproduction steps or a link to a branch;

For build failures, attach the `dotnet build` / `dotnet test` log. For
behavioural issues, show expected vs actual results.


## Getting started

1. Install the [.NET SDK](https://dotnet.microsoft.com/download) version pinned in `global.json`;
2. Clone the repository and restore dependencies:

   ```bash
   dotnet restore p99.NET.sln
   ```

3. Build and test:

   ```bash
   dotnet build p99.NET.sln
   dotnet test p99.NET.sln
   ```

   Or use the cross-platform build scripts:

   ```bash
   ./build.sh        # Linux / macOS
   ./build.ps1       # Windows PowerShell
   ```


## Project layout

| Path | Purpose |
|------|---------|
| `src/P99/` | Main library (published to NuGet as `p99`) |
| `tests/P99.Tests/` | Unit tests |
| `samples/P99.QuickStart/` | Minimal consumer example |

Shared MSBuild settings live in `Directory.Build.props`. NuGet package
versions are centralized in `Directory.Packages.props`.


## Coding standards

Formatting and analyser settings are defined in [`.editorconfig`](.editorconfig)
and shared MSBuild properties in [`Directory.Build.props`](Directory.Build.props).
The editor ruler at column 76 in [`.vscode/settings.json`](.vscode/settings.json)
marks the documentation limit.

- **C#** sources use **4 spaces** per indent level (not tabs);
- Builds treat warnings as errors; nullable reference types are enabled;
- Match existing naming, layout, and brace style in the file you edit;
- Public API in **`src/P99/`** must have XML documentation (`///`);
- Keep changes focused; prefer small, reviewable pull requests;


### `DOC_76` (the 76-rule for documentation comments)

All XML documentation comments (`///`) on **public** library members must
follow this convention (aligned with sibling **p99** C and Rust projects):

1. **Line length**: no `///` line may exceed **76 characters** in total
   (including leading indentation, `///`, spaces, and XML markup). Code
   lines are not limited;
2. **Greedy wrapping**: pack as many words as possible onto each line up to
   that limit. Do not wrap early when the next word still fits;
3. **Capitalisation**: the first word of a comment should be capitalised,
   unless it is a well-known name (for example, `p99`, `NuGet`);

**Single-line form** — when the opening tag, text, and closing tag all fit
on one line:

```csharp
/// <summary>Returns the count of events in a specific bucket.</summary>
```

**Multi-line form** — when the single-line form would exceed 76 characters:

- the opening and closing tags each occupy their own line;
- prose is placed on separate lines between them;
- each prose line has **one extra leading space** after `///` (that is,
  `///  ` rather than `/// `);
- nested elements (for example `<para>` inside `<remarks>`) add a further
  leading space on their tag and content lines;

```csharp
/// <summary>
///  Clears the instance, resetting all values to the equivalent of a
///  newly constructed instance.
/// </summary>
/// <remarks>
///  <para>
///   Tracks event durations with nanosecond precision across power-of-two
///   bucket spacing. The structure is compact, allocation-free, and suited
///   for high-frequency timing measurements.
///  </para>
/// </remarks>
```

**Inline comments** (`//` inside methods) are limited to **100 characters**
per line (including indentation) and should be wrapped greedily.


## Documentation

When adding or changing behaviour, update as appropriate:

- [README.md](./README.md) — overview, building, and API summary;
- [CHANGELOG.md](./CHANGELOG.md) — release notes under **[Unreleased]**
  (not a backlog);
- XML documentation on affected public members in **`src/P99/`**;

Markdown lists use a **`-`** marker and each item ends with a **`;`** (not
`*` or `.`), except the final item in a section which may end with `.` when
it completes a sentence.


## Pull requests

1. Create a feature branch from `master`;
2. Ensure `dotnet build` and `dotnet test` pass locally;
3. Update `CHANGELOG.md` under **Unreleased** when the change is user-visible;
4. Open a pull request with a clear description of the problem and solution;


## Releases

Maintainers publish releases by creating a GitHub Release. The release
workflow packs the library and publishes to
[NuGet.org](https://www.nuget.org/) when `NUGET_API_KEY` is configured in
repository secrets.

Tag releases with semantic version tags (for example, `0.1.2` or `v0.1.2`).


## License

By contributing, you agree that your contributions will be licensed under
the [3-clause BSD license](LICENSE) as the project.
