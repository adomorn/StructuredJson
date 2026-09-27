# Try StructuredJson on a real task

These examples use small, fixed inputs so you can see exactly what changed. They print JSON, check the result and exit with an error if it differs from the expected output. They do not make network requests or overwrite files.

## Run from this repository

Install the SDK pinned in `global.json` and a .NET 8 runtime, then run these commands from the repository root:

```sh
dotnet run --project examples/StructuredJson.Example -f net8.0 -- configuration
dotnet run --project examples/StructuredJson.Example -f net8.0 -- api-response
dotnet run --project examples/StructuredJson.Example -f net8.0 -- test-fixtures
dotnet run --project examples/StructuredJson.Example -f net8.0 -- comparison
```

Omit the argument after `--` to run everything. You can also select `net9.0`, `net10.0` or `net11.0` if the corresponding runtime is installed; .NET 11 is tested against RC1. These commands build the library from source. CI also runs all scenarios against the locally packed library on Windows, Linux and macOS, for all four targets.

Want to try the published package with just a .NET 8 SDK? Create a console project **outside this checkout** so the repository's SDK pin does not apply:

```sh
dotnet new console -n JsonEditingDemo -f net8.0
cd JsonEditingDemo
dotnet add package StructuredJson --version 2.0.0
```

Replace its `Program.cs` with [the example program](https://github.com/adomorn/StructuredJson/blob/development/examples/StructuredJson.Example/Program.cs), then run `dotnet run -- configuration` (or another scenario above).

## Change a configuration value

Suppose you want more logging for your application without changing the setting for system components:

```csharp
using SJ = StructuredJson.StructuredJson;

var config = new SJ("""{"Logging":{"LogLevel":{"Default":"Information","System":"Warning"}},"AllowedHosts":"*"}""");
config.Set("Logging:LogLevel:Default", "Debug");
config.Set("Features:Search:Enabled", true);
Console.WriteLine(config.ToJson());
```

`Default` becomes `Debug`, `System` stays `Warning`, and the missing `Features` objects are created. The sample works on JSON text in memory. It is not an ASP.NET configuration provider and does not preserve comments or original formatting.

## Reshape an API response

An upstream service returns prices as strings. Change the first item's price to a JSON number, add an availability flag and remove one known debug field:

```csharp
using SJ = StructuredJson.StructuredJson;

var response = new SJ("""{"items":[{"id":"p1","price":"19.95","debug":"internal"},{"id":"p2","price":"8.50"}],"meta":{"page":1}}""");
decimal price = response.GetRequired<decimal>("items[0]:price");
response.Set("items[0]:price", price);
response.Set("items[0]:available", true);
response.Remove("items[0]:debug");
bool hasNextPage = response.TryGet<int>("meta:nextPage", out int nextPage);
Console.WriteLine(response.ToJson());
```

The second item and pagination metadata stay intact. `hasNextPage` is false: a missing page number is distinct from a valid zero. This deliberately edits one known item; removing a field here is not a general data-redaction policy.

## Build a test fixture without changing the baseline

Start from a known-good payload and make a variant for an inactive user:

```csharp
using SJ = StructuredJson.StructuredJson;

var baseline = new SJ("""{"user":{"name":"Ada","active":true,"scores":[10,20]}}""");
var variant = new SJ(baseline.ToJson());
variant.Set("user:active", false);
variant.Set("user:scores[1]", 0);
int[] scores = variant.GetRequired<int[]>("user:scores")!;
scores[0] = 999;
Console.WriteLine(variant.ToJson());
```

The baseline still contains an active user with scores `[10,20]`. The variant contains an inactive user with scores `[10,0]`. Changing the returned array does not change the variant: reads return detached values.

For the same edit using the built-in JSON DOM, see [StructuredJson and JsonNode](https://github.com/adomorn/StructuredJson/blob/development/docs/jsonnode-comparison.md).
