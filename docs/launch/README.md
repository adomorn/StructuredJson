# Getting the first few users

The first goal is to learn whether StructuredJson makes a real JSON-editing task easier. Aim for feedback from 5–10 independent projects before expanding the feature list around guesses.

The public starting point is [Update nested JSON in C# without creating model classes](https://github.com/adomorn/StructuredJson/blob/development/docs/guides/update-nested-json-in-csharp.md). It leads to a runnable example, an honest JsonNode comparison and the NuGet package.

## A small launch

1. Publish the guide in the repository with the examples and updated README.
2. Adapt the guide for DEV Community. Use the GitHub guide URL as the canonical source when cross-posting. Read the rendered preview and run the example before publishing.
3. Share the short LinkedIn text below with the guide link. A code screenshot or a short recording of the example is enough; a promotional graphic is optional.
4. If a .NET community allows project introductions, share one useful example there and disclose that you maintain the library. Follow that community's rules and answer questions in the thread.
5. Ask people who tried it what they were editing and where they got stuck. Record concrete feedback, then fix the most common obstacle.

The copy below is ready to adapt, but has not been posted to external accounts. Do not present planned users, integrations or performance results as existing evidence.

## DEV Community

**Title:** Update nested JSON in C# without creating model classes

**Suggested tags:** `csharp`, `dotnet`, `json`, `opensource`

Use [the full guide](https://github.com/adomorn/StructuredJson/blob/development/docs/guides/update-nested-json-in-csharp.md) as the article body. Add this opening disclosure:

> I maintain StructuredJson. This is a short walkthrough of the kind of JSON edit it was built for, including when I would use JsonNode instead.

Keep the limitations and JsonNode comparison in the article. They help a reader decide whether the extra package belongs in their project.

## LinkedIn

I released StructuredJson 2.0.0, a .NET library for editing nested JSON through paths.

For example, `json.Set("user:preferences:theme", "dark")` creates the missing objects while leaving nearby values alone. You can also update array elements and read values with `TryGet<T>` or `GetRequired<T>`.

JsonNode can do these edits too. The reason to try this package is the path API, especially when the shape varies and you do not want a model class for every payload.

I wrote a short guide with runnable examples and a side-by-side JsonNode comparison:
https://github.com/adomorn/StructuredJson/blob/development/docs/guides/update-nested-json-in-csharp.md

If you try it, I'd like to hear which JSON edit you used it for and what felt awkward.

## A shorter community introduction

I maintain StructuredJson, a C# library for reading and updating JSON with paths such as `items[0]:price`.

I put together a small example that changes a nested value, creates a missing object and checks that the rest of the payload stays intact. It also shows the same edit with JsonNode, which may be all you need for a known schema.

Guide and code: https://github.com/adomorn/StructuredJson/blob/development/docs/guides/update-nested-json-in-csharp.md

I'd appreciate feedback on the path syntax or a task where the API gets in your way. Version 2.0.0 supports .NET 8/9/10 and .NET 11 RC1; it does not support .NET Framework.

## Keep track without guessing

The first snapshot records 1,748 cumulative downloads from the [public NuGet search API](https://azuresearch-usnc.nuget.org/query?q=packageid%3AStructuredJson&prerelease=false&take=1) on 2026-09-27. Blank fields mean not recorded, not zero.

Once a week, add a row to [the adoption worksheet](adoption.csv). Keep dates in UTC and compare intervals of the same length. Leave unavailable numbers empty rather than recording zero.

- **NuGet downloads:** record the cumulative total displayed on the package page. The difference between snapshots is an indicator of activity, not a count of new people. Package restores, including your own validation, can affect it.
- **GitHub traffic:** repository maintainers can use Insights → Traffic. Record the window shown by GitHub with the numbers; overlapping windows should not be added together.
- **Known usage:** count a project only after its maintainer confirms use or a public reference shows it. Link public evidence in the notes, and keep private conversations out of this public worksheet.
- **Feedback:** record the problem or friction, not just a star or compliment. Keep the issue link and the next action.

At the end of the first month, look for the point where people stop. Visits without trials suggest the explanation or examples need work. Trials followed by the same question suggest a documentation or API issue. Requests for .NET Framework support would give the compatibility work a concrete audience. These are hypotheses to test, not conclusions from download counts alone.

Suggested GitHub description:

> Read and update nested JSON in C# using paths, without model classes. Typed reads, escaped keys and configurable limits for .NET 8/9/10 and .NET 11 RC.

Use accurate topics such as `csharp`, `dotnet`, `json`, `json-manipulation` and `nuget-package`. Avoid `jsonpath` or `json-schema`: those describe capabilities this library does not provide.
