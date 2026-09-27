# StructuredJson

[![NuGet](https://img.shields.io/nuget/v/StructuredJson.svg)](https://www.nuget.org/packages/StructuredJson/)
[![CI](https://github.com/adomorn/StructuredJson/actions/workflows/ci.yml/badge.svg)](https://github.com/adomorn/StructuredJson/actions/workflows/ci.yml)

A bounded JSON object editor for .NET with escaped property paths, nested arrays, lossless JSON numbers and detached reads.

## Supported platforms

StructuredJson **2.0.0 is a stable library release**, targeting `net8.0`, `net9.0`, `net10.0` and `net11.0`. **.NET 11 support is validated against .NET 11 RC1**, a prerelease platform; this does not claim validation against the future final .NET 11 runtime. Use serviced .NET runtime patches.

Building all targets requires the exact .NET 11 RC1 SDK pinned in `global.json`. Runtime consumers on .NET 8/9/10 do not need that SDK. Version 2 removes .NET Standard/.NET Framework targets. See [migration](https://github.com/adomorn/StructuredJson/blob/development/docs/migration-v2.md) and [changelog](https://github.com/adomorn/StructuredJson/blob/development/CHANGELOG.md).

```sh
dotnet add package StructuredJson --version 2.0.0
```

## Quick start

```csharp
using SJ = StructuredJson.StructuredJson;

var json = new SJ();
json.Set("user:name", "Ada");
json.Set("user:scores", new[] { 10, 20 });
json.Set("user:scores[1]", 30); // preserves the first score
json.Set("matrix[0][1]", 42);
json.Set(@"metadata:build\:version", "2.0.0");

int score = json.GetRequired<int>("user:scores[0]");
bool found = json.TryGet<int>("user:age", out int age);
Console.WriteLine(json.ToJson());
```

The alias avoids the namespace/class name ambiguity. A compiled example and package smoke test live in `examples/StructuredJson.Example`.

## Paths

| Meaning | C# verbatim string |
| --- | --- |
| Object property | `@"user:name"` |
| Array element | `@"items[0]"` |
| Array inside array | `@"matrix[0][1]"` |
| Literal colon/brackets | `@"a\:b:x\[0\]"` |
| Literal backslash | `@"folder\\name"` |
| Empty property name | `@"\e"` |
| Literal property `\e` | `@"\\e"` |

Properties are ordinal and case-sensitive. Whitespace in keys is significant. Indices contain ASCII digits only. Empty segments, trailing characters, unknown escapes, negative indices and incomplete brackets are rejected. The root must be an object; arrays within it are supported.

## API and failure behavior

- `Set(path, value)` copies/normalizes CLR objects, dictionaries, arrays and JsonElements before mutation. Invalid input or limits raise `ArgumentException`; incompatible intermediate types raise `InvalidOperationException`. Existing data remains unchanged on validation failure. Replacing the final property is intentional; replacing an incompatible intermediate value needs explicit permission.
- `Get(path)` returns a detached dictionary/list/scalar snapshot or null. Small integers are `int`/`long`; other numbers remain lossless `JsonElement` tokens. Use typed reads for decimal/double/unsigned values.
- `Get<T>(path)` returns `default(T)` on absence or conversion failure, retaining the convenience API. Invalid syntax throws. Unexpected exceptions from user converters are not swallowed.
- `TryGet<T>(path, out value)` distinguishes a valid 0/false from absence/conversion failure. Explicit null succeeds for nullable/reference types and fails for nonnullable value types. An absent value always returns false.
- `GetRequired<T>(path)` throws `KeyNotFoundException` for absence or `InvalidCastException` for conversion failure. Explicit null is valid for nullable/reference types.
- `HasPath(path)` includes explicit null and null-filled array gaps; malformed paths return false.
- `Remove(path)` removes a property or shifts array elements; missing/malformed paths return false.
- `ListPaths()` includes nulls and empty containers. Returned paths are escaped, unique and reusable; the empty root produces an empty dictionary.
- `ToJson(options)` writes number tokens without precision loss. Only formatting (`WriteIndented`, `Encoder`) is taken from this optional argument; naming policies/converters do not rewrite an already normalized tree.
- `Clear()` resets the object and its node budget.

JSON constructors reject empty input, duplicate property names and non-object roots. A value such as `1e400` can round-trip as a JSON number token, but conversion to a finite double fails explicitly through `TryGet`/`GetRequired`.

## Limits, conversion and ownership

```csharp
using System.Globalization;
using StructuredJson;
using SJ = StructuredJson.StructuredJson;

var json = new SJ(new StructuredJsonOptions
{
    MaxDepth = 128,
    MaxPathLength = 4096,
    MaxArrayLength = 100_000,
    MaxNodeCount = 1_000_000,
    NumberCulture = CultureInfo.InvariantCulture,
    OverwriteOnTypeConflict = false
});
```

These are the defaults. Depth counts containers including the root. Node count includes the root, containers, values and null-filled gaps. Limits apply to stored structures and paths; they are not a maximum transient serialization/input-string memory quota. Sparse writes use ordinary lists and allocate all gaps; they are not a compressed sparse representation.

Numeric strings use invariant culture by default and reject thousands separators. An explicit `NumberCulture` is supported. Non-finite CLR numbers and cyclic CLR graphs are rejected with default serializer settings. `SerializerOptions` configures CLR input and typed output (custom converters, naming, case sensitivity); options are copied on construction. Reference preservation/cycle ignoring is unsupported. Custom converters define their own JSON representation and should be trusted.

Typed reads cannot combine collection `Populate` behavior with member/type number-handling policies that require a scoped converter: System.Text.Json does not allow those converters to populate an existing collection. `TryGet` returns false and `GetRequired` throws `InvalidCastException` for this combination. Use `Replace` with a writable property. Ordinary collection population without a scoped number policy remains supported; CLR input serialization uses native System.Text.Json behavior.

Input and output collections are detached; mutating them does not change stored data. Operations are synchronized, but a sequence such as `HasPath` followed by `Get` is not a transaction. Use one `TryGet` where appropriate. The API uses reflection-based JSON conversion; Native AOT/trimming support is not claimed.

## Development and validation

Install the SDK in `global.json` and .NET 8/9/10 runtimes (the SDK supplies .NET 11 RC1):

```sh
dotnet restore StructuredJson.sln --locked-mode
dotnet build StructuredJson.sln -c Release --no-restore
dotnet test StructuredJson.Tests/StructuredJson.Tests.csproj -c Release --no-build --no-restore
dotnet pack StructuredJson/StructuredJson.csproj -c Release --no-build --no-restore -o artifacts
python3 scripts/package-smoke.py
python3 scripts/test_release.py
```

CI runs all four targets on Linux, Windows and macOS. A separate Sonar .NET begin/build/end workflow submits coverage and enforces its configured quality gate; fork PRs use the secret-free CI checks. Maintainers should mark the three CI matrix jobs as required checks in GitHub rulesets.

Benchmarks measure both time and allocations; there is no path cache or unmeasured performance guarantee:

```sh
dotnet run -c Release --project benchmarks/StructuredJson.Benchmarks -- --filter '*'
```

## Releases

Versions are read from the library csproj. Each release requires a matching dated changelog section. After review and merge, pushing the matching version tag (e.g. `v2.0.0`) reruns CI, publishes its tested package to NuGet, then creates the GitHub release from that version's notes. The `nuget` GitHub environment requires a `NUGET_API_KEY`; configure environment reviewers and protected tags/rulesets before granting release authority. Re-running the same tag can recover a partially completed publication only when the existing NuGet package contents match (excluding NuGet repository signatures); never move a published tag or reuse a version for different bytes.

See [CONTRIBUTING](https://github.com/adomorn/StructuredJson/blob/development/CONTRIBUTING.md), [SECURITY](https://github.com/adomorn/StructuredJson/blob/development/SECURITY.md), and the [MIT license](https://github.com/adomorn/StructuredJson/blob/development/LICENSE).
