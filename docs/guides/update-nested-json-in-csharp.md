# Update nested JSON in C# without creating model classes

You have a JSON response and need to change two values before passing it on. Defining classes for the whole payload feels like more work than the edit itself. Walking the JSON tree by hand works, but gets repetitive when the paths come from different parts of your application.

That is the use case behind StructuredJson. It is a small .NET library for reading and editing JSON objects through paths. It uses System.Text.Json, and version 2.0.0 is available on NuGet.

Let's try an edit you can run in a console application.

## Start with a short payload

In a directory outside the StructuredJson repository, use a .NET 8 SDK:

```sh
dotnet new console -n JsonEditingDemo -f net8.0
cd JsonEditingDemo
dotnet add package StructuredJson --version 2.0.0
```

Replace `Program.cs` with:

```csharp
using SJ = StructuredJson.StructuredJson;

var json = new SJ("""{"user":{"name":"Ada","scores":[10,20]}}""");
json.Set("user:scores[1]", 30);
json.Set("user:preferences:theme", "dark");
int score = json.GetRequired<int>("user:scores[0]");
Console.WriteLine(json.ToJson());
```

Run `dotnet run`. You get the following values, pretty-printed:

```json
{
  "user": {
    "name": "Ada",
    "scores": [10, 30],
    "preferences": { "theme": "dark" }
  }
}
```

The first edit changes the second score. The second creates the missing `preferences` object and adds its theme. Neither edit changes the user's name or first score. The alias `SJ` is there because the namespace and class are both named `StructuredJson`.

## What if the shape changes?

A path uses `:` between properties and `[index]` for array positions. Literal separators in property names can be escaped. There are no wildcards or filter expressions: this is an editing path, not JSONPath.

Missing containers can be created, but incompatible existing values are different. If `preferences` is a string, setting `user:preferences:theme` throws by default instead of replacing that string with an object. Replacing a final property, such as `user:name`, is an ordinary edit.

For optional data, use `TryGet<T>` so a missing value does not look like a real zero or false. This complete example prints `No next page supplied.`:

```csharp
using SJ = StructuredJson.StructuredJson;

var response = new SJ("""{"meta":{"page":1}}""");
if (response.TryGet<int>("meta:nextPage", out int nextPage))
    Console.WriteLine($"Next page: {nextPage}");
else
    Console.WriteLine("No next page supplied.");
```

`GetRequired<T>` is for values that must exist and convert successfully. Missing values and failed conversions raise different exceptions. An invalid path is an error even when using `TryGet`.

## Why not just use JsonNode?

You can. JsonNode also edits JSON without model classes, and it is a good choice when direct DOM navigation is already comfortable. StructuredJson adds a path-based interface, detached reads and configurable structural limits. Whether that saves you work depends on your application.

The [side-by-side comparison](https://github.com/adomorn/StructuredJson/blob/development/docs/jsonnode-comparison.md) performs the same edit with both APIs and checks their output. There is no speed claim attached to it.

There are limits to keep in mind. The root must be an object. The library does not validate JSON Schema or implement JSON Patch. It bounds the stored tree, but those limits are not a complete input-memory quota. If your schema is stable, ordinary C# models may still be the clearest choice.

## Try it on something familiar

The repository has [three runnable examples](https://github.com/adomorn/StructuredJson/blob/development/examples/README.md): changing a logging setting, reshaping an API response and creating an independent test fixture. Each checks the resulting JSON, so you can change the input and see what happens.

Version 2.0.0 targets .NET 8, 9, 10 and 11. The package is stable; **.NET 11 support was tested against RC1**, not the final runtime. CI covers Windows, Linux and macOS. Existing .NET Framework projects are not supported by this version.

If you try it, the most useful feedback is a small example of an edit that felt awkward. Share a sanitized payload, the result you wanted and your .NET version in a [GitHub issue](https://github.com/adomorn/StructuredJson/issues/new/choose).

[Install from NuGet](https://www.nuget.org/packages/StructuredJson/2.0.0) · [Browse the source](https://github.com/adomorn/StructuredJson) · [Read the changelog](https://github.com/adomorn/StructuredJson/blob/development/CHANGELOG.md)
