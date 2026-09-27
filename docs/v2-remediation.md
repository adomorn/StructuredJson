# StructuredJson 2.0.0 remediation and review record

This document tracks the 27 September 2026 audit against the v2 implementation. The original audit and its evidence describe the historical 1.x baseline (`b401ed7`), not the current API; the archived reproduction program is not a current regression suite.

## Audit disposition

| Audit item | Resolution | Evidence |
| --- | --- | --- |
| F01 malformed paths | Complete grammar with escaped properties and repeated indices; no partial matching | RegressionTests.InvalidPathsCannotMutateData; AdversarialTests.MalformedPathsAreRejectedByAllEntryPoints |
| F02 precision | Raw JsonElement number tokens; explicit typed conversion, finite defaults throughout nested floating point graphs | NumberTokensRoundTripWithoutLoss; NumberBoundariesConvertExactly; NonFiniteConversionsFailInsideContainersAndPocos |
| F03 CLR models | Normalize every Set through the same private JSON tree before mutation | ClrArraysCanBeEditedWithoutLosingSiblings; PocoPropertiesCanBeEditedWithoutLosingSiblings |
| F04 roots | Non-object, empty and duplicate-key inputs rejected | NonObjectRootsAreRejected; AmbiguousOrEmptyJsonIsRejected |
| F05 limits | Array/path/depth/total-node budgets checked before attachment | BoundsAreCheckedBeforeAnyMutation; NodeBudgetRecoversAfterRemoveAndClear |
| F06 property addressing | Literal escapes and empty-key syntax; collision-free reusable paths | EscapedAndEmptyKeysAreDistinct; NullEmptyAndReservedPathsAreDiscoverable |
| F07 publication | Version authority in csproj; immutable matching tags; tested package artifact; existing NuGet identity checked; publish before GitHub release | scripts/test_release.py; scripts/validate-release.py; release.yml |
| F08 Sonar | Checkout, pinned tool, .NET begin/build/end, OpenCover and quality gate | sonarcloud.yml; requires repository SONAR_TOKEN |
| F09 PR checks | All supported TFM tests on Linux/macOS/Windows, local-only package consumption | ci.yml; GitHub rulesets are maintainer settings, not repository source |
| F10 examples | Explicit class alias; compiled example included in solution and package smoke | examples/StructuredJson.Example; corrected Example.cs |
| F11 lifetime | Clone/normalize input elements immediately | JsonElementSurvivesItsDocument |
| F12 conversions | All integer types, invariant defaults, overflow status, converter precedence | AllIntegerStringConversionsWork; NumericConvertersTakePrecedenceOverBuiltInStringParsing |
| F13 dependencies | Removed explicit SourceLink/Nullable/STJ packages; SDK tooling and runtime STJ; upgraded tests and audit-all locks | Library nuspec has no NuGet dependencies; fresh dependency audit |

## Design/bakım başlıkları

1. Culture: invariant default; explicit culture option; no accidental thousands interpretation.
2. Type conflicts: strict default, explicit overwrite option, migration documentation.
3. Error contracts: TryGet/GetRequired plus narrow expected-conversion exception handling; unexpected user-code errors propagate.
4. Discovery: nulls and empty containers included; empty root explicitly documented.
5. Nested arrays: first-class repeated index tokens across CRUD and discovery.
6. Depth/cycle/ownership: consistent depth validation, bounded tree, detached snapshots and cycle rejection on CLR ingestion.
7. Performance: removed per-call Regex; no unbounded cache; measured benchmark harness and accurate allocation documentation.
8. Architecture: parser/options/tree/converters/facade separated; shared resolver, synchronized operations and explicit serializer boundaries. Native AOT is explicitly outside the supported configurations rather than an unverified claim.
9. Reproducibility: exact SDK, lock files, analyzers/warnings-as-errors, separate packaging and package compatibility validation.
10. Hygiene: contribution links, branch/review/release process, examples, SHA-pinned actions, Dependabot coverage and personal IDE session removal.

## Validation and review ledger

- Original implementation: 21/21 new regression cases failed, confirming the tests reproduce real defects.
- First complete v2 implementation: 107 tests passed per target on .NET 8/9/10/11 RC1.
- Added deterministic adversarial tests: 125 tests passed per target, including three independent 500-operation array models.
- Independent agent review found two P2 issues: numeric converters bypassed for string representations, and nested floating point overflow reported as successful. Two new tests failed before correction; all 127 tests then passed per target after the fixes.
- Independent reviewer additionally exercised 10,000 bounded random Set/Remove operations: 5,506 completed and 4,494 rejected atomically, with JSON node counts, discovery and same-options round trips checked.
- A second fresh reviewer found member-level number handling, explicit string-converter precedence and symbol-publication retry defects. New conversion regressions failed before correction; all 129 tests then passed per target. Symbols now publish independently of the main package retry.
- A third fresh reviewer found conflicting collection number-handling metadata. A new read/write/overflow regression reproduced the failure; after correction all 130 tests passed per target, including inherited type-level settings and nullable dictionary values.
- A fourth fresh reviewer found number-handling attributes on collection subclasses were bypassed. Two new regressions failed before correction; all 132 tests then passed per target. The fix preserves list/map type attributes, property precedence, nested collection policies and overflow rejection.
- A fifth fresh reviewer found member number handling was lost for boxed floating values. Differential tests against System.Text.Json reproduced it and caught policy leakage into nested POCOs. The correction covers boxed numbers, global/member precedence and nested object/collection graphs.
- After the fifth-round correction, all 133 tests passed on each of the four runtimes (532 executions).
- A sixth fresh reviewer found extension-data dictionaries could be serialized as normal nested properties by the number-policy adapter. A new regression failed before correction. CLR input now uses native STJ settings; finite conversion adapters apply only to typed reads and preserve native extension-data contracts. All 134 tests passed per target afterward (536 executions).
- A seventh fresh reviewer found nonfinite floating dictionary keys bypassed validation; native key parsing now includes a finite-result check. Self-review also found two adjacent issues: scoped collection converters cannot honor Populate (now an explicitly documented unsupported conversion), and permissive nested string parsing differed from STJ (now delegated to STJ's parser). Three new regressions failed before these corrections.
- An eighth fresh reviewer found the same dictionary-key gap for Half, plus malformed UTF-16 paths that could become duplicate replacement-character keys when serialized. New regressions reproduced both. Half now has finite guards; path validation rejects unpaired surrogates before mutation and preserves valid surrogate pairs.
- A ninth fresh reviewer found escaped malformed Unicode in JsonElement names/strings leaked STJ's InvalidOperationException. A new regression reproduced both input entry points; narrow text-decoding translation now preserves the documented ArgumentException contract and atomic state without swallowing user converter errors.
- Self-review strengthened NuGet retry behavior with content identity comparison (allowing only the repository signature difference), plus tag/default-branch ancestry validation. Two release-script tests pass.
- Final self-review checked complete parsing, atomic attachment/node accounting, detached ownership, structural bounds, conversion failure contracts, serializer precedence, package contents, secret permissions, immutable publication and migration claims. No known actionable issue remains from self-review; independent verdicts are recorded separately.
- Local .NET runtimes: 8.0.31, 9.0.20, 10.0.12, 11.0.0-rc.1.26425.128. SDK: 11.0.100-rc.1.26425.128. Host: macOS ARM64.
- Fresh solution dependency audit reported no known vulnerable direct or transitive package dependencies. This is an advisory-database result, not a guarantee against undiscovered vulnerabilities.
- Local final build completed with zero warnings/errors; 2.0.0 nupkg/snupkg packaging and isolated NuGet consumer smoke passed for all four runtimes. Workflow lint and release validators passed. Remote CI is a separate required PR gate; its results are attached to the PR.

- Final local coverage (.NET 8): 378/404 lines (93.56%) and 371/424 branches (87.50%). Coverage is evidence of exercised paths, not proof of correctness.

- Tenth fresh whole-change review: **APPROVED, no actionable findings** against implementation commit `0365a22` plus all pending documentation/benchmark files. The reviewer independently tested records, constructor-bound readonly properties, fields, nested collections, global number handling, ordinary Populate, and Half/float/double finite boundaries on .NET 10. All 13 actionable findings from the preceding nine fresh reviews are addressed.
- Final full local runtime matrix: **140 tests passed per target, 560 executions**, zero failures/skips. Format verification, workflow lint and release-version/tag validators passed.

## Sonar follow-up

- After the token was refreshed, remote analysis authenticated successfully but failed the quality gate: new-code coverage was 76.8% and two executable-selection findings produced security rating C. The failing gate was kept enabled.
- Split complex parser, mutation, import and conversion methods into focused helpers; corrected exception parameter names, path formatting and array allocation warnings without disabling analyzer rules.
- Removed the smoke script's arbitrary executable argument; it resolves the installed SDK from PATH. Expanded release-helper tests from two to eleven, including publication failures, existing-package identity and version-specific release notes.
- Local SonarAnalyzer.CSharp 10.34.0.3385 build: zero warnings/errors. All 140 tests passed on each of four runtimes (560 executions), and rebuilt package consumers passed on all four targets. Python helper line/branch coverage is 97% from unit tests and actual package-smoke execution.
- Eleventh fresh whole-change review found one P2 issue: ignored Populate collections inherited number policies and rejected otherwise valid typed reads. A differential regression failed before the fix and passed after skipping STJ members with no accessors; constructor-bound readable properties remain processed.
- After the eleventh-review correction, the complete analyzer build again reported zero warnings/errors, all **141 tests passed per target (564 executions)**, and final package smoke passed on all four runtimes.
- Sonar now imports Python coverage, declares the Python version and test scope, and excludes the binary icon from text analysis. Compiled examples remain analyzed and are executed as package smoke tests, but are excluded from the unit-coverage denominator.

## Benchmark interpretation

BenchmarkDotNet ShortRun, one warmup and three measurement iterations, on local .NET 10.0.12 (1,000-item fixture) before final converter hardening: path read ~140 ns / 392 B, path update ~320 ns / 848 B, serialization ~51 microseconds / 242 KB, discovery ~83 microseconds / 252 KB. These are local exploratory measurements, not a regression threshold or performance guarantee. The benchmark source is committed so supported environments can remeasure the final package.

## Release boundaries

The library version is stable 2.0.0. net11.0 support means validation on RC1, not a guarantee about the future final runtime. The migration guide documents removed legacy targets and intentional breaking safety defaults. Publishing secrets, environment approvers, branch rulesets and tag protections must be configured by maintainers. A PR does not publish the package or merge itself; release occurs only from a validated version tag.
