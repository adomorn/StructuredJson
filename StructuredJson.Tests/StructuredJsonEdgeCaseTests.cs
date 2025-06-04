using System;
using System.Collections.Generic;
using System.Text.Json;
using Xunit;
using System.Linq;
using System.Globalization;

namespace StructuredJson.Tests
{
    /// <summary>
    /// Comprehensive edge case tests for the StructuredJson library.
    /// </summary>
    public class StructuredJsonEdgeCaseTests
    {
        #region Path Parsing Edge Cases

        [Fact]
        public void PathParsing_EmptyArrayIndex_ThrowsOrHandlesGracefully()
        {
            // Arrange
            var sj = new StructuredJson();

            // Act & Assert - Should handle gracefully or throw meaningful exception
            var exception = Record.Exception(() => sj.Set("items[]", "value"));
            Assert.NotNull(exception); // Should throw because empty array index is invalid
        }

        [Fact]
        public void PathParsing_NegativeArrayIndex_ThrowsOrHandlesGracefully()
        {
            // Arrange
            var sj = new StructuredJson();

            // Act & Assert
            var exception = Record.Exception(() => sj.Set("items[-1]", "value"));
            Assert.NotNull(exception); // Should throw because negative index is invalid
        }

        [Fact]
        public void PathParsing_InvalidArrayIndex_ThrowsOrHandlesGracefully()
        {
            // Arrange
            var sj = new StructuredJson();

            // Act & Assert
            var exception = Record.Exception(() => sj.Set("items[abc]", "value"));
            Assert.NotNull(exception); // Should throw because non-numeric index is invalid
        }

        [Fact]
        public void PathParsing_VeryLargeArrayIndex_HandlesCorrectly()
        {
            // Arrange
            var sj = new StructuredJson();

            // Act & Assert - Should either work or throw OutOfMemoryException
            var exception = Record.Exception(() => sj.Set("items[999999]", "value"));
            
            if (exception == null)
            {
                // If it works, verify the value is set
                Assert.Equal("value", sj.Get("items[999999]"));
            }
            else
            {
                // Should be OutOfMemoryException or similar
                Assert.True(exception is OutOfMemoryException || exception is ArgumentException);
            }
        }

        [Fact]
        public void PathParsing_MultipleArrayIndices_ThrowsOrHandlesGracefully()
        {
            // Arrange
            var sj = new StructuredJson();

            // Act & Assert - items[0][1] should be invalid syntax
            var exception = Record.Exception(() => sj.Set("items[0][1]", "value"));
            Assert.NotNull(exception); // Should throw because this syntax is not supported
        }

        [Fact]
        public void PathParsing_SpecialCharactersInPropertyNames_HandlesCorrectly()
        {
            // Arrange
            var sj = new StructuredJson();

            // Act
            sj.Set("user-info:full_name", "John Doe");
            sj.Set("config:api_key", "abc123");
            sj.Set("data:some.property", "value");
            sj.Set("test:prop@domain", "email-like");

            // Assert
            Assert.Equal("John Doe", sj.Get("user-info:full_name"));
            Assert.Equal("abc123", sj.Get("config:api_key"));
            Assert.Equal("value", sj.Get("data:some.property"));
            Assert.Equal("email-like", sj.Get("test:prop@domain"));
        }

        [Fact]
        public void PathParsing_WhitespaceInPaths_HandlesCorrectly()
        {
            // Arrange
            var sj = new StructuredJson();

            // Act & Assert - Paths with whitespace should be handled consistently
            sj.Set("user: name", "John"); // Space after colon
            sj.Set("user :age", 30); // Space before colon
            sj.Set("user : city", "Ankara"); // Spaces around colon

            Assert.Equal("John", sj.Get("user: name"));
            Assert.Equal(30, sj.Get<int>("user :age"));
            Assert.Equal("Ankara", sj.Get("user : city"));
        }

        #endregion

        #region Data Type Edge Cases

        [Fact]
        public void DataTypes_NullValues_HandlesCorrectly()
        {
            // Arrange
            var sj = new StructuredJson();

            // Act
            sj.Set("nullValue", null);
            sj.Set("user:name", "John");
            sj.Set("user:spouse", null);

            // Assert
            Assert.Null(sj.Get("nullValue"));
            Assert.Null(sj.Get("user:spouse"));
            Assert.Equal("John", sj.Get("user:name"));
            Assert.True(sj.HasPath("nullValue"));
            Assert.True(sj.HasPath("user:spouse"));
        }

        [Fact]
        public void DataTypes_LargeNumbers_HandlesCorrectly()
        {
            // Arrange
            var sj = new StructuredJson();

            // Act
            sj.Set("longValue", long.MaxValue);
            sj.Set("longMinValue", long.MinValue);
            sj.Set("decimalValue", decimal.MaxValue);
            sj.Set("doubleValue", double.MaxValue);

            // Assert
            Assert.Equal(long.MaxValue, sj.Get<long>("longValue"));
            Assert.Equal(long.MinValue, sj.Get<long>("longMinValue"));
            Assert.Equal(decimal.MaxValue, sj.Get<decimal>("decimalValue"));
            Assert.Equal(double.MaxValue, sj.Get<double>("doubleValue"));
        }

        [Fact]
        public void DataTypes_UnicodeCharacters_HandlesCorrectly()
        {
            // Arrange
            var sj = new StructuredJson();

            // Act
            sj.Set("turkish", "Türkçe karakterler: ğüşıöç");
            sj.Set("emoji", "🚀 🎉 🌟");
            sj.Set("chinese", "你好世界");
            sj.Set("arabic", "مرحبا بالعالم");

            // Assert
            Assert.Equal("Türkçe karakterler: ğüşıöç", sj.Get("turkish"));
            Assert.Equal("🚀 🎉 🌟", sj.Get("emoji"));
            Assert.Equal("你好世界", sj.Get("chinese"));
            Assert.Equal("مرحبا بالعالم", sj.Get("arabic"));
        }

        [Fact]
        public void DataTypes_EmptyStrings_HandlesCorrectly()
        {
            // Arrange
            var sj = new StructuredJson();

            // Act
            sj.Set("emptyString", "");
            sj.Set("user:name", "");
            sj.Set("array[0]", "");

            // Assert
            Assert.Equal("", sj.Get("emptyString"));
            Assert.Equal("", sj.Get("user:name"));
            Assert.Equal("", sj.Get("array[0]"));
            Assert.True(sj.HasPath("emptyString"));
        }

        [Fact]
        public void DataTypes_BooleanValues_HandlesCorrectly()
        {
            // Arrange
            var sj = new StructuredJson();

            // Act
            sj.Set("isActive", true);
            sj.Set("isDeleted", false);
            sj.Set("user:isAdmin", true);

            // Assert
            Assert.True(sj.Get<bool>("isActive"));
            Assert.False(sj.Get<bool>("isDeleted"));
            Assert.True(sj.Get<bool>("user:isAdmin"));
        }

        [Fact]
        public void DataTypes_DateTimeValues_HandlesCorrectly()
        {
            // Arrange
            var sj = new StructuredJson();
            var now = DateTime.Now;
            var utcNow = DateTime.UtcNow;

            // Act
            sj.Set("createdAt", now);
            sj.Set("updatedAt", utcNow);

            // Assert
            Assert.Equal(now, sj.Get<DateTime>("createdAt"));
            Assert.Equal(utcNow, sj.Get<DateTime>("updatedAt"));
        }

        [Fact]
        public void DataTypes_ComplexObjects_HandlesCorrectly()
        {
            // Arrange
            var sj = new StructuredJson();
            var complexObject = new
            {
                Name = "John",
                Age = 30,
                Address = new { City = "Ankara", Country = "Turkey" }
            };

            // Act
            sj.Set("user", complexObject);

            // Assert
            var retrieved = sj.Get("user");
            Assert.NotNull(retrieved);
            
            // Should be able to serialize back to JSON
            var json = sj.ToJson();
            Assert.Contains("John", json);
            Assert.Contains("Ankara", json);
        }

        #endregion

        #region Array Handling Edge Cases

        [Fact]
        public void ArrayHandling_ConvertArrayToObject_HandlesCorrectly()
        {
            // Arrange
            var sj = new StructuredJson();
            sj.Set("data[0]", "item1");
            sj.Set("data[1]", "item2");

            // Act - Convert array to object
            sj.Set("data:name", "converted");

            // Assert
            Assert.Equal("converted", sj.Get("data:name"));
            // Original array items should be gone
            Assert.Null(sj.Get("data[0]"));
        }

        [Fact]
        public void ArrayHandling_ConvertObjectToArray_HandlesCorrectly()
        {
            // Arrange
            var sj = new StructuredJson();
            sj.Set("data:name", "John");
            sj.Set("data:age", 30);

            // Act - Convert object to array
            sj.Set("data[0]", "item1");

            // Assert
            Assert.Equal("item1", sj.Get("data[0]"));
            // Original object properties should be gone
            Assert.Null(sj.Get("data:name"));
        }

        [Fact]
        public void ArrayHandling_RemoveMiddleElement_ShiftsIndices()
        {
            // Arrange
            var sj = new StructuredJson();
            sj.Set("items[0]", "first");
            sj.Set("items[1]", "second");
            sj.Set("items[2]", "third");

            // Act
            var removed = sj.Remove("items[1]");

            // Assert
            Assert.True(removed);
            Assert.Equal("first", sj.Get("items[0]"));
            Assert.Equal("third", sj.Get("items[1]")); // Should shift down
            Assert.Null(sj.Get("items[2]")); // Should be null now
        }

        [Fact]
        public void ArrayHandling_SparseArrayWithLargeGaps_HandlesCorrectly()
        {
            // Arrange
            var sj = new StructuredJson();

            // Act
            sj.Set("items[0]", "first");
            sj.Set("items[100]", "hundred");

            // Assert
            Assert.Equal("first", sj.Get("items[0]"));
            Assert.Equal("hundred", sj.Get("items[100]"));
            
            // Check some middle indices are null
            Assert.Null(sj.Get("items[50]"));
            Assert.Null(sj.Get("items[99]"));
            
            // Verify array size
            var paths = sj.ListPaths();
            var arrayPaths = paths.Keys.Where(k => k.StartsWith("items[")).ToList();
            Assert.Equal(2, arrayPaths.Count); // Only non-null items should be listed
        }

        [Fact]
        public void ArrayHandling_NestedArrays_HandlesCorrectly()
        {
            // Arrange
            var sj = new StructuredJson();

            // Act
            sj.Set("matrix[0]:values[0]", 1);
            sj.Set("matrix[0]:values[1]", 2);
            sj.Set("matrix[1]:values[0]", 3);
            sj.Set("matrix[1]:values[1]", 4);

            // Assert
            Assert.Equal(1, sj.Get<int>("matrix[0]:values[0]"));
            Assert.Equal(2, sj.Get<int>("matrix[0]:values[1]"));
            Assert.Equal(3, sj.Get<int>("matrix[1]:values[0]"));
            Assert.Equal(4, sj.Get<int>("matrix[1]:values[1]"));
        }

        #endregion

        #region Memory and Performance Edge Cases

        [Fact]
        public void Performance_DeepNestedStructure_HandlesCorrectly()
        {
            // Arrange
            var sj = new StructuredJson();
            var deepPath = string.Join(":", Enumerable.Range(0, 50).Select(i => $"level{i}"));

            // Act
            sj.Set(deepPath, "deep value");

            // Assert
            Assert.Equal("deep value", sj.Get(deepPath));
            Assert.True(sj.HasPath(deepPath));
        }

        [Fact]
        public void Performance_ManyProperties_HandlesCorrectly()
        {
            // Arrange
            var sj = new StructuredJson();

            // Act - Add 1000 properties
            for (int i = 0; i < 1000; i++)
            {
                sj.Set($"prop{i}", $"value{i}");
            }

            // Assert
            Assert.Equal("value0", sj.Get("prop0"));
            Assert.Equal("value999", sj.Get("prop999"));
            Assert.Equal(1000, sj.ListPaths().Count);
        }

        [Fact]
        public void Performance_LargeArrayOperations_HandlesCorrectly()
        {
            // Arrange
            var sj = new StructuredJson();

            // Act - Add 100 array elements
            for (int i = 0; i < 100; i++)
            {
                sj.Set($"items[{i}]", $"item{i}");
            }

            // Assert
            Assert.Equal("item0", sj.Get("items[0]"));
            Assert.Equal("item99", sj.Get("items[99]"));
            
            var paths = sj.ListPaths();
            var arrayPaths = paths.Keys.Where(k => k.StartsWith("items[")).ToList();
            Assert.Equal(100, arrayPaths.Count);
        }

        #endregion

        #region JSON Serialization Edge Cases

        [Fact]
        public void JsonSerialization_SpecialCharactersInJson_HandlesCorrectly()
        {
            // Arrange
            var jsonWithSpecialChars = """
            {
                "message": "Hello \"World\"",
                "path": "C:\\Users\\John",
                "newline": "Line1\nLine2",
                "tab": "Col1\tCol2",
                "unicode": "Café ñoño"
            }
            """;

            // Act
            var sj = new StructuredJson(jsonWithSpecialChars);

            // Assert
            Assert.Equal("Hello \"World\"", sj.Get("message"));
            Assert.Equal("C:\\Users\\John", sj.Get("path"));
            Assert.Equal("Line1\nLine2", sj.Get("newline"));
            Assert.Equal("Col1\tCol2", sj.Get("tab"));
            Assert.Equal("Café ñoño", sj.Get("unicode"));
        }

        [Fact]
        public void JsonSerialization_EmptyJsonObjects_HandlesCorrectly()
        {
            // Arrange & Act
            var sj1 = new StructuredJson("{}");
            var sj2 = new StructuredJson("{\"empty\": {}}");
            var sj3 = new StructuredJson("{\"emptyArray\": []}");

            // Assert
            Assert.Empty(sj1.ListPaths());
            Assert.True(sj2.HasPath("empty"));
            Assert.True(sj3.HasPath("emptyArray"));
        }

        [Fact]
        public void JsonSerialization_MalformedJson_ThrowsException()
        {
            // Arrange
            var malformedJsons = new[]
            {
                "{\"name\": \"John\", \"age\":}", // Missing value
                "{\"name\": \"John\" \"age\": 30}", // Missing comma
                "{\"name\": \"John\", \"age\": 30", // Missing closing brace
                "\"name\": \"John\", \"age\": 30}", // Missing opening brace
                "{\"name\": \"John\", \"age\": 30}}", // Extra closing brace
            };

            // Act & Assert
            foreach (var malformedJson in malformedJsons)
            {
                Assert.Throws<ArgumentException>(() => new StructuredJson(malformedJson));
            }
        }

        [Fact]
        public void JsonSerialization_RoundTripWithComplexData_MaintainsIntegrity()
        {
            // Arrange
            var sj = new StructuredJson();
            sj.Set("user:name", "John Doe");
            sj.Set("user:age", 30);
            sj.Set("user:isActive", true);
            sj.Set("user:balance", 1234.56m);
            sj.Set("user:tags[0]", "admin");
            sj.Set("user:tags[1]", "premium");
            sj.Set("user:metadata:created", DateTime.Now);
            sj.Set("user:metadata:lastLogin", null);

            // Act
            var json = sj.ToJson();
            var sj2 = new StructuredJson(json);

            // Assert
            Assert.Equal("John Doe", sj2.Get("user:name"));
            Assert.Equal(30, sj2.Get<int>("user:age"));
            Assert.True(sj2.Get<bool>("user:isActive"));
            Assert.Equal("admin", sj2.Get("user:tags[0]"));
            Assert.Equal("premium", sj2.Get("user:tags[1]"));
            Assert.True(sj2.HasPath("user:metadata:lastLogin"));
        }

        [Fact]
        public void JsonSerialization_UnquotedKeys_DemonstratesBehavior()
        {
            // Arrange - Test different JSON formats
            var standardJson = """{"name": "John", "age": 30}"""; // Standard JSON
            var unquotedJson = """{name: "John", age: 30}"""; // Unquoted keys
            var singleQuotedJson = """{'name': 'John', 'age': 30}"""; // Single quotes

            // Act & Assert
            
            // 1. Standard JSON should work
            var sj1 = new StructuredJson(standardJson);
            Assert.Equal("John", sj1.Get("name"));
            Assert.Equal(30, sj1.Get<int>("age"));

            // 2. Unquoted keys should fail
            var exception1 = Assert.Throws<ArgumentException>(() => new StructuredJson(unquotedJson));
            Assert.Contains("Invalid JSON", exception1.Message);

            // 3. Single quoted keys should fail
            var exception2 = Assert.Throws<ArgumentException>(() => new StructuredJson(singleQuotedJson));
            Assert.Contains("Invalid JSON", exception2.Message);
        }

        [Fact]
        public void JsonSerialization_MixedQuotedUnquotedKeys_HandlesCorrectly()
        {
            // Arrange - JSON with mixed quoted and unquoted keys
            var mixedJson = """
            {
                "name": "John",
                age: 30,
                "address": {
                    city: "Ankara",
                    "country": "Turkey"
                }
            }
            """;

            // Act & Assert
            var exception = Record.Exception(() => new StructuredJson(mixedJson));
            
            // System.Text.Json should reject this format
            Assert.NotNull(exception);
            Assert.IsType<ArgumentException>(exception);
        }

        [Fact]
        public void JsonSerialization_SingleQuotedKeys_HandlesCorrectly()
        {
            // Arrange - JSON with single-quoted keys
            var singleQuotedJson = """
            {
                'name': 'John',
                'age': 30,
                'address': {
                    'city': 'Ankara',
                    'country': 'Turkey'
                }
            }
            """;

            // Act & Assert
            var exception = Record.Exception(() => new StructuredJson(singleQuotedJson));
            
            // System.Text.Json should reject this format
            Assert.NotNull(exception);
            Assert.IsType<ArgumentException>(exception);
        }

        #endregion

        #region Type Conversion Edge Cases

        [Fact]
        public void TypeConversion_StringToNumber_HandlesCorrectly()
        {
            // Arrange
            var sj = new StructuredJson();
            sj.Set("stringNumber", "123");
            sj.Set("stringDecimal", "123.45");

            // Act & Assert
            Assert.Equal(123, sj.Get<int>("stringNumber"));
            Assert.Equal(123.45, sj.Get<double>("stringDecimal"), 2);
        }

        [Fact]
        public void TypeConversion_NumberToString_HandlesCorrectly()
        {
            // Arrange
            var sj = new StructuredJson();
            sj.Set("number", 123);
            sj.Set("decimal", 123.45);

            // Act & Assert
            Assert.Equal("123", sj.Get<string>("number"));
            Assert.Contains("123.45", sj.Get<string>("decimal"));
        }

        [Fact]
        public void TypeConversion_InvalidConversion_ReturnsDefault()
        {
            // Arrange
            var sj = new StructuredJson();
            sj.Set("text", "not a number");

            // Act & Assert
            Assert.Equal(0, sj.Get<int>("text")); // Should return default(int)
            Assert.Equal(0.0, sj.Get<double>("text")); // Should return default(double)
        }

        #endregion

        #region Boundary Conditions

        [Fact]
        public void BoundaryConditions_MaxPathLength_HandlesCorrectly()
        {
            // Arrange
            var sj = new StructuredJson();
            var longPath = string.Join(":", Enumerable.Range(0, 100).Select(i => $"segment{i}"));

            // Act
            sj.Set(longPath, "value");

            // Assert
            Assert.Equal("value", sj.Get(longPath));
        }

        [Fact]
        public void BoundaryConditions_EmptyAndWhitespaceKeys_HandlesCorrectly()
        {
            // Arrange
            var sj = new StructuredJson();

            // Act
            sj.Set("   ", "whitespace key");
            sj.Set("\t", "tab key");
            sj.Set("\n", "newline key");

            // Assert
            Assert.Equal("whitespace key", sj.Get("   "));
            Assert.Equal("tab key", sj.Get("\t"));
            Assert.Equal("newline key", sj.Get("\n"));
        }

        [Fact]
        public void BoundaryConditions_VeryLongStringValues_HandlesCorrectly()
        {
            // Arrange
            var sj = new StructuredJson();
            var longString = new string('A', 10000); // 10KB string

            // Act
            sj.Set("longString", longString);

            // Assert
            Assert.Equal(longString, sj.Get("longString"));
            Assert.Equal(10000, sj.Get<string>("longString")?.Length);
        }

        #endregion
    }
} 