# Do I need this if I already use JsonNode?

Maybe not. `JsonNode` is a useful built-in JSON DOM, and it can edit JSON without model classes too. StructuredJson is worth trying when you want to pass around paths such as `user:scores[1]`, create missing containers during an edit, or get detached values back from reads.

Here is the same small edit with both APIs. The input has a `user` object and two scores, but no preferences:

```csharp
const string input = """{"user":{"name":"Ada","scores":[10,20]}}""";
```

With StructuredJson:

```csharp
using SJ = StructuredJson.StructuredJson;

var json = new SJ(input);
json.Set("user:scores[1]", 30);
json.Set("user:preferences:theme", "dark");
Console.WriteLine(json.ToJson());
```

With `JsonNode`:

```csharp
using System.Text.Json.Nodes;

var json = JsonNode.Parse(input)!.AsObject();
var user = json["user"]!.AsObject();
user["scores"]!.AsArray()[1] = 30;
user["preferences"] = new JsonObject { ["theme"] = "dark" };
Console.WriteLine(json.ToJsonString());
```

Both produce the same JSON values. The example program checks that with `JsonNode.DeepEquals`; formatting differs. The `JsonNode` version is intentionally written for this known input. For an input that already contains preferences, check and reuse that object to preserve its other properties. StructuredJson's path edit handles missing objects and preserves existing siblings; an incompatible intermediate value throws by default.

[Run the comparison](https://github.com/adomorn/StructuredJson/blob/development/examples/README.md) with `-- comparison`.

| What you need | StructuredJson 2.0.0 | JsonNode |
| --- | --- | --- |
| Edit a known object tree | Path strings with `Set` and typed reads | Object and array indexers, with `GetValue<T>()` for scalar reads |
| Create missing containers | `Set` creates them along a valid path | Create or reuse the objects and arrays in your traversal |
| Change a value obtained from a read | Reads are detached; use `Set` to write changes back | Nodes belong to the mutable DOM; edits to attached nodes change that tree |
| Limit stored tree size | Configurable depth, path length, array length and node count | Add application-specific limits where needed; JSON parsing also has depth controls |
| Work with a root array or scalar | Root must be an object | Root may be an object, array or scalar |
| Use JSONPath queries or JSON Patch | Neither is implemented | Neither is a built-in JsonNode query/patch API |
| Avoid an extra package | Adds the StructuredJson package | Available through System.Text.Json in modern .NET |

StructuredJson's paths use their own documented syntax. They are not JSONPath, JSON Pointer or ASP.NET configuration keys, even where the separators look familiar.

For a stable schema, typed models with `JsonSerializer` may express your intent better than either approach. For performance-sensitive code, measure your workload. This comparison is about behavior and code you write, not a claim that one library is faster.

References: Microsoft's [JSON DOM guide](https://learn.microsoft.com/dotnet/standard/serialization/system-text-json/use-dom) and [JsonNode API](https://learn.microsoft.com/dotnet/api/system.text.json.nodes.jsonnode); StructuredJson's [API behavior and limits](https://github.com/adomorn/StructuredJson#api-and-failure-behavior).
