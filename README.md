# StructuredJson

[![NuGet Version](https://img.shields.io/nuget/v/StructuredJson.svg)](https://www.nuget.org/packages/StructuredJson/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

A powerful .NET library for creating, reading, and updating JSON objects using a path-based API. Built with `Dictionary<string, object?>` as the underlying data structure and `System.Text.Json` for serialization.

## Features

- **Path-based API**: Use intuitive path syntax to navigate and manipulate JSON structures
- **Multi-target Support**: Compatible with .NET Standard 2.0, .NET 8, and .NET 9
- **System.Text.Json**: Uses the modern, high-performance JSON serializer
- **Type Safety**: Generic methods with intelligent type conversion for strongly-typed value retrieval
- **Comprehensive**: Full CRUD operations with path validation and robust error handling
- **Smart Type Conversion**: Automatic conversion between strings and numbers, plus JsonElement handling
- **Array Support**: Full array manipulation with automatic null-filling for sparse arrays
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

// Get values with automatic type conversion
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

The library uses a simple and intuitive path syntax with comprehensive validation:

- **Object Properties**: Use `:` to separate object properties
  - `"user:name"` → `user.name`
  - `"config:database:host"` → `config.database.host`

- **Array Elements**: Use `[index]` to access array elements
  - `"users[0]"` → `users[0]`
  - `"items[2]:name"` → `items[2].name`

- **Complex Paths**: Combine objects and arrays
  - `"user:addresses[0]:city"` → `user.addresses[0].city`
  - `"data:items[1]:properties[0]:value"` → `data.items[1].properties[0].value`

### Path Validation

The library performs comprehensive path validation and throws `ArgumentException` for:
- Empty or null paths
- Empty array indices (`items[]`)
- Invalid array indices (non-numeric: `items[abc]`)
- Negative array indices (`items[-1]`)
- Multiple array indices in one segment (`items[0][1]`)
- Invalid path segments

## API Reference

### Constructors

```csharp
// Create empty structure
var sj = new StructuredJson();

// Create from JSON string (with validation)
var sj = new StructuredJson(jsonString);
```

### Core Methods

#### Set(string path, object? value)
Sets a value at the specified path. Creates nested structures and arrays automatically.

```csharp
sj.Set("user:name", "John");
sj.Set("user:addresses[0]:city", "Ankara");
sj.Set("items[5]", "value"); // Creates sparse array with nulls at indices 0-4
```

**Throws**: `ArgumentException` for invalid paths

#### Get(string path)
Gets a value from the specified path. Returns `null` if path doesn't exist.

```csharp
var value = sj.Get("user:name");
```

**Throws**: `ArgumentException` for invalid paths

#### Get<T>(string path)
Gets a strongly-typed value with intelligent type conversion.

```csharp
var age = sj.Get<int>("user:age");
var isActive = sj.Get<bool>("user:isActive");
var name = sj.Get<string>("user:name");

// Smart conversions
sj.Set("stringNumber", "42");
var number = sj.Get<int>("stringNumber");  // Returns 42

sj.Set("numberAsString", 123);
var text = sj.Get<string>("numberAsString"); // Returns "123"
```

**Type Conversion Features**:
- String ↔ Number conversions (int, long, double, decimal, float)
- JsonElement handling for complex deserialization
- Fallback to JsonSerializer for complex types
- Returns `default(T)` for failed conversions

#### ToJson(JsonSerializerOptions? options = null)
Converts the structure to a JSON string with optional formatting.

```csharp
var json = sj.ToJson(); // Pretty-printed by default
var compactJson = sj.ToJson(new JsonSerializerOptions { WriteIndented = false });
```

#### ListPaths()
Returns all paths and their values as a dictionary. Handles sparse arrays intelligently.

```csharp
var paths = sj.ListPaths();
// Returns: Dictionary<string, object?>
// Note: Only includes non-null values for array elements (sparse array support)
```

#### HasPath(string path)
Checks if a path exists in the structure with safe error handling.

```csharp
bool exists = sj.HasPath("user:name");
// Returns false for invalid paths instead of throwing
```

#### Remove(string path)
Removes a value at the specified path. For arrays, removes the element and shifts indices.

```csharp
bool removed = sj.Remove("user:age");
bool arrayRemoved = sj.Remove("items[1]"); // Removes and shifts remaining elements
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

// Read values with type conversion
var host = sj.Get("config:database:host");           // "localhost"
var port = sj.Get<int>("config:database:port");      // 5432
var firstFeature = sj.Get("config:features[0]");     // "auth"

// Modify values
sj.Set("config:database:host", "production-server");
sj.Set("config:features[3]", "monitoring");

// Get updated JSON
var updatedJson = sj.ToJson();
```

### Advanced Array Manipulation

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
// Now items[1] contains what was previously at items[2]

// ListPaths() only shows non-null array elements
var paths = sj.ListPaths();
// Won't include null array elements in the output
```

### Type Conversion Examples

```csharp
var sj = new StructuredJson();

// String to number conversions
sj.Set("stringInt", "42");
sj.Set("stringDouble", "3.14");
sj.Set("stringDecimal", "99.99");

var intValue = sj.Get<int>("stringInt");         // 42
var doubleValue = sj.Get<double>("stringDouble"); // 3.14
var decimalValue = sj.Get<decimal>("stringDecimal"); // 99.99

// Number to string conversions
sj.Set("numberValue", 123);
var stringValue = sj.Get<string>("numberValue"); // "123"

// Complex type handling via JsonSerializer
sj.Set("complexObject", new { Name = "Test", Value = 42 });
var complexResult = sj.Get<Dictionary<string, object>>("complexObject");
```

## Error Handling

The library provides comprehensive error handling with specific exception types:

```csharp
try
{
    var sj = new StructuredJson();
    
    // These will throw ArgumentException with descriptive messages
    sj.Set("", "value");           // "Path cannot be null or empty"
    sj.Set(null, "value");         // "Path cannot be null or empty"
    sj.Set("items[]", "value");    // "Empty array index in path"
    sj.Set("items[abc]", "value"); // "Invalid array index 'abc' in path"
    sj.Set("items[-1]", "value");  // "Negative array index '-1' in path"
    sj.Set("items[0][1]", "value"); // "Multiple array indices not supported"
    
    // Invalid JSON in constructor
    var invalid = new StructuredJson("{invalid json}"); // ArgumentException with JsonException inner
}
catch (ArgumentException ex)
{
    Console.WriteLine($"Invalid argument: {ex.Message}");
    if (ex.InnerException is JsonException jsonEx)
    {
        Console.WriteLine($"JSON Error: {jsonEx.Message}");
    }
}

// Safe methods that don't throw
bool exists = sj.HasPath("invalid[path");  // Returns false instead of throwing
bool removed = sj.Remove("invalid[path"); // Returns false instead of throwing
```

## Performance Considerations

- **Dictionary-based**: Uses `Dictionary<string, object?>` internally for O(1) key lookups
- **List semantics**: Array operations maintain proper list behavior with appropriate performance
- **System.Text.Json**: Leverages high-performance JSON serialization
- **Regex optimization**: Path parsing uses compiled regex patterns
- **Lazy evaluation**: Type conversions are performed only when needed
- **Memory efficient**: Sparse arrays don't allocate unnecessary null elements in ListPaths()

## Thread Safety

`StructuredJson` is **not thread-safe**. If you need to access the same instance from multiple threads, implement appropriate synchronization mechanisms such as:

```csharp
private readonly object _lock = new object();
private readonly StructuredJson _sj = new StructuredJson();

public void SafeSet(string path, object value)
{
    lock (_lock)
    {
        _sj.Set(path, value);
    }
}
```

## Best Practices

1. **Path Validation**: Always handle `ArgumentException` when working with dynamic paths
2. **Type Safety**: Use generic `Get<T>()` methods for better type safety
3. **Error Handling**: Use `HasPath()` to check existence before `Get()` operations
4. **Performance**: Cache `ListPaths()` results if called frequently
5. **Memory**: Call `Clear()` when reusing instances with large datasets

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request. For major changes, please open an issue first to discuss what you would like to change.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Changelog

### Version 1.0.0
- Initial release
- Path-based JSON manipulation API
- Support for .NET Standard 2.0, .NET 8, and .NET 9
- Intelligent type conversion system
- Comprehensive path validation
- Sparse array support
- Robust error handling
- Full XML documentation
- Comprehensive unit test coverage 