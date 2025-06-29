#if NET8_0_OR_GREATER
// Global usings are enabled for .NET 8+
using System;
using System.Text.Json;
using Xunit;
#else
using System;
using System.Collections.Generic;
using System.Text.Json;
using Xunit;
using System.Linq;
#endif

namespace StructuredJson.Tests
{
    /// <summary>
    /// Comprehensive unit tests for the StructuredJson library.
    /// </summary>
    public class StructuredJsonTests
    {
        [Fact]
        public void Constructor_EmptyConstructor_CreatesEmptyStructure()
        {
            // Arrange & Act
            var sj = new StructuredJson();

            // Assert
            Assert.Equal("{}", sj.ToJson(new JsonSerializerOptions { WriteIndented = false }));
        }

        [Fact]
        public void Constructor_ValidJsonString_ParsesCorrectly()
        {
            // Arrange
            var json = """{"name": "John", "age": 30}""";

            // Act
            var sj = new StructuredJson(json);

            // Assert
            Assert.Equal("John", sj.Get("name"));
            Assert.Equal(30, sj.Get<int>("age"));
        }

        [Fact]
        public void Constructor_InvalidJsonString_ThrowsArgumentException()
        {
            // Arrange
            var invalidJson = """{"name": "John", "age":}""";

            // Act & Assert
            Assert.Throws<ArgumentException>(() => new StructuredJson(invalidJson));
        }

        [Fact]
        public void Set_SimpleProperty_SetsValueCorrectly()
        {
            // Arrange
            var sj = new StructuredJson();

            // Act
            sj.Set("name", "John");

            // Assert
            Assert.Equal("John", sj.Get("name"));
        }

        [Fact]
        public void Set_NestedProperty_CreatesNestedStructure()
        {
            // Arrange
            var sj = new StructuredJson();

            // Act
            sj.Set("user:name", "John");
            sj.Set("user:age", 30);

            // Assert
            Assert.Equal("John", sj.Get("user:name"));
            Assert.Equal(30, sj.Get<int>("user:age"));
        }

        [Fact]
        public void Set_ArrayProperty_CreatesArrayStructure()
        {
            // Arrange
            var sj = new StructuredJson();

            // Act
            sj.Set("users[0]", "John");
            sj.Set("users[1]", "Jane");

            // Assert
            Assert.Equal("John", sj.Get("users[0]"));
            Assert.Equal("Jane", sj.Get("users[1]"));
        }

        [Fact]
        public void Set_NestedArrayProperty_CreatesComplexStructure()
        {
            // Arrange
            var sj = new StructuredJson();

            // Act
            sj.Set("user:addresses[0]:city", "Ankara");
            sj.Set("user:addresses[0]:country", "Turkey");
            sj.Set("user:addresses[1]:city", "Istanbul");

            // Assert
            Assert.Equal("Ankara", sj.Get("user:addresses[0]:city"));
            Assert.Equal("Turkey", sj.Get("user:addresses[0]:country"));
            Assert.Equal("Istanbul", sj.Get("user:addresses[1]:city"));
        }

        [Fact]
        public void Set_NullOrEmptyPath_ThrowsArgumentException()
        {
            // Arrange
            var sj = new StructuredJson();

            // Act & Assert
            Assert.Throws<ArgumentException>(() => sj.Set("", "value"));
#if NET8_0_OR_GREATER
            Assert.Throws<ArgumentException>(() => sj.Set(null!, "value"));
#else
            Assert.Throws<ArgumentException>(() => sj.Set(null, "value"));
#endif
            sj.Set("   ", "value");
            Assert.Equal("value", sj.Get("   "));
        }

        [Fact]
        public void Get_ExistingPath_ReturnsCorrectValue()
        {
            // Arrange
            var sj = new StructuredJson();
            sj.Set("name", "John");

            // Act
            var result = sj.Get("name");

            // Assert
            Assert.Equal("John", result);
        }

        [Fact]
        public void Get_NonExistingPath_ReturnsNull()
        {
            // Arrange
            var sj = new StructuredJson();

            // Act
            var result = sj.Get("nonexistent");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void Get_NullOrEmptyPath_ThrowsArgumentException()
        {
            // Arrange
            var sj = new StructuredJson();

            // Act & Assert
            Assert.Throws<ArgumentException>(() => sj.Get(""));
#if NET8_0_OR_GREATER
            Assert.Throws<ArgumentException>(() => sj.Get(null!));
#else
            Assert.Throws<ArgumentException>(() => sj.Get(null));
#endif
            sj.Set("   ", "test");
            Assert.Equal("test", sj.Get("   "));
        }

        [Fact]
        public void GetGeneric_ExistingPath_ReturnsTypedValue()
        {
            // Arrange
            var sj = new StructuredJson();
            sj.Set("age", 30);
            sj.Set("name", "John");
            sj.Set("isActive", true);

            // Act & Assert
            Assert.Equal(30, sj.Get<int>("age"));
            Assert.Equal("John", sj.Get<string>("name"));
            Assert.True(sj.Get<bool>("isActive"));
        }

        [Fact]
        public void GetGeneric_NonExistingPath_ReturnsDefault()
        {
            // Arrange
            var sj = new StructuredJson();

            // Act & Assert
            Assert.Equal(0, sj.Get<int>("nonexistent"));
            Assert.Null(sj.Get<string>("nonexistent"));
            Assert.False(sj.Get<bool>("nonexistent"));
        }

        [Fact]
        public void ToJson_SimpleStructure_ReturnsCorrectJson()
        {
            // Arrange
            var sj = new StructuredJson();
            sj.Set("name", "John");
            sj.Set("age", 30);

            // Act
            var json = sj.ToJson(new JsonSerializerOptions { WriteIndented = false });

            // Assert
            Assert.Contains("\"name\":\"John\"", json);
            Assert.Contains("\"age\":30", json);
        }

        [Fact]
        public void ToJson_ComplexStructure_ReturnsCorrectJson()
        {
            // Arrange
            var sj = new StructuredJson();
            sj.Set("user:name", "John");
            sj.Set("user:addresses[0]:city", "Ankara");

            // Act
            var json = sj.ToJson(new JsonSerializerOptions { WriteIndented = false });

            // Assert
            Assert.Contains("\"user\":", json);
            Assert.Contains("\"name\":\"John\"", json);
            Assert.Contains("\"addresses\":", json);
            Assert.Contains("\"city\":\"Ankara\"", json);
        }

        [Fact]
        public void ListPaths_SimpleStructure_ReturnsAllPaths()
        {
            // Arrange
            var sj = new StructuredJson();
            sj.Set("name", "John");
            sj.Set("age", 30);

            // Act
            var paths = sj.ListPaths();

            // Assert
            Assert.Equal(2, paths.Count);
            Assert.Equal("John", paths["name"]);
            Assert.Equal(30, paths["age"]);
        }

        [Fact]
        public void ListPaths_ComplexStructure_ReturnsAllPaths()
        {
            // Arrange
            var sj = new StructuredJson();
            sj.Set("user:name", "John");
            sj.Set("user:addresses[0]:city", "Ankara");

            // Act
            var paths = sj.ListPaths();

            // Assert
            Assert.True(paths.ContainsKey("user:name"));
            Assert.True(paths.ContainsKey("user:addresses[0]:city"));
            Assert.Equal("John", paths["user:name"]);
            Assert.Equal("Ankara", paths["user:addresses[0]:city"]);
        }

        [Fact]
        public void HasPath_ExistingPath_ReturnsTrue()
        {
            // Arrange
            var sj = new StructuredJson();
            sj.Set("user:name", "John");

            // Act & Assert
            Assert.True(sj.HasPath("user:name"));
        }

        [Fact]
        public void HasPath_NonExistingPath_ReturnsFalse()
        {
            // Arrange
            var sj = new StructuredJson();
            sj.Set("user:name", "John");

            // Act & Assert
            Assert.False(sj.HasPath("user:age"));
            Assert.False(sj.HasPath("nonexistent"));
        }

        [Fact]
        public void HasPath_NullOrEmptyPath_ReturnsFalse()
        {
            // Arrange
            var sj = new StructuredJson();

            // Act & Assert
            Assert.False(sj.HasPath(""));
            Assert.False(sj.HasPath(null!));
        }

        [Fact]
        public void Remove_ExistingPath_RemovesAndReturnsTrue()
        {
            // Arrange
            var sj = new StructuredJson();
            sj.Set("name", "John");
            sj.Set("age", 30);

            // Act
            var result = sj.Remove("name");

            // Assert
            Assert.True(result);
            Assert.Null(sj.Get("name"));
            Assert.Equal(30, sj.Get<int>("age")); // Other values should remain
        }

        [Fact]
        public void Remove_NonExistingPath_ReturnsFalse()
        {
            // Arrange
            var sj = new StructuredJson();
            sj.Set("name", "John");

            // Act
            var result = sj.Remove("nonexistent");

            // Assert
            Assert.False(result);
            Assert.Equal("John", sj.Get("name")); // Existing values should remain
        }

        [Fact]
        public void Remove_ArrayElement_RemovesCorrectly()
        {
            // Arrange
            var sj = new StructuredJson();
            sj.Set("items[0]", "first");
            sj.Set("items[1]", "second");
            sj.Set("items[2]", "third");

            // Act
            var result = sj.Remove("items[1]");

            // Assert
            Assert.True(result);
            Assert.Equal("first", sj.Get("items[0]"));
            Assert.Equal("third", sj.Get("items[1]")); // Should shift down
            Assert.Null(sj.Get("items[2]")); // Should no longer exist
        }

        [Fact]
        public void Remove_NullOrEmptyPath_ReturnsFalse()
        {
            // Arrange
            var sj = new StructuredJson();
            sj.Set("name", "John");

            // Act & Assert
            Assert.False(sj.Remove(""));
            Assert.False(sj.Remove(null!));
            Assert.Equal("John", sj.Get("name")); // Should remain unchanged
        }

        [Fact]
        public void Clear_WithData_RemovesAllData()
        {
            // Arrange
            var sj = new StructuredJson();
            sj.Set("name", "John");
            sj.Set("user:age", 30);
            sj.Set("items[0]", "test");

            // Act
            sj.Clear();

            // Assert
            Assert.Equal("{}", sj.ToJson(new JsonSerializerOptions { WriteIndented = false }));
            Assert.Null(sj.Get("name"));
            Assert.Null(sj.Get("user:age"));
            Assert.Null(sj.Get("items[0]"));
        }

        [Fact]
        public void ArrayHandling_SparseArray_FillsWithNulls()
        {
            // Arrange
            var sj = new StructuredJson();

            // Act
            sj.Set("items[5]", "value");

            // Assert
            Assert.Null(sj.Get("items[0]"));
            Assert.Null(sj.Get("items[1]"));
            Assert.Null(sj.Get("items[2]"));
            Assert.Null(sj.Get("items[3]"));
            Assert.Null(sj.Get("items[4]"));
            Assert.Equal("value", sj.Get("items[5]"));

            // ListPaths should only include non-null values
            var paths = sj.ListPaths();
            Assert.Single(paths);
            Assert.Equal("value", paths["items[5]"]);
        }

        [Fact]
        public void ComplexScenario_UserWithMultipleAddresses_WorksCorrectly()
        {
            // Arrange
            var sj = new StructuredJson();

            // Act
            sj.Set("user:name", "John Doe");
            sj.Set("user:age", 30);
            sj.Set("user:addresses[0]:type", "home");
            sj.Set("user:addresses[0]:city", "Ankara");
            sj.Set("user:addresses[0]:country", "Turkey");
            sj.Set("user:addresses[1]:type", "work");
            sj.Set("user:addresses[1]:city", "Istanbul");
            sj.Set("user:addresses[1]:country", "Turkey");
            sj.Set("user:hobbies[0]", "reading");
            sj.Set("user:hobbies[1]", "swimming");

            // Assert
            Assert.Equal("John Doe", sj.Get("user:name"));
            Assert.Equal(30, sj.Get<int>("user:age"));
            Assert.Equal("home", sj.Get("user:addresses[0]:type"));
            Assert.Equal("Ankara", sj.Get("user:addresses[0]:city"));
            Assert.Equal("Turkey", sj.Get("user:addresses[0]:country"));
            Assert.Equal("work", sj.Get("user:addresses[1]:type"));
            Assert.Equal("Istanbul", sj.Get("user:addresses[1]:city"));
            Assert.Equal("Turkey", sj.Get("user:addresses[1]:country"));
            Assert.Equal("reading", sj.Get("user:hobbies[0]"));
            Assert.Equal("swimming", sj.Get("user:hobbies[1]"));

            // Test JSON output
            var json = sj.ToJson();
            Assert.Contains("John Doe", json);
            Assert.Contains("Ankara", json);
            Assert.Contains("Istanbul", json);
            Assert.Contains("reading", json);
            Assert.Contains("swimming", json);

            // Test path listing
            var paths = sj.ListPaths();
            Assert.True(paths.Count >= 10); // Should have all the paths we set
        }

        [Fact]
        public void JsonRoundTrip_ComplexStructure_MaintainsData()
        {
            // Arrange
            var originalJson = """
            {
                "user": {
                    "name": "John",
                    "age": 30,
                    "addresses": [
                        {"city": "Ankara", "country": "Turkey"},
                        {"city": "Istanbul", "country": "Turkey"}
                    ]
                }
            }
            """;

            // Act
            var sj = new StructuredJson(originalJson);
            var recreatedJson = sj.ToJson();
            var sj2 = new StructuredJson(recreatedJson);

            // Assert
            Assert.Equal("John", sj2.Get("user:name"));
            Assert.Equal(30, sj2.Get<int>("user:age"));
            Assert.Equal("Ankara", sj2.Get("user:addresses[0]:city"));
            Assert.Equal("Turkey", sj2.Get("user:addresses[0]:country"));
            Assert.Equal("Istanbul", sj2.Get("user:addresses[1]:city"));
            Assert.Equal("Turkey", sj2.Get("user:addresses[1]:country"));
        }

        [Fact]
        public void PathParsing_SpecialCharacters_HandlesCorrectly()
        {
            // Arrange
            var sj = new StructuredJson();

            // Act & Assert
            sj.Set("key with spaces", "value1");
            Assert.Equal("value1", sj.Get("key with spaces"));

            sj.Set("key-with-dashes", "value2");
            Assert.Equal("value2", sj.Get("key-with-dashes"));

            sj.Set("key_with_underscores", "value3");
            Assert.Equal("value3", sj.Get("key_with_underscores"));
        }

        [Fact]
        public void OverwriteExistingValue_ReplacesCorrectly()
        {
            // Arrange
            var sj = new StructuredJson();
            sj.Set("name", "John");

            // Act
            sj.Set("name", "Jane");

            // Assert
            Assert.Equal("Jane", sj.Get("name"));
        }

        [Fact]
        public void OverwriteObjectWithPrimitive_ReplacesCorrectly()
        {
            // Arrange
            var sj = new StructuredJson();
            sj.Set("user:name", "John");
            sj.Set("user:age", 30);

            // Act
            sj.Set("user", "Simple String");

            // Assert
            Assert.Equal("Simple String", sj.Get("user"));
            Assert.Null(sj.Get("user:name")); // Should no longer exist
            Assert.Null(sj.Get("user:age")); // Should no longer exist
        }
    }
} 