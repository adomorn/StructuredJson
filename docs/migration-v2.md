# Migrating from 1.x to 2.0.0

2.0.0 is a stable package for .NET 8, 9 and 10, with a `net11.0` asset tested on .NET 11 RC1. The prerelease status belongs to the .NET 11 platform. Consumers on older .NET Framework/.NET Standard must stay on 1.x or upgrade their runtime; 1.x is not the recommended security-maintained line.

1. Upgrade your PackageReference to 2.0.0 and use `using SJ = StructuredJson.StructuredJson;` in consumer code.
2. Escape literal `:`, `[`, `]`, and `\` in property names with `\`. Use `\e` for an empty property. Remove permissive paths such as `a::b`, `a[` and `items[0]suffix`.
3. Audit callers relying on implicit container replacement. `new SJ(new StructuredJsonOptions { OverwriteOnTypeConflict = true })` restores that explicit choice. Normal field/element updates preserve siblings.
4. Use typed numeric reads: `GetRequired<decimal>("amount")`, `GetRequired<ulong>("id")`. Untyped reads keep non-integral/large numbers as `JsonElement` tokens. Get<T> still returns default on failed conversions; prefer TryGet or GetRequired when failure matters.
5. Numeric strings are invariant by default. Supply an explicit `NumberCulture` for localized input. Group separators are not accepted by built-in parsing: tr-TR `123.45` fails instead of silently becoming 12345. Use actual JSON numbers where possible.
6. Expect ListPaths to include nulls and empty containers. Null-filled gaps cannot be distinguished from explicit JSON nulls. Filtering is the caller's choice.
7. Do not mutate values returned by Get/ListPaths to edit the tree; they are detached. Call Set instead. Dispose source JsonDocuments freely after Set has returned.
8. Configure MaxDepth/MaxArrayLength/MaxNodeCount/MaxPathLength for your domain. Limit exceptions leave existing contents unchanged; raising bounds affects memory/CPU costs.
9. Constructors require nonempty JSON with an object root and unique property names. Root arrays/scalars are rejected; arrays inside the root now support repeated index paths.
10. Put serializer converters and CLR naming policy into StructuredJsonOptions.SerializerOptions. ToJson's optional settings affect indentation and encoder only; stored property names and numeric tokens are not rewritten. ReferenceHandler policies are rejected; cyclic CLR graphs fail under default settings.
11. Typed collection reads cannot combine Populate with scoped member/type number policies; TryGet returns false and GetRequired throws InvalidCastException. Use Replace with a writable property for that combination. Ordinary collection population and native CLR input serialization are preserved.

For source builds, install the exact SDK from global.json and the 8/9/10 runtimes. The released package itself has no external NuGet dependencies. Operations are synchronized individually; compound multi-call updates still require application-level coordination. Native AOT is not a supported configuration in this release.
