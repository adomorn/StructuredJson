---
name: Feature Request
about: Suggest an idea for this project
title: "[FEATURE] "
labels: "enhancement, needs-triage"
assignees: ''

---

### 🤔 The Problem
Please describe the problem you are trying to solve. What is it that you want to do, but can't with the current version of `StructuredJson`?

*(Example: I'm trying to parse a JSON file that contains comments, but `new StructuredJson(jsonString)` throws an exception.)*

### 💡 Proposed Solution
Describe the solution you'd like. How would you ideally solve this problem with the library? Please provide a code example if possible.

*(Example: It would be great to have an option to ignore comments during parsing.)*

```csharp
// Example of how the new feature might be used:
var options = new JsonParserOptions { IgnoreComments = true };
var sj = new StructuredJson(jsonWithComments, options); 
```

###  Alternatives Considered
Have you considered any alternative solutions or workarounds?

*(Example: I currently pre-process the JSON string with a regex to remove comments, but this is slow and error-prone.)*

### 📖 Additional Context
Add any other context, motivation, or screenshots about the feature request here. Why would this feature be useful to other users of the library? 