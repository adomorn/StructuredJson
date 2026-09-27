using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace StructuredJson;

internal static class NativeNumberReading
{
    internal static JsonSerializerOptions Strings { get; } = new() { NumberHandling = JsonNumberHandling.AllowReadingFromString };
}

// Installed after user converters: default conversions reject overflow at every graph depth.
internal sealed class FiniteDoubleConverter : JsonConverter<double>
{
    public override double ReadAsPropertyName(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var native = (JsonConverter<double>)JsonSerializerOptions.Default.GetConverter(typeof(double));
        double value = native.ReadAsPropertyName(ref reader, typeToConvert, options);
        return double.IsFinite(value) ? value : throw new JsonException("Dictionary key is outside the finite double range.");
    }

    public override double Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        double value;
        if (reader.TokenType == JsonTokenType.Number) value = reader.GetDouble();
        else if (reader.TokenType == JsonTokenType.String && options.NumberHandling.HasFlag(JsonNumberHandling.AllowReadingFromString))
            value = JsonSerializer.Deserialize<double>(ref reader, NativeNumberReading.Strings);
        else throw new JsonException("A finite JSON number is required.");
        return double.IsFinite(value) ? value : throw new JsonException("Number is outside the finite double range.");
    }
    public override void Write(Utf8JsonWriter writer, double value, JsonSerializerOptions options)
    {
        if (!double.IsFinite(value)) throw new JsonException("A finite number is required.");
        if (options.NumberHandling.HasFlag(JsonNumberHandling.WriteAsString)) writer.WriteStringValue(value.ToString("R", CultureInfo.InvariantCulture));
        else writer.WriteNumberValue(value);
    }
}

internal sealed class FiniteSingleConverter : JsonConverter<float>
{
    public override float ReadAsPropertyName(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var native = (JsonConverter<float>)JsonSerializerOptions.Default.GetConverter(typeof(float));
        float value = native.ReadAsPropertyName(ref reader, typeToConvert, options);
        return float.IsFinite(value) ? value : throw new JsonException("Dictionary key is outside the finite float range.");
    }

    public override float Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        float value;
        if (reader.TokenType == JsonTokenType.Number) value = reader.GetSingle();
        else if (reader.TokenType == JsonTokenType.String && options.NumberHandling.HasFlag(JsonNumberHandling.AllowReadingFromString))
            value = JsonSerializer.Deserialize<float>(ref reader, NativeNumberReading.Strings);
        else throw new JsonException("A finite JSON number is required.");
        return float.IsFinite(value) ? value : throw new JsonException("Number is outside the finite float range.");
    }
    public override void Write(Utf8JsonWriter writer, float value, JsonSerializerOptions options)
    {
        if (!float.IsFinite(value)) throw new JsonException("A finite number is required.");
        if (options.NumberHandling.HasFlag(JsonNumberHandling.WriteAsString)) writer.WriteStringValue(value.ToString("R", CultureInfo.InvariantCulture));
        else writer.WriteNumberValue(value);
    }
}

internal sealed class FiniteHalfConverter : JsonConverter<Half>
{
    public override Half ReadAsPropertyName(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var native = (JsonConverter<Half>)JsonSerializerOptions.Default.GetConverter(typeof(Half));
        Half value = native.ReadAsPropertyName(ref reader, typeToConvert, options);
        return Half.IsFinite(value) ? value : throw new JsonException("Dictionary key is outside the finite Half range.");
    }

    public override Half Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var nativeOptions = options.NumberHandling.HasFlag(JsonNumberHandling.AllowReadingFromString)
            ? NativeNumberReading.Strings : JsonSerializerOptions.Default;
        Half value = JsonSerializer.Deserialize<Half>(ref reader, nativeOptions);
        return Half.IsFinite(value) ? value : throw new JsonException("Number is outside the finite Half range.");
    }

    public override void Write(Utf8JsonWriter writer, Half value, JsonSerializerOptions options)
    {
        if (!Half.IsFinite(value)) throw new JsonException("A finite number is required.");
        if (options.NumberHandling.HasFlag(JsonNumberHandling.WriteAsString)) writer.WriteStringValue(value.ToString("R", CultureInfo.InvariantCulture));
        else writer.WriteNumberValue((float)value);
    }
}
