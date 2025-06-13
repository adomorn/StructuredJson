#if NET8_0_OR_GREATER
// Global usings are enabled for .NET 8+
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
#else
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
#endif

#if NET8_0_OR_GREATER
#nullable enable
#endif

namespace StructuredJson
{
    /// <summary>
    /// A JSON manipulation library that provides path-based API for creating, reading, and updating JSON objects.
    /// Uses Dictionary&lt;string, object?&gt; as the underlying data structure and System.Text.Json for serialization.
    /// </summary>
    public class StructuredJson
    {
#if NET8_0_OR_GREATER
        private readonly Dictionary<string, object?> _data;
#else
        private readonly Dictionary<string, object> _data;
#endif

        /// <summary>
        /// Initializes a new instance of the StructuredJson class with an empty data structure.
        /// </summary>
        public StructuredJson()
        {
#if NET8_0_OR_GREATER
            _data = new Dictionary<string, object?>();
#else
            _data = new Dictionary<string, object>();
#endif
        }

        /// <summary>
        /// Initializes a new instance of the StructuredJson class from a JSON string.
        /// </summary>
        /// <param name="json">The JSON string to parse.</param>
        /// <exception cref="ArgumentException">Thrown when the JSON string is invalid.</exception>
        public StructuredJson(string json)
        {
#if NET8_0_OR_GREATER
            _data = new Dictionary<string, object?>();
#else
            _data = new Dictionary<string, object>();
#endif
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
#if NET8_0_OR_GREATER
        public void Set(string path, object? value)
#else
        public void Set(string path, object value)
#endif
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
#if NET8_0_OR_GREATER
        public object? Get(string path)
#else
        public object Get(string path)
#endif
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
#if NET8_0_OR_GREATER
        public T? Get<T>(string path)
#else
        public T Get<T>(string path)
#endif
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
#if NET8_0_OR_GREATER
                    return (T)(object)value.ToString()!;
#else
                    return (T)(object)value.ToString();
#endif
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
#if NET8_0_OR_GREATER
        public string ToJson(JsonSerializerOptions? options = null)
#else
        public string ToJson(JsonSerializerOptions options = null)
#endif
        {
            options = options ?? new JsonSerializerOptions { WriteIndented = true };
            return JsonSerializer.Serialize(_data, options);
        }

        /// <summary>
        /// Lists all paths and their corresponding values in the data structure.
        /// </summary>
        /// <returns>A dictionary containing all path-value pairs.</returns>
#if NET8_0_OR_GREATER
        public Dictionary<string, object?> ListPaths()
        {
            var result = new Dictionary<string, object?>();
#else
        public Dictionary<string, object> ListPaths()
        {
            var result = new Dictionary<string, object>();
#endif
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
                            throw new ArgumentException($"Invalid array index '{indexString}' in path: '{part}'",
                                nameof(path));

                        // Check for negative array index
                        if (index < 0)
                            throw new ArgumentException($"Negative array index '{index}' in path: '{part}'",
                                nameof(path));

                        // Check for multiple array indices (like [0][1])
                        if (part.Count(c => c == '[') > 1)
                            throw new ArgumentException($"Multiple array indices not supported in path: '{part}'",
                                nameof(path));

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

#if NET8_0_OR_GREATER
        private void SetValueAtPath(Dictionary<string, object?> data, List<PathSegment> pathSegments, object? value)
#else
        private void SetValueAtPath(Dictionary<string, object> data, List<PathSegment> pathSegments, object value)
#endif
        {
            for (int i = 0; i < pathSegments.Count; i++)
            {
                var segment = pathSegments[i];
                var isLastSegment = i == pathSegments.Count - 1;

                if (segment.IsArray)
                {
                    EnsureArrayExists(data, segment.Key, segment.ArrayIndex);
                    var list = (List<object>)data[segment.Key]!;

                    if (isLastSegment)
                    {
                        list[segment.ArrayIndex] = value!;
                    }
                    else
                    {
                        if (list[segment.ArrayIndex] == null)
                        {
#if NET8_0_OR_GREATER
                            list[segment.ArrayIndex] = new Dictionary<string, object?>();
#else
                            list[segment.ArrayIndex] = new Dictionary<string, object>();
#endif
                        }

#if NET8_0_OR_GREATER
                        if (!(list[segment.ArrayIndex] is Dictionary<string, object?> nextData))
                        {
                            list[segment.ArrayIndex] = new Dictionary<string, object?>();
                            nextData = (Dictionary<string, object?>)list[segment.ArrayIndex]!;
                        }
                        data = nextData;
#else
                        if (!(list[segment.ArrayIndex] is Dictionary<string, object> nextData))
                        {
                            list[segment.ArrayIndex] = new Dictionary<string, object>();
                            nextData = (Dictionary<string, object>)list[segment.ArrayIndex]!;
                        }
                        data = nextData;
#endif
                    }
                }
                else
                {
                    if (isLastSegment)
                    {
                        data[segment.Key] = value;
                    }
                    else
                    {
                        if (!data.ContainsKey(segment.Key) || data[segment.Key] == null)
                        {
#if NET8_0_OR_GREATER
                            data[segment.Key] = new Dictionary<string, object?>();
#else
                            data[segment.Key] = new Dictionary<string, object>();
#endif
                        }

#if NET8_0_OR_GREATER
                        if (!(data[segment.Key] is Dictionary<string, object?> nextData))
                        {
                            data[segment.Key] = new Dictionary<string, object?>();
                            nextData = (Dictionary<string, object?>)data[segment.Key]!;
                        }
                        data = nextData;
#else
                        if (!(data[segment.Key] is Dictionary<string, object> nextData))
                        {
                            data[segment.Key] = new Dictionary<string, object>();
                            nextData = (Dictionary<string, object>)data[segment.Key]!;
                        }
                        data = nextData;
#endif
                    }
                }
            }
        }

#if NET8_0_OR_GREATER
        private object? GetValueAtPath(Dictionary<string, object?> data, List<PathSegment> pathSegments)
#else
        private object GetValueAtPath(Dictionary<string, object> data, List<PathSegment> pathSegments)
#endif
        {
            foreach (var segment in pathSegments)
            {
                if (!data.ContainsKey(segment.Key))
                    return null;

                var value = data[segment.Key];
                if (value == null)
                    return null;

                if (segment.IsArray)
                {
                    if (!(value is List<object> list))
                        return null;

                    if (segment.ArrayIndex >= list.Count)
                        return null;

                    value = list[segment.ArrayIndex];
                    if (value == null)
                        return null;
                }

                if (segment != pathSegments.Last())
                {
#if NET8_0_OR_GREATER
                    if (!(value is Dictionary<string, object?> nextData))
#else
                    if (!(value is Dictionary<string, object> nextData))
#endif
                        return null;
                    data = nextData;
                }
                else
                {
                    return value;
                }
            }

            return null;
        }

#if NET8_0_OR_GREATER
        private bool PathExists(Dictionary<string, object?> data, List<PathSegment> pathSegments)
#else
        private bool PathExists(Dictionary<string, object> data, List<PathSegment> pathSegments)
#endif
        {
            foreach (var segment in pathSegments)
            {
                if (!data.ContainsKey(segment.Key))
                    return false;

                var value = data[segment.Key];

                if (segment.IsArray)
                {
                    if (!(value is List<object> list))
                        return false;

                    if (segment.ArrayIndex >= list.Count)
                        return false;

                    // For arrays, we need to check if we're at the last segment
                    if (segment == pathSegments.Last())
                        return true; // Path exists even if value is null
                    
                    value = list[segment.ArrayIndex];
                    if (value == null)
                        return false; // Can't navigate further if intermediate value is null
                }

                if (segment != pathSegments.Last())
                {
                    // For intermediate segments, null means we can't navigate further
                    if (value == null)
                        return false;
                        
#if NET8_0_OR_GREATER
                    if (!(value is Dictionary<string, object?> nextData))
#else
                    if (!(value is Dictionary<string, object> nextData))
#endif
                        return false;
                    data = nextData;
                }
            }

            return true;
        }

#if NET8_0_OR_GREATER
        private bool RemoveValueAtPath(Dictionary<string, object?> data, List<PathSegment> pathSegments)
#else
        private bool RemoveValueAtPath(Dictionary<string, object> data, List<PathSegment> pathSegments)
#endif
        {
            if (pathSegments.Count == 0)
                return false;

            var lastSegment = pathSegments.Last();
            var parentSegments = pathSegments.Take(pathSegments.Count - 1).ToList();

            if (parentSegments.Count > 0)
            {
                var parentValue = GetValueAtPath(_data, parentSegments);
                if (parentValue == null)
                    return false;

#if NET8_0_OR_GREATER
                if (!(parentValue is Dictionary<string, object?> parentData))
#else
                if (!(parentValue is Dictionary<string, object> parentData))
#endif
                    return false;

                data = parentData;
            }

            if (lastSegment.IsArray)
            {
                if (!data.ContainsKey(lastSegment.Key))
                    return false;

                var value = data[lastSegment.Key];
                if (!(value is List<object> list))
                    return false;

                if (lastSegment.ArrayIndex >= list.Count)
                    return false;

                list.RemoveAt(lastSegment.ArrayIndex);
                return true;
            }
            else
            {
                return data.Remove(lastSegment.Key);
            }
        }

#if NET8_0_OR_GREATER
        private void EnsureArrayExists(Dictionary<string, object?> data, string key, int index)
#else
        private void EnsureArrayExists(Dictionary<string, object> data, string key, int index)
#endif
        {
            if (!data.ContainsKey(key) || !(data[key] is List<object>))
            {
                data[key] = new List<object>();
            }

            var list = (List<object>)data[key]!;
            while (list.Count <= index)
            {
                list.Add(null!);
            }
        }

#if NET8_0_OR_GREATER
        private void BuildPathList(Dictionary<string, object?> data, string currentPath,
            Dictionary<string, object?> result)
#else
        private void BuildPathList(Dictionary<string, object> data, string currentPath,
            Dictionary<string, object> result)
#endif
        {
            foreach (var kvp in data)
            {
                var path = string.IsNullOrEmpty(currentPath) ? kvp.Key : $"{currentPath}:{kvp.Key}";

                if (kvp.Value is List<object> list)
                {
                    for (int i = 0; i < list.Count; i++)
                    {
                        var arrayPath = $"{path}[{i}]";
                        var item = list[i];

                        if (item != null)
                        {
#if NET8_0_OR_GREATER
                            if (item is Dictionary<string, object?> nestedDict)
#else
                            if (item is Dictionary<string, object> nestedDict)
#endif
                            {
                                BuildPathList(nestedDict, arrayPath, result);
                            }
                            else
                            {
                                result[arrayPath] = item;
                            }
                        }
                    }
                }
#if NET8_0_OR_GREATER
                else if (kvp.Value is Dictionary<string, object?> nestedData)
#else
                else if (kvp.Value is Dictionary<string, object> nestedData)
#endif
                {
                    BuildPathList(nestedData, path, result);
                }
                else
                {
                    result[path] = kvp.Value;
                }
            }
        }

#if NET8_0_OR_GREATER
        private void PopulateFromJsonElement(JsonElement element, Dictionary<string, object?> target)
#else
        private void PopulateFromJsonElement(JsonElement element, Dictionary<string, object> target)
#endif
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    foreach (var property in element.EnumerateObject())
                    {
                        target[property.Name] = ConvertJsonElement(property.Value);
                    }
                    break;
            }
        }

#if NET8_0_OR_GREATER
        private object? ConvertJsonElement(JsonElement element)
#else
        private object ConvertJsonElement(JsonElement element)
#endif
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
#if NET8_0_OR_GREATER
                    var obj = new Dictionary<string, object?>();
#else
                    var obj = new Dictionary<string, object>();
#endif
                    foreach (var property in element.EnumerateObject())
                    {
                        obj[property.Name] = ConvertJsonElement(property.Value);
                    }
                    return obj;

                case JsonValueKind.Array:
                    var array = new List<object>();
                    foreach (var item in element.EnumerateArray())
                    {
                        array.Add(ConvertJsonElement(item)!);
                    }
                    return array;

                case JsonValueKind.String:
                    return element.GetString();
                case JsonValueKind.Number:
                    if (element.TryGetInt32(out int intValue))
                        return intValue;
                    if (element.TryGetInt64(out long longValue))
                        return longValue;
                    return element.GetDouble();
                case JsonValueKind.True:
                    return true;
                case JsonValueKind.False:
                    return false;
                case JsonValueKind.Null:
                    return null;
                default:
                    return element.Clone();
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

#if NET8_0_OR_GREATER
#nullable restore
#endif