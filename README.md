# StructuredJson

[![NuGet Version](https://img.shields.io/nuget/v/StructuredJson.svg)](https://www.nuget.org/packages/StructuredJson/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET](https://img.shields.io/badge/.NET-Standard%202.0%20%7C%208.0%20%7C%209.0-purple.svg)](https://dotnet.microsoft.com/)

A powerful .NET library for creating, reading, and updating JSON objects using a path-based API. Built with `Dictionary<string, object?>` as the underlying data structure and `System.Text.Json` for modern, high-performance serialization.

## ✨ Features

- **🔗 Path-based API**: Intuitive path syntax for navigating and manipulating JSON structures
- **🌐 Cross-platform**: Full compatibility with .NET Standard 2.0, .NET 8, and .NET 9
- **⚡ High Performance**: Uses `System.Text.Json` for optimal serialization performance
- **🔄 Smart Type Conversion**: Intelligent conversion between strings, numbers, and complex types
- **📋 Full CRUD Operations**: Complete Create, Read, Update, Delete capabilities with robust error handling
- **🎯 Sparse Array Support**: Efficient handling of arrays with automatic null-filling for gaps
- **🌍 Unicode Ready**: Full support for international characters, emojis, and special symbols
- **📊 Path Validation**: Comprehensive validation with meaningful error messages
- **🔍 Path Discovery**: List all paths and values in your JSON structure
- **📖 Well Documented**: Complete XML documentation and extensive unit test coverage

## 📦 Installation

Install via NuGet Package Manager:

```bash
dotnet add package StructuredJson
```

Or via Package Manager Console:

```powershell
Install-Package StructuredJson
```

Or via PackageReference in your `.csproj`:

```xml
<PackageReference Include="StructuredJson" Version="1.0.0" />
```

## 🚀 Quick Start

```csharp
using StructuredJson;

// Create a new instance
var sj = new StructuredJson();

// Set values using intuitive path syntax
sj.Set("user:name", "John Doe");
sj.Set("user:age", 30);
sj.Set("user:isActive", true);
sj.Set("user:addresses[0]:city", "Ankara");
sj.Set("user:addresses[0]:country", "Turkey");
sj.Set("user:addresses[1]:city", "Istanbul");

// Get values with automatic type conversion
var name = sj.Get("user:name");                    // "John Doe"
var age = sj.Get<int>("user:age");                 // 30
var isActive = sj.Get<bool>("user:isActive");      // true
var city = sj.Get("user:addresses[0]:city");       // "Ankara"

// Convert to beautifully formatted JSON
var json = sj.ToJson();
Console.WriteLine(json);

// List all paths and values
var paths = sj.ListPaths();
foreach (var kvp in paths)
{
    Console.WriteLine($"{kvp.Key}: {kvp.Value}");
}
```

## 📍 Path Syntax

StructuredJson uses an intuitive and powerful path syntax with comprehensive validation:

### Object Properties
Use `:` to navigate object properties:
```csharp
sj.Set("user:name", "John");                    // user.name
sj.Set("config:database:host", "localhost");    // config.database.host
sj.Set("app:settings:theme", "dark");           // app.settings.theme
```

### Array Elements
Use `[index]` to access array elements:
```csharp
sj.Set("users[0]", "John");                     // users[0]
sj.Set("items[2]:name", "Product");             // items[2].name
sj.Set("data[5]:values[3]", 42);                // data[5].values[3]
```

### Complex Nested Paths
Combine objects and arrays seamlessly:
```csharp
sj.Set("user:addresses[0]:coordinates:lat", 39.9334);
sj.Set("products[1]:reviews[0]:rating", 5);
sj.Set("config:servers[2]:endpoints[0]:url", "https://api.example.com");
```

### Path Validation
The library provides comprehensive path validation:
- ✅ `"user:name"` - Valid object property
- ✅ `"items[0]"` - Valid array element
- ✅ `"data:list[5]:value"` - Valid nested path
- ❌ `"items[]"` - Invalid: empty array index
- ❌ `"items[-1]"` - Invalid: negative index
- ❌ `"items[abc]"` - Invalid: non-numeric index

## 🔧 API Reference

### Constructors

```csharp
// Create empty structure
var sj = new StructuredJson();

// Create from JSON string (with comprehensive validation)
var sj = new StructuredJson(jsonString);
```

### Core Methods

#### Set(string path, object? value)
Sets a value at the specified path, creating nested structures automatically:

```csharp
sj.Set("user:name", "John");
sj.Set("user:addresses[0]:city", "Ankara");
sj.Set("items[5]", "value");  // Creates sparse array with nulls at 0-4

// Supports all data types
sj.Set("numbers:int", 42);
sj.Set("numbers:decimal", 123.45m);
sj.Set("flags:isActive", true);
sj.Set("data:nullValue", null);
sj.Set("text:unicode", "🚀 Türkçe 你好");
```

**Throws**: `ArgumentException` for invalid paths

#### Get(string path) / Get<T>(string path)
Retrieves values with intelligent type conversion:

```csharp
// Basic retrieval
var name = sj.Get("user:name");          // Returns object
var age = sj.Get<int>("user:age");       // Returns strongly-typed int

// Smart type conversions
sj.Set("stringNumber", "42");
var number = sj.Get<int>("stringNumber");     // Returns 42 (int)

sj.Set("numberString", 123);
var text = sj.Get<string>("numberString");    // Returns "123" (string)

// Complex type handling
var user = sj.Get<UserModel>("user");         // Deserializes to custom type
```

**Type Conversion Features**:
- String ↔ Number conversions (int, long, double, decimal, float)
- JsonElement handling for complex deserialization
- Automatic type detection and conversion
- Returns `default(T)` for failed conversions

#### HasPath(string path)
Safely checks if a path exists:

```csharp
bool exists = sj.HasPath("user:name");        // true/false
bool invalid = sj.HasPath("invalid[]path");   // false (doesn't throw)
```

#### Remove(string path)
Removes values with intelligent array handling:

```csharp
bool removed = sj.Remove("user:age");         // Removes property
bool arrayRemoved = sj.Remove("items[1]");    // Removes and shifts array elements
```

#### ListPaths()
Discovers all paths and values in your structure:

```csharp
var paths = sj.ListPaths();
// Returns: Dictionary<string, object?>
// Example output:
// "user:name" -> "John"
// "user:addresses[0]:city" -> "Ankara"
// "user:addresses[1]:city" -> "Istanbul"
```

#### ToJson(JsonSerializerOptions? options = null)
Converts to JSON with flexible formatting:

```csharp
var prettyJson = sj.ToJson();                 // Pretty-printed (default)
var compactJson = sj.ToJson(new JsonSerializerOptions { 
    WriteIndented = false 
});
```

#### Clear()
Removes all data from the structure:

```csharp
sj.Clear();  // Structure becomes empty: {}
```

## 🎯 Advanced Features

### Sparse Array Support
Automatically handles arrays with gaps:

```csharp
var sj = new StructuredJson();
sj.Set("items[0]", "first");
sj.Set("items[5]", "sixth");     // Automatically fills [1-4] with null

var json = sj.ToJson();
// Result: {"items": ["first", null, null, null, null, "sixth"]}
```

### Unicode and International Support
Full support for international characters:

```csharp
sj.Set("turkish", "Türkçe karakterler: ğüşıöç");
sj.Set("emoji", "🚀 🎉 🌟 💻");
sj.Set("chinese", "你好世界");
sj.Set("arabic", "مرحبا بالعالم");

// All perfectly preserved in JSON output
```

### Complex Object Handling
Works seamlessly with custom objects:

```csharp
public class Address
{
    public string City { get; set; }
    public string Country { get; set; }
    public double[] Coordinates { get; set; }
}

var address = new Address 
{ 
    City = "Ankara", 
    Country = "Turkey", 
    Coordinates = new[] { 39.9334, 32.8597 } 
};

sj.Set("user:address", address);
var retrievedAddress = sj.Get<Address>("user:address");
```

### Performance Optimizations
Handles large-scale operations efficiently:

```csharp
// Efficient for large datasets
for (int i = 0; i < 10000; i++)
{
    sj.Set($"data:items[{i}]:id", i);
    sj.Set($"data:items[{i}]:value", $"Item {i}");
}

// Fast path-based lookups with O(1) dictionary access
var item5000 = sj.Get("data:items[5000]:value");
```

## 📊 Real-World Examples

### User Profile Management
```csharp
var profile = new StructuredJson();

// Basic info
profile.Set("user:id", 12345);
profile.Set("user:name", "John Doe");
profile.Set("user:email", "john@example.com");
profile.Set("user:isVerified", true);

// Multiple addresses
profile.Set("user:addresses[0]:type", "home");
profile.Set("user:addresses[0]:street", "123 Main St");
profile.Set("user:addresses[0]:city", "Ankara");
profile.Set("user:addresses[0]:country", "Turkey");

profile.Set("user:addresses[1]:type", "work"); 
profile.Set("user:addresses[1]:street", "456 Business Ave");
profile.Set("user:addresses[1]:city", "Istanbul");
profile.Set("user:addresses[1]:country", "Turkey");

// Preferences
profile.Set("user:preferences:theme", "dark");
profile.Set("user:preferences:language", "tr-TR");
profile.Set("user:preferences:notifications:email", true);
profile.Set("user:preferences:notifications:sms", false);

// Access data
var homeAddress = profile.Get("user:addresses[0]:city");  // "Ankara"
var emailNotifications = profile.Get<bool>("user:preferences:notifications:email");  // true
```

### Configuration Management
```csharp
var config = new StructuredJson();

// Database settings
config.Set("database:host", "localhost");
config.Set("database:port", 5432);
config.Set("database:name", "myapp");
config.Set("database:ssl", true);

// API endpoints
config.Set("api:endpoints[0]:name", "users");
config.Set("api:endpoints[0]:url", "/api/v1/users");
config.Set("api:endpoints[0]:methods[0]", "GET");
config.Set("api:endpoints[0]:methods[1]", "POST");

config.Set("api:endpoints[1]:name", "products");
config.Set("api:endpoints[1]:url", "/api/v1/products");
config.Set("api:endpoints[1]:methods[0]", "GET");

// Feature flags
config.Set("features:newDashboard", true);
config.Set("features:betaFeatures", false);
config.Set("features:maintenanceMode", false);

// Export to configuration file
File.WriteAllText("appsettings.json", config.ToJson());
```

### E-commerce Product Catalog
```csharp
var catalog = new StructuredJson();

// Product 1
catalog.Set("products[0]:id", "P001");
catalog.Set("products[0]:name", "Laptop");
catalog.Set("products[0]:price", 999.99m);
catalog.Set("products[0]:currency", "USD");
catalog.Set("products[0]:inStock", true);

catalog.Set("products[0]:specifications:cpu", "Intel i7");
catalog.Set("products[0]:specifications:ram", "16GB");
catalog.Set("products[0]:specifications:storage", "512GB SSD");

catalog.Set("products[0]:reviews[0]:rating", 5);
catalog.Set("products[0]:reviews[0]:comment", "Excellent laptop!");
catalog.Set("products[0]:reviews[1]:rating", 4);
catalog.Set("products[0]:reviews[1]:comment", "Good value for money");

// Product 2
catalog.Set("products[1]:id", "P002");
catalog.Set("products[1]:name", "Smartphone");
catalog.Set("products[1]:price", 599.99m);
catalog.Set("products[1]:inStock", false);

// Query products
var laptopPrice = catalog.Get<decimal>("products[0]:price");  // 999.99
var laptopRating = catalog.Get<int>("products[0]:reviews[0]:rating");  // 5
var phoneInStock = catalog.Get<bool>("products[1]:inStock");  // false
```

## 🛠️ Error Handling

StructuredJson provides comprehensive error handling:

```csharp
try
{
    var sj = new StructuredJson();
    
    // These will throw ArgumentException with descriptive messages:
    sj.Set("", "value");           // Empty path
    sj.Set("items[]", "value");    // Empty array index
    sj.Set("items[abc]", "value"); // Invalid array index
    sj.Set("items[-1]", "value");  // Negative array index
}
catch (ArgumentException ex)
{
    Console.WriteLine($"Path error: {ex.Message}");
}

try
{
    // Invalid JSON in constructor
    var sj = new StructuredJson("{invalid json}");
}
catch (ArgumentException ex)
{
    Console.WriteLine($"JSON parsing error: {ex.Message}");
}
```

## 🔍 Path Discovery and Debugging

Easily explore your JSON structure:

```csharp
var sj = new StructuredJson();
sj.Set("user:name", "John");
sj.Set("user:addresses[0]:city", "Ankara");
sj.Set("user:addresses[1]:city", "Istanbul");
sj.Set("settings:theme", "dark");

// List all paths
var paths = sj.ListPaths();
foreach (var kvp in paths)
{
    Console.WriteLine($"{kvp.Key}: {kvp.Value}");
}

// Output:
// user:name: John
// user:addresses[0]:city: Ankara
// user:addresses[1]:city: Istanbul
// settings:theme: dark

// Check if specific paths exist
bool hasUserName = sj.HasPath("user:name");        // true
bool hasUserAge = sj.HasPath("user:age");          // false
bool hasFirstAddress = sj.HasPath("user:addresses[0]");  // true
```

## 🏗️ Architecture and Performance

- **Underlying Structure**: `Dictionary<string, object?>` for O(1) key lookups
- **Serialization**: `System.Text.Json` for modern, high-performance JSON handling
- **Memory Efficient**: Sparse arrays don't allocate unnecessary memory
- **Path Parsing**: Optimized regex-based path parsing with caching
- **Type Conversion**: Lazy evaluation with intelligent fallback strategies
- **Cross-platform**: Full compatibility across Windows, macOS, and Linux

## 🧪 Framework Compatibility

- **.NET Standard 2.0**: Maximum compatibility with all .NET implementations
- **.NET 8.0**: Latest performance optimizations and nullable reference types
- **.NET 9.0**: Cutting-edge features and improvements
- **Cross-platform**: Windows, macOS, Linux support
- **Legacy Support**: Works with .NET Framework 4.6.1+

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🤝 Contributing

Contributions are welcome! Please read our [Contributing Guide](CONTRIBUTING.md) for details on our code of conduct and the process for submitting pull requests.

## 📋 Changelog

See [CHANGELOG.md](CHANGELOG.md) for a detailed list of changes and version history.

## 🐛 Issues and Support

- **Bug Reports**: [GitHub Issues](https://github.com/adomorn/StructuredJson/issues)
- **Feature Requests**: [GitHub Discussions](https://github.com/adomorn/StructuredJson/discussions)
- **Documentation**: This README and XML documentation in the code

---

**Made with ❤️ for the .NET community** 