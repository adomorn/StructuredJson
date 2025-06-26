---
name: Bug Report
about: Create a report to help us improve
title: "[BUG] "
labels: "bug, needs-triage"
assignees: ''

---

### 🐞 Describe the Bug
A clear and concise description of what the bug is. Please be as specific as possible.

### 👣 Steps to Reproduce
Please provide a minimal, self-contained code sample that demonstrates the bug. This is the most important part of a bug report.

```csharp
using StructuredJson;
using System;

// 1. Your C# code to set up the StructuredJson object
var sj = new StructuredJson();

// Example:
// sj.Set("path:to:value", "someValue");
// sj.Set("array[0]", 123);

// 2. The code that triggers the bug
var result = sj.Get("some:path"); // or sj.ToJson(), new StructuredJson(jsonString), etc.

// 3. Print the actual result (if applicable)
Console.WriteLine(result);
```

### ակ Expected Behavior
A clear and concise description of what you expected to happen. Please provide the expected output.

*(Example: I expected the `result` to be "someValue".)*
*(Example: I expected the `ToJson()` output to be `{"key":"value"}`.)*

###  Actual Behavior
What was the actual result? Please include any error messages or stack traces.

*(Example: The `result` was null.)*
*(Example: An `IndexOutOfRangeException` was thrown.)*
*(Example: The `ToJson()` output was `{"key":null}`.)*

### 💻 System Information
- **OS:** [e.g. Windows 11, Ubuntu 22.04]
- **.NET Version:** [e.g. .NET 7.0, .NET Framework 4.8]
- **StructuredJson Version:** [e.g. 1.0.0]

### 📖 Additional Context
Add any other context about the problem here. For example, is this a recent regression you noticed after updating the library? 