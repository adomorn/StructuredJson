using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace StructuredJson
{
    /// <summary>
    /// A JSON manipulation library that provides path-based API for creating, reading, and updating JSON objects.
    /// Uses Dictionary&lt;string, object?&gt; as the underlying data structure and System.Text.Json for serialization.
    /// </summary>
    public class StructuredJson
    {
        private readonly Dictionary<string, object?> _data;

        /// <summary>
        /// Initializes a new instance of the StructuredJson class with an empty data structure.
        /// </summary>
        public StructuredJson()
        {
            _data = new Dictionary<string, object?>();
        }

        /// <summary>
        /// Initializes a new instance of the StructuredJson class from a JSON string.
        /// </summary>
        /// <param name="json">The JSON string to parse.</param>
        /// <exception cref="ArgumentException">Thrown when the JSON string is invalid.</exception>
        public StructuredJson(string json)
        {
            _data = new Dictionary<string, object?>();
            if (!string.IsNullOrWhiteSpace(json))
            {
                try
                {
                    var jsonElement = JsonSerializer.Deserialize<JsonElement>(json);
                    PopulateFromJsonElement(jsonElement, _data);
                }
                catch (JsonException ex)
                {
                    throw new ArgumentException("Invalid JSON string provided.", nameof(json), ex);
                }
            }
        }

        /// <summary>
        /// Sets a value at the specified path.
        /// Path syntax: use ':' for object properties and '[]' for arrays (e.g., "user:addresses[0]:city").
        /// </summary>
        /// <param name="path">The path where to set the value.</param>
        /// <param name="value">The value to set.</param>
        /// <exception cref="ArgumentException">Thrown when the path is null or empty.</exception>
        public void Set(string path, object? value)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("Path cannot be null or empty.", nameof(path));

            var pathSegments = ParsePath(path);
            SetValueAtPath(_data, pathSegments, value);
        }

        /// <summary>
        /// Gets a value from the specified path.
        /// </summary>
        /// <param name="path">The path to retrieve the value from.</param>
        /// <returns>The value at the specified path, or null if the path doesn't exist.</returns>
        /// <exception cref="ArgumentException">Thrown when the path is null or empty.</exception>
        public object? Get(string path)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("Path cannot be null or empty.", nameof(path));

            var pathSegments = ParsePath(path);
            return GetValueAtPath(_data, pathSegments);
        }

        /// <summary>
        /// Gets a strongly-typed value from the specified path.
        /// </summary>
        /// <typeparam name="T">The type to cast the value to.</typeparam>
        /// <param name="path">The path to retrieve the value from.</param>
        /// <returns>The value at the specified path cast to type T, or default(T) if the path doesn't exist or casting fails.</returns>
        public T? Get<T>(string path)
        {
            var value = Get(path);
            if (value == null)
                return default(T);

            try
            {
                if (value is JsonElement jsonElement)
                {
                    return JsonSerializer.Deserialize<T>(jsonElement.GetRawText());
                }
                
                if (value is T directValue)
                    return directValue;

                // Handle specific type conversions
                var targetType = typeof(T);
                var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;

                // String to number conversions
                if (value is string stringValue && IsNumericType(underlyingType))
                {
                    if (underlyingType == typeof(int) && int.TryParse(stringValue, out int intResult))
                        return (T)(object)intResult;
                    if (underlyingType == typeof(long) && long.TryParse(stringValue, out long longResult))
                        return (T)(object)longResult;
                    if (underlyingType == typeof(double) && double.TryParse(stringValue, out double doubleResult))
                        return (T)(object)doubleResult;
                    if (underlyingType == typeof(decimal) && decimal.TryParse(stringValue, out decimal decimalResult))
                        return (T)(object)decimalResult;
                    if (underlyingType == typeof(float) && float.TryParse(stringValue, out float floatResult))
                        return (T)(object)floatResult;
                }

                // Number to string conversions
                if (underlyingType == typeof(string) && IsNumericType(value.GetType()))
                {
                    return (T)(object)value.ToString()!;
                }

                // Try to convert using JsonSerializer for complex types
                var json = JsonSerializer.Serialize(value);
                return JsonSerializer.Deserialize<T>(json);
            }
            catch
            {
                return default(T);
            }
        }

        /// <summary>
        /// Converts the current data structure to a JSON string.
        /// </summary>
        /// <param name="options">JSON serializer options for formatting.</param>
        /// <returns>A JSON string representation of the data.</returns>
        public string ToJson(JsonSerializerOptions? options = null)
        {
            options ??= new JsonSerializerOptions { WriteIndented = true };
            return JsonSerializer.Serialize(_data, options);
        }

        /// <summary>
        /// Lists all paths and their corresponding values in the data structure.
        /// </summary>
        /// <returns>A dictionary containing all path-value pairs.</returns>
        public Dictionary<string, object?> ListPaths()
        {
            var result = new Dictionary<string, object?>();
            BuildPathList(_data, "", result);
            return result;
        }

        /// <summary>
        /// Checks if a path exists in the data structure.
        /// </summary>
        /// <param name="path">The path to check.</param>
        /// <returns>True if the path exists, false otherwise.</returns>
        public bool HasPath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return false;

            try
            {
                var pathSegments = ParsePath(path);
                return PathExists(_data, pathSegments);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Removes a value at the specified path.
        /// </summary>
        /// <param name="path">The path to remove.</param>
        /// <returns>True if the path was found and removed, false otherwise.</returns>
        public bool Remove(string path)
        {
            if (string.IsNullOrEmpty(path))
                return false;

            try
            {
                var pathSegments = ParsePath(path);
                return RemoveValueAtPath(_data, pathSegments);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Clears all data from the structure.
        /// </summary>
        public void Clear()
        {
            _data.Clear();
        }

        private List<PathSegment> ParsePath(string path)
        {
            var segments = new List<PathSegment>();
            var regex = new Regex(@"([^:\[\]]+)(\[([^\]]*)\])?");
            var parts = path.Split(':');

            foreach (var part in parts)
            {
                if (string.IsNullOrEmpty(part))
                    continue;

                var match = regex.Match(part);
                if (match.Success)
                {
                    var key = match.Groups[1].Value; // Don't trim to allow whitespace keys
                    if (string.IsNullOrEmpty(key))
                        throw new ArgumentException($"Invalid path segment: '{part}'", nameof(path));

                    if (match.Groups[3].Success)
                    {
                        var indexString = match.Groups[3].Value;
                        
                        // Check for empty array index
                        if (string.IsNullOrEmpty(indexString))
                            throw new ArgumentException($"Empty array index in path: '{part}'", nameof(path));
                        
                        // Check for invalid array index format
                        if (!int.TryParse(indexString, out int index))
                            throw new ArgumentException($"Invalid array index '{indexString}' in path: '{part}'", nameof(path));
                        
                        // Check for negative array index
                        if (index < 0)
                            throw new ArgumentException($"Negative array index '{index}' in path: '{part}'", nameof(path));
                        
                        // Check for multiple array indices (like [0][1])
                        if (part.Count(c => c == '[') > 1)
                            throw new ArgumentException($"Multiple array indices not supported in path: '{part}'", nameof(path));

                        segments.Add(new PathSegment { Key = key, IsArray = true, ArrayIndex = index });
                    }
                    else
                    {
                        segments.Add(new PathSegment { Key = key, IsArray = false });
                    }
                }
                else
                {
                    throw new ArgumentException($"Invalid path segment: '{part}'", nameof(path));
                }
            }

            if (segments.Count == 0)
                throw new ArgumentException("Path resulted in no valid segments", nameof(path));

            return segments;
        }

        private void SetValueAtPath(Dictionary<string, object?> data, List<PathSegment> pathSegments, object? value)
        {
            if (pathSegments.Count == 0)
                return;

            var segment = pathSegments[0];
            var isLastSegment = pathSegments.Count == 1;

            if (isLastSegment)
            {
                if (segment.IsArray)
                {
                    EnsureArrayExists(data, segment.Key, segment.ArrayIndex);
                    if (data[segment.Key] is List<object?> array)
                    {
                        array[segment.ArrayIndex] = value;
                    }
                }
                else
                {
                    data[segment.Key] = value;
                }
            }
            else
            {
                if (segment.IsArray)
                {
                    EnsureArrayExists(data, segment.Key, segment.ArrayIndex);
                    if (data[segment.Key] is List<object?> array)
                    {
                        if (array[segment.ArrayIndex] == null || !(array[segment.ArrayIndex] is Dictionary<string, object?>))
                        {
                            array[segment.ArrayIndex] = new Dictionary<string, object?>();
                        }
                        
                        if (array[segment.ArrayIndex] is Dictionary<string, object?> nestedDict)
                        {
                            SetValueAtPath(nestedDict, pathSegments.Skip(1).ToList(), value);
                        }
                    }
                }
                else
                {
                    if (!data.ContainsKey(segment.Key) || !(data[segment.Key] is Dictionary<string, object?>))
                    {
                        data[segment.Key] = new Dictionary<string, object?>();
                    }
                    
                    if (data[segment.Key] is Dictionary<string, object?> nestedDict)
                    {
                        SetValueAtPath(nestedDict, pathSegments.Skip(1).ToList(), value);
                    }
                }
            }
        }

        private object? GetValueAtPath(Dictionary<string, object?> data, List<PathSegment> pathSegments)
        {
            if (pathSegments.Count == 0)
                return null;

            var segment = pathSegments[0];
            var isLastSegment = pathSegments.Count == 1;

            if (!data.ContainsKey(segment.Key))
                return null;

            if (segment.IsArray)
            {
                if (!(data[segment.Key] is List<object?> array) || segment.ArrayIndex >= array.Count || segment.ArrayIndex < 0)
                    return null;

                var arrayValue = array[segment.ArrayIndex];
                
                if (isLastSegment)
                    return arrayValue;

                if (arrayValue is Dictionary<string, object?> nestedDict)
                    return GetValueAtPath(nestedDict, pathSegments.Skip(1).ToList());
                
                return null;
            }
            else
            {
                var value = data[segment.Key];
                
                if (isLastSegment)
                    return value;

                if (value is Dictionary<string, object?> nestedDict)
                    return GetValueAtPath(nestedDict, pathSegments.Skip(1).ToList());
                
                return null;
            }
        }

        private bool PathExists(Dictionary<string, object?> data, List<PathSegment> pathSegments)
        {
            if (pathSegments.Count == 0)
                return true;

            var segment = pathSegments[0];
            var isLastSegment = pathSegments.Count == 1;

            if (!data.ContainsKey(segment.Key))
                return false;

            if (segment.IsArray)
            {
                if (!(data[segment.Key] is List<object?> array) || segment.ArrayIndex >= array.Count || segment.ArrayIndex < 0)
                    return false;

                if (isLastSegment)
                    return true;

                var arrayValue = array[segment.ArrayIndex];
                if (arrayValue is Dictionary<string, object?> nestedDict)
                    return PathExists(nestedDict, pathSegments.Skip(1).ToList());
                
                return false;
            }
            else
            {
                if (isLastSegment)
                    return true;

                var value = data[segment.Key];
                if (value is Dictionary<string, object?> nestedDict)
                    return PathExists(nestedDict, pathSegments.Skip(1).ToList());
                
                return false;
            }
        }

        private bool RemoveValueAtPath(Dictionary<string, object?> data, List<PathSegment> pathSegments)
        {
            if (pathSegments.Count == 0)
                return false;

            var segment = pathSegments[0];
            var isLastSegment = pathSegments.Count == 1;

            if (!data.ContainsKey(segment.Key))
                return false;

            if (isLastSegment)
            {
                if (segment.IsArray)
                {
                    if (data[segment.Key] is List<object?> array && segment.ArrayIndex < array.Count && segment.ArrayIndex >= 0)
                    {
                        array.RemoveAt(segment.ArrayIndex);
                        return true;
                    }
                    return false;
                }
                else
                {
                    return data.Remove(segment.Key);
                }
            }
            else
            {
                if (segment.IsArray)
                {
                    if (!(data[segment.Key] is List<object?> array) || segment.ArrayIndex >= array.Count || segment.ArrayIndex < 0)
                        return false;

                    var arrayValue = array[segment.ArrayIndex];
                    if (arrayValue is Dictionary<string, object?> nestedDict)
                        return RemoveValueAtPath(nestedDict, pathSegments.Skip(1).ToList());
                    
                    return false;
                }
                else
                {
                    var value = data[segment.Key];
                    if (value is Dictionary<string, object?> nestedDict)
                        return RemoveValueAtPath(nestedDict, pathSegments.Skip(1).ToList());
                    
                    return false;
                }
            }
        }

        private void EnsureArrayExists(Dictionary<string, object?> data, string key, int index)
        {
            if (!data.ContainsKey(key) || !(data[key] is List<object?>))
            {
                data[key] = new List<object?>();
            }

            if (data[key] is List<object?> array)
            {
                while (array.Count <= index)
                {
                    array.Add(null);
                }
            }
        }

        private void BuildPathList(Dictionary<string, object?> data, string currentPath, Dictionary<string, object?> result)
        {
            foreach (var kvp in data)
            {
                var newPath = string.IsNullOrEmpty(currentPath) ? kvp.Key : $"{currentPath}:{kvp.Key}";

                if (kvp.Value is Dictionary<string, object?> nestedDict)
                {
                    BuildPathList(nestedDict, newPath, result);
                }
                else if (kvp.Value is List<object?> array)
                {
                    for (int i = 0; i < array.Count; i++)
                    {
                        var arrayPath = $"{newPath}[{i}]";
                        if (array[i] is Dictionary<string, object?> arrayNestedDict)
                        {
                            BuildPathList(arrayNestedDict, arrayPath, result);
                        }
                        else if (array[i] != null) // Only include non-null values for sparse arrays
                        {
                            result[arrayPath] = array[i];
                        }
                    }
                }
                else
                {
                    result[newPath] = kvp.Value; // Include all values, including null for regular properties
                }
            }
        }

        private void PopulateFromJsonElement(JsonElement element, Dictionary<string, object?> target)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    foreach (var property in element.EnumerateObject())
                    {
                        var convertedValue = ConvertJsonElement(property.Value);
                        target[property.Name] = convertedValue;
                    }
                    break;
                case JsonValueKind.Array:
                    var list = new List<object?>();
                    foreach (var item in element.EnumerateArray())
                    {
                        list.Add(ConvertJsonElement(item));
                    }
                    // This case shouldn't happen for root level, but handle it gracefully
                    break;
            }
        }

        private object? ConvertJsonElement(JsonElement element)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    var dict = new Dictionary<string, object?>();
                    foreach (var property in element.EnumerateObject())
                    {
                        var convertedValue = ConvertJsonElement(property.Value);
                        dict[property.Name] = convertedValue;
                    }
                    return dict;
                case JsonValueKind.Array:
                    var list = new List<object?>();
                    foreach (var item in element.EnumerateArray())
                    {
                        list.Add(ConvertJsonElement(item));
                    }
                    return list;
                case JsonValueKind.String:
                    return element.GetString();
                case JsonValueKind.Number:
                    if (element.TryGetInt32(out int intValue))
                        return intValue;
                    if (element.TryGetInt64(out long longValue))
                        return longValue;
                    if (element.TryGetDouble(out double doubleValue))
                        return doubleValue;
                    return element.GetDecimal();
                case JsonValueKind.True:
                    return true;
                case JsonValueKind.False:
                    return false;
                case JsonValueKind.Null:
                    return null;
                default:
                    return element.GetRawText();
            }
        }

        private static bool IsNumericType(Type type)
        {
            return type == typeof(int) || type == typeof(long) || type == typeof(double) || 
                   type == typeof(decimal) || type == typeof(float) || type == typeof(short) || 
                   type == typeof(byte) || type == typeof(uint) || type == typeof(ulong) || 
                   type == typeof(ushort) || type == typeof(sbyte);
        }

        private class PathSegment
        {
            public string Key { get; set; } = string.Empty;
            public bool IsArray { get; set; }
            public int ArrayIndex { get; set; }
        }
    }
} 