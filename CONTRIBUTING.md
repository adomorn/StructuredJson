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

After reviewed changes are merged, maintainers validate the csproj version and dated changelog section, then create the matching immutable tag. Release CI resolves the tag to a commit on the default branch, tests that exact source on all supported OS/framework combinations and consumes its package locally. The publish job uses only that run's tested artifact, rechecks the tag and publishes NuGet before the GitHub release. A version must never be reused for different package contents.

Publishing uses [NuGet Trusted Publishing](https://learn.microsoft.com/nuget/nuget-org/trusted-publishing) instead of a stored API key. In the `Adomorn` NuGet account, configure a GitHub policy for owner `adomorn`, repository `StructuredJson`, workflow `release.yml`, environment `nuget`, and package `StructuredJson` with new-version publication permission. Protect the GitHub `nuget` environment appropriately. Only the publish job receives `id-token: write`; the pinned NuGet login action exchanges its GitHub identity for a short-lived, masked API key immediately before publication.

To recover a failed release after fixing the workflow, run **Release NuGet Package** manually from the default branch with the original tag (for example `v2.0.0`). Manual execution from another branch is rejected. The workflow tests and publishes the original tag's commit using the updated workflow; it does not move the tag or change the package version. Existing packages must match the tested artifact before symbol or GitHub release recovery can proceed.

Fork PRs do not receive Sonar/publishing secrets. Never use pull_request_target to execute fork code with privileged credentials. Contributions are licensed under MIT.
