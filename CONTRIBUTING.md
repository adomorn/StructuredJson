# Contributing

Open an [issue](https://github.com/adomorn/StructuredJson/issues) for a bug or proposed API change. Security reports use SECURITY.md.

1. Branch from `development`, using a focused name such as `fix/strict-paths`.
2. Add a regression test that fails before the fix. Do not weaken assertions just to make a failure disappear.
3. Make the smallest coherent changes, documenting intentional breaking behavior and migration steps.
4. Install the SDK pinned in global.json and all supported runtimes. Run the README validation commands, including package consumption and release validator tests.
5. Update CHANGELOG.md in the same PR. The library csproj is the version authority.
6. Self-review the complete diff, then obtain an independent review before opening/merging the final PR. Keep unrelated changes separate and use meaningful commits.
7. Open a PR against `development` with the problem, final behavior, compatibility implications and actual test results. All CI matrix jobs must pass before merge; maintainers configure these as required ruleset checks. Do not put tokens in code, logs or comments.

## Style and tests

Use .editorconfig and the enabled .NET analyzers. Public APIs require XML comments. Tests should check observable behavior, including rejected-operation nonmutation, literal path round trips, number precision, serializer settings and limits. Use deterministic seeds for generated tests. Benchmarks belong in the benchmark project, not timing assertions in unit tests.

## Release process

After reviewed changes are merged, maintainers validate the csproj version and dated changelog section, then create the matching immutable tag. Release CI tests the tagged source on all supported OS/framework combinations and consumes its package locally. The publish job uses only that tested artifact and the protected `nuget` environment secret. It publishes NuGet before the GitHub release. A version must never be reused for different package contents.

Fork PRs do not receive Sonar/publishing secrets. Never use pull_request_target to execute fork code with privileged credentials. Contributions are licensed under MIT.
