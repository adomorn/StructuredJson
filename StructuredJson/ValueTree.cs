using System.Globalization;
using System.Text.Json;
using System.Text;

namespace StructuredJson;

internal static class ValueTree
{
    internal static object? Normalize(object? value, StructuredJsonOptions options)
    {
        if (value is null) return null;
        var element = value is JsonElement json ? json.Clone() : JsonSerializer.SerializeToElement(value, options.SerializerOptions);
        int count = 0;
        return Import(element, options, 0, ref count);
    }

    private static object? Import(JsonElement element, StructuredJsonOptions options, int depth, ref int count)
    {
        if (++count > options.MaxNodeCount) throw new ArgumentException("Value exceeds MaxNodeCount.");
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                if (++depth > options.MaxDepth) throw new ArgumentException("Value exceeds MaxDepth.");
                var dictionary = new Dictionary<string, object?>(StringComparer.Ordinal);
                foreach (var property in element.EnumerateObject())
                    if (!dictionary.TryAdd(property.Name, Import(property.Value, options, depth, ref count)))
                        throw new ArgumentException("Duplicate JSON property names are not supported.");
                return dictionary;
            case JsonValueKind.Array:
                if (++depth > options.MaxDepth) throw new ArgumentException("Value exceeds MaxDepth.");
                if (element.GetArrayLength() > options.MaxArrayLength) throw new ArgumentException("Array exceeds MaxArrayLength.");
                var list = new List<object?>(element.GetArrayLength());
                foreach (var item in element.EnumerateArray()) list.Add(Import(item, options, depth, ref count));
                return list;
            case JsonValueKind.String: return element.GetString();
            case JsonValueKind.True: return true;
            case JsonValueKind.False: return false;
            case JsonValueKind.Null: return null;
            case JsonValueKind.Number: return element.Clone();
            default: throw new ArgumentException("Undefined JSON values are not supported.");
        }
    }

    internal static int Count(object? node) => node switch
    {
        Dictionary<string, object?> map => 1 + map.Values.Sum(Count),
        List<object?> list => 1 + list.Sum(Count),
        _ => 1
    };

    internal static int Height(object? node) => node switch
    {
        Dictionary<string, object?> map => 1 + (map.Count == 0 ? 0 : map.Values.Max(Height)),
        List<object?> list => 1 + (list.Count == 0 ? 0 : list.Max(Height)),
        _ => 0
    };

    internal static object? Export(object? node) => node switch
    {
        Dictionary<string, object?> map => map.ToDictionary(x => x.Key, x => Export(x.Value), StringComparer.Ordinal),
        List<object?> list => list.Select(Export).ToList(),
        JsonElement number when number.TryGetInt32(out var n) => n,
        JsonElement number when number.TryGetInt64(out var n) => n,
        // Non-integral/large numbers stay tokens so untyped reads cannot silently lose precision.
        JsonElement number => number.Clone(),
        _ => node
    };

    internal static void ValidatePaths(object? value, string prefix, int maxLength)
    {
        if (prefix.Length > maxLength) throw new ArgumentException("Value contains a path exceeding MaxPathLength.");
        if (value is Dictionary<string, object?> map)
            foreach (var pair in map)
                ValidatePaths(pair.Value, prefix.Length == 0 ? PathParser.Escape(pair.Key) : prefix + ":" + PathParser.Escape(pair.Key), maxLength);
        else if (value is List<object?> list)
            for (int i = 0; i < list.Count; i++) ValidatePaths(list[i], prefix + "[" + i.ToString(CultureInfo.InvariantCulture) + "]", maxLength);
    }

    internal static bool TryConvert<T>(object? value, StructuredJsonOptions options, out T? result)
    {
        result = default;
        if (value is null) return default(T) is null;
        try
        {
            bool explicitConverter = NumberHandlingPolicy.HasExplicitConverter(options.SerializerOptions, typeof(T));
            if (typeof(T) == typeof(object) && !explicitConverter) { result = (T?)Export(value); return true; }
            if (typeof(T) == typeof(string) && value is JsonElement numeric && !explicitConverter)
            { result = (T)(object)numeric.GetRawText(); return true; }
            var target = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);
            if (value is string text && IsNumeric(target) && !explicitConverter)
            {
                var culture = options.NumberCulture;
                object number = Type.GetTypeCode(target) switch
                {
                    TypeCode.SByte => sbyte.Parse(text, NumberStyles.Integer, culture),
                    TypeCode.Byte => byte.Parse(text, NumberStyles.Integer, culture),
                    TypeCode.Int16 => short.Parse(text, NumberStyles.Integer, culture),
                    TypeCode.UInt16 => ushort.Parse(text, NumberStyles.Integer, culture),
                    TypeCode.Int32 => int.Parse(text, NumberStyles.Integer, culture),
                    TypeCode.UInt32 => uint.Parse(text, NumberStyles.Integer, culture),
                    TypeCode.Int64 => long.Parse(text, NumberStyles.Integer, culture),
                    TypeCode.UInt64 => ulong.Parse(text, NumberStyles.Integer, culture),
                    TypeCode.Single => float.Parse(text, NumberStyles.Float, culture),
                    TypeCode.Double => double.Parse(text, NumberStyles.Float, culture),
                    TypeCode.Decimal => decimal.Parse(text, NumberStyles.Float, culture),
                    _ => throw new InvalidCastException()
                };
                if (number is double d && !double.IsFinite(d) || number is float f && !float.IsFinite(f)) return false;
                result = (T)number;
                return true;
            }
            result = value is JsonElement element
                ? element.Deserialize<T>(options.SerializerOptions)
                : JsonSerializer.Deserialize<T>(Serialize(value, options.SerializerOptions), options.SerializerOptions);
            if (result is double nonfinite && !double.IsFinite(nonfinite) || result is float nonfiniteFloat && !float.IsFinite(nonfiniteFloat))
            { result = default; return false; }
            return true;
        }
        catch (Exception e) when (e is JsonException or NotSupportedException or FormatException or OverflowException or InvalidCastException)
        { result = default; return false; }
    }

    internal static string Serialize(object? value, JsonSerializerOptions options)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = options.WriteIndented, Encoder = options.Encoder }))
            Write(writer, value);
        return Encoding.UTF8.GetString(stream.ToArray());
    }

    private static void Write(Utf8JsonWriter writer, object? value)
    {
        switch (value)
        {
            case Dictionary<string, object?> map:
                writer.WriteStartObject();
                foreach (var pair in map) { writer.WritePropertyName(pair.Key); Write(writer, pair.Value); }
                writer.WriteEndObject(); break;
            case List<object?> list:
                writer.WriteStartArray(); foreach (var item in list) Write(writer, item); writer.WriteEndArray(); break;
            case JsonElement number: number.WriteTo(writer); break;
            case string text: writer.WriteStringValue(text); break;
            case bool boolean: writer.WriteBooleanValue(boolean); break;
            case null: writer.WriteNullValue(); break;
            default: throw new InvalidOperationException("Invalid internal JSON value.");
        }
    }

    private static bool IsNumeric(Type type) => !type.IsEnum && Type.GetTypeCode(type) is
        TypeCode.SByte or TypeCode.Byte or TypeCode.Int16 or TypeCode.UInt16 or TypeCode.Int32 or TypeCode.UInt32 or
        TypeCode.Int64 or TypeCode.UInt64 or TypeCode.Single or TypeCode.Double or TypeCode.Decimal;
}
