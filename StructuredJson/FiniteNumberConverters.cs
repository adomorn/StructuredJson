using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace StructuredJson;

// Installed after user converters: default conversions reject overflow at every graph depth.
internal sealed class FiniteDoubleConverter : JsonConverter<double>
{
    public override double Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        double value;
        if (reader.TokenType == JsonTokenType.Number) value = reader.GetDouble();
        else if (reader.TokenType == JsonTokenType.String && options.NumberHandling.HasFlag(JsonNumberHandling.AllowReadingFromString) &&
                 double.TryParse(reader.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)) value = parsed;
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
    public override float Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        float value;
        if (reader.TokenType == JsonTokenType.Number) value = reader.GetSingle();
        else if (reader.TokenType == JsonTokenType.String && options.NumberHandling.HasFlag(JsonNumberHandling.AllowReadingFromString) &&
                 float.TryParse(reader.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)) value = parsed;
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
