# StructuredJson

[![NuGet Version](https://img.shields.io/nuget/v/StructuredJson.svg)](https://www.nuget.org/packages/StructuredJson/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

A powerful .NET library for creating, reading, and updating JSON objects using a path-based API. Built with `Dictionary<string, object>` as the underlying data structure and `System.Text.Json` for serialization.

## Features

- **Path-based API**: Use intuitive path syntax to navigate and manipulate JSON structures
- **Multi-target Support**: Compatible with .NET Standard 2.0, .NET 8, and .NET 9
- **System.Text.Json**: Uses the modern, high-performance JSON serializer
- **Type Safety**: Generic methods for strongly-typed value retrieval
- **Comprehensive**: Full CRUD operations with path validation and error handling
- **Well Documented**: Complete XML documentation and extensive unit tests

## Installation

Install via NuGet Package Manager:

```bash
dotnet add package StructuredJson
```

Or via Package Manager Console:

```powershell
Install-Package StructuredJson
```

## Quick Start

```csharp
using StructuredJson;

// Create a new instance
var sj = new StructuredJson();

// Set values using path syntax
sj.Set("user:name", "John Doe");
sj.Set("user:age", 30);
sj.Set("user:addresses[0]:city", "Ankara");
sj.Set("user:addresses[0]:country", "Turkey");
sj.Set("user:addresses[1]:city", "Istanbul");

// Get values
var name = sj.Get("user:name");                    // "John Doe"
var age = sj.Get<int>("user:age");                 // 30
var city = sj.Get("user:addresses[0]:city");       // "Ankara"

// Convert to JSON
var json = sj.ToJson();
Console.WriteLine(json);

// List all paths
var paths = sj.ListPaths();
foreach (var kvp in paths)
{
    Console.WriteLine($"{kvp.Key}: {kvp.Value}");
}
```

## Path Syntax

The library uses a simple and intuitive path syntax:

- **Object Properties**: Use `:` to separate object properties
  - `"user:name"` → `user.name`
  - `"config:database:host"` → `config.database.host`

- **Array Elements**: Use `[index]` to access array elements
  - `"users[0]"` → `users[0]`
  - `"items[2]:name"` → `items[2].name`

- **Complex Paths**: Combine objects and arrays
  - `"user:addresses[0]:city"` → `user.addresses[0].city`
  - `"data:items[1]:properties[0]:value"` → `data.items[1].properties[0].value`

## API Reference

### Constructors

```csharp
// Create empty structure
var sj = new StructuredJson();

// Create from JSON string
var sj = new StructuredJson(jsonString);
```

### Core Methods

#### Set(string path, object? value)
Sets a value at the specified path. Creates nested structures automatically.

```csharp
sj.Set("user:name", "John");
sj.Set("user:addresses[0]:city", "Ankara");
```

#### Get(string path)
Gets a value from the specified path. Returns `null` if path doesn't exist.

```csharp
var value = sj.Get("user:name");
```

#### Get<T>(string path)
Gets a strongly-typed value from the specified path.

```csharp
var age = sj.Get<int>("user:age");
var isActive = sj.Get<bool>("user:isActive");
var name = sj.Get<string>("user:name");
```

#### ToJson(JsonSerializerOptions? options = null)
Converts the structure to a JSON string.

```csharp
var json = sj.ToJson();
var compactJson = sj.ToJson(new JsonSerializerOptions { WriteIndented = false });
```

#### ListPaths()
Returns all paths and their values as a dictionary.

```csharp
var paths = sj.ListPaths();
// Returns: Dictionary<string, object?>
```

#### HasPath(string path)
Checks if a path exists in the structure.

```csharp
bool exists = sj.HasPath("user:name");
```

#### Remove(string path)
Removes a value at the specified path. Returns `true` if successful.

```csharp
bool removed = sj.Remove("user:age");
```

#### Clear()
Removes all data from the structure.

```csharp
sj.Clear();
```

## Examples

### Working with Complex Structures

```csharp
var sj = new StructuredJson();

// Create a user with multiple addresses
sj.Set("user:name", "John Doe");
sj.Set("user:age", 30);
sj.Set("user:isActive", true);

// Home address
sj.Set("user:addresses[0]:type", "home");
sj.Set("user:addresses[0]:street", "123 Main St");
sj.Set("user:addresses[0]:city", "Ankara");
sj.Set("user:addresses[0]:country", "Turkey");

// Work address
sj.Set("user:addresses[1]:type", "work");
sj.Set("user:addresses[1]:street", "456 Business Ave");
sj.Set("user:addresses[1]:city", "Istanbul");
sj.Set("user:addresses[1]:country", "Turkey");

// Hobbies array
sj.Set("user:hobbies[0]", "reading");
sj.Set("user:hobbies[1]", "swimming");
sj.Set("user:hobbies[2]", "coding");

// Convert to JSON
var json = sj.ToJson();
```

### Loading from Existing JSON

```csharp
var existingJson = """
{
    "config": {
        "database": {
            "host": "localhost",
            "port": 5432,
            "name": "mydb"
        },
        "features": ["auth", "logging", "caching"]
    }
}
""";

var sj = new StructuredJson(existingJson);

// Read values
var host = sj.Get("config:database:host");           // "localhost"
var port = sj.Get<int>("config:database:port");      // 5432
var firstFeature = sj.Get("config:features[0]");     // "auth"

// Modify values
sj.Set("config:database:host", "production-server");
sj.Set("config:features[3]", "monitoring");

// Get updated JSON
var updatedJson = sj.ToJson();
```

### Array Manipulation

```csharp
var sj = new StructuredJson();

// Create sparse array (automatically fills with nulls)
sj.Set("items[5]", "value at index 5");

// items[0] through items[4] will be null
var nullValue = sj.Get("items[0]"); // null
var actualValue = sj.Get("items[5]"); // "value at index 5"

// Add more items
sj.Set("items[0]", "first item");
sj.Set("items[1]", "second item");

// Remove an item (shifts indices)
sj.Remove("items[1]");
// Now items[1] contains "value at index 5"
```

## Error Handling

The library provides comprehensive error handling:

```csharp
try
{
    var sj = new StructuredJson();
    
    // These will throw ArgumentException
    sj.Set("", "value");        // Empty path
    sj.Set(null, "value");      // Null path
    sj.Get("");                 // Empty path
    
    // Invalid JSON in constructor
    var invalid = new StructuredJson("{invalid json}");
}
catch (ArgumentException ex)
{
    Console.WriteLine($"Invalid argument: {ex.Message}");
}
```

## Performance Considerations

- The library uses `Dictionary<string, object>` internally for O(1) key lookups
- Array operations maintain list semantics with appropriate performance characteristics
- JSON serialization leverages `System.Text.Json` for optimal performance
- Path parsing is optimized with regex caching

## Thread Safety

`StructuredJson` is **not thread-safe**. If you need to access the same instance from multiple threads, you should implement appropriate synchronization mechanisms.

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request. For major changes, please open an issue first to discuss what you would like to change.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Changelog

### Version 1.0.0
- Initial release
- Path-based JSON manipulation API
- Support for .NET Standard 2.0, .NET 8, and .NET 9
- Comprehensive unit test coverage
- Full XML documentation 