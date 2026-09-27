# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- Runnable configuration, API transformation and test-fixture examples, plus a checked JsonNode comparison.
- A practical JSON editing guide.

### Changed
- Standardized repository documentation and example output on English; kept Unicode character coverage with escaped test data.
- Removed internal audit reports, raw verification output, agent work plans, IDE files, social-post drafts and adoption tracking from the repository.
- Removed the uncompiled root example; the maintained runnable examples remain under `examples/`.
- README now leads with installation, input/output and concrete use cases, with explicit guidance on when to use JsonNode or typed models.

## [2.0.0] - 2026-09-27

### Added
- Native .NET 8, 9, 10 and 11 target assemblies. .NET 11 support is validated against RC1 and documented separately from this stable library version.
- Escaped path properties, empty property names (`\e`) and repeated array indices such as `matrix[0][1]`.
- `StructuredJsonOptions` with configurable depth, path, array and total-node limits; explicit type-conflict overwrite and numeric culture policies.
- `TryGet<T>` and `GetRequired<T>` to distinguish missing values, nulls and conversion failures.
- Detached snapshots and synchronized individual operations.
- Four-framework/three-OS PR CI, independent local package smoke tests, compiled examples, benchmark harness, dependency locks and release-version/changelog validation.

### Fixed
- Malformed paths can no longer alias and delete or overwrite valid properties.
- Paths reject malformed UTF-16 before mutation, preventing replacement-character key collisions while preserving valid Unicode pairs.
- Malformed Unicode in parsed JSON names and strings follows the documented invalid-input exception contract.
- JSON number tokens retain their original precision and range through round trips.
- CLR arrays, typed dictionaries, POCOs and JsonElements use the same traversable tree; updating one field preserves siblings.
- Stored JsonElements no longer depend on the caller's JsonDocument lifetime.
- Non-object JSON roots, duplicate properties and empty input are rejected instead of silently losing data.
- Validation failure leaves existing state unchanged; sparse expansion and nested values obey resource limits.
- Numeric string conveniences cover signed and unsigned 8/16/32/64-bit integers; overflow and non-finite conversions fail instead of reporting success.
- Half/float/double dictionary-key conversions reject NaN and infinities while retaining native finite-key parsing.
- Explicit converters take precedence over convenience conversions; member/type number-handling settings remain effective for scalars, nullable values and numeric collections.
- Ignored properties no longer cause typed reads to fail because of their unused collection population and number policies.
- Path discovery escapes reserved characters and includes nulls and empty containers.
- CI actions use supported Node 24 runtimes and pinned commits; Python coverage tooling installs wheel packages only.
- Sonar uses checkout plus the .NET scanner begin/build/end workflow and coverage import.
- Simplified parser, tree mutation and conversion helpers to satisfy Sonar maintainability rules without suppressions; corrected exception parameter names and allocation warnings.
- Package smoke tests resolve the installed SDK from PATH instead of accepting an executable argument. Python release checks now exercise missing versions, publication failures, immutable package identity and release-note extraction, with coverage imported into Sonar.
- Release version is no longer hardcoded or changed after compilation; only a matching version tag publishes the tested artifact.
- Publication retries verify existing package contents and retry symbol publication independently.
- NuGet Trusted Publishing uses short-lived GitHub OIDC credentials; manual release recovery validates and retests the original immutable tag without moving it.
- Removed the obsolete vulnerable SourceLink package reference in favor of the pinned SDK tooling and updated test dependencies.
- Corrected namespace aliases in examples, dependency update coverage and placeholder contribution links; removed personal IDE state.

### Changed / Breaking
- Removed .NET Standard 2.0 / .NET Framework targets; minimum consumer runtime is .NET 8.
- Intermediate type conflicts throw by default; opt into `OverwriteOnTypeConflict` for deliberate legacy replacement.
- Numeric strings use invariant culture and do not accept thousands separators unless a custom converter defines a representation.
- Untyped non-integral/large numeric reads return lossless JsonElement tokens; use `Get<decimal>`, `Get<double>` or `Get<ulong>` for typed values.
- `ListPaths` now includes array nulls and empty objects/arrays; returned values are detached.
- Empty JSON input and ambiguous duplicate properties are errors; unknown/malformed path syntax is strictly rejected.
- `ToJson` options control formatting only; CLR conversion settings belong in `StructuredJsonOptions.SerializerOptions`.
- Typed collection reads combining `Populate` with scoped member/type number handling report unsupported conversion; use `Replace` and a writable property. Native CLR input serialization and ordinary population are preserved.
- Default array length is bounded at 100,000 and default depth at 128. Increase limits explicitly for trusted larger inputs.
- See [the v2 migration guide](https://github.com/adomorn/StructuredJson/blob/development/docs/migration-v2.md) for concrete upgrade examples.

## [1.0.0] - 2025-06-13

### Added
- Initial implementation of StructuredJson library
- Path-based API for JSON manipulation
- Support for .NET Standard 2.0, .NET 8, and .NET 9
- Comprehensive test suite with cross-platform compatibility
- XML documentation for all public APIs
- GitHub Actions workflow for automated releases
- Intelligent type conversion system
- Sparse array support with automatic null-filling
- Locale-aware number formatting
- Comprehensive path validation
- Robust error handling
- Memory-efficient sparse array support

### Features
- Path-based API for intuitive JSON navigation
- Smart type conversion between strings and numbers
- Array manipulation with automatic null-filling
- Locale-aware number formatting
- Comprehensive error handling
- Dictionary-based O(1) key lookups
- Optimized path parsing with regex
- Lazy evaluation for type conversions

### Technical Details
- Built on .NET Standard 2.0 for maximum compatibility
- Uses System.Text.Json for modern JSON serialization
- Cross-platform support (Windows, macOS, Linux)
- Full XML documentation coverage
- Comprehensive unit test coverage
