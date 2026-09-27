using System.Collections;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace StructuredJson;

internal static class NumberHandlingPolicy
{
    internal static void Configure(JsonSerializerOptions options)
    {
        options.TypeInfoResolver = (options.TypeInfoResolver ?? new DefaultJsonTypeInfoResolver()).WithAddedModifier(info =>
        {
            if (info.Converter is IScopedNumberHandlingConverter)
                info.NumberHandling = JsonNumberHandling.Strict;
            else if (info.Kind is JsonTypeInfoKind.Enumerable or JsonTypeInfoKind.Dictionary &&
                info.Options.Converters.OfType<CollectionNumberHandlingFactory>().Any(factory => factory.ExcludedType == info.Type))
                info.NumberHandling = info.Options.NumberHandling;
            if (info.Kind != JsonTypeInfoKind.Object) return;
            foreach (var property in info.Properties)
            {
                var handling = property.NumberHandling ?? info.NumberHandling;
                var underlying = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
                bool relevant = underlying == typeof(double) || underlying == typeof(float) ||
                    (underlying != typeof(string) && typeof(IEnumerable).IsAssignableFrom(underlying));
                if (handling is null || !relevant || property.CustomConverter is not null || HasExplicitConverter(info.Options, property.PropertyType)) continue;
                // STJ does not pass member-level NumberHandling into custom converters.
                // Re-enter this property's declared contract with scoped global settings.
                property.CustomConverter = (JsonConverter)Activator.CreateInstance(
                    typeof(ScopedNumberHandlingConverter<>).MakeGenericType(property.PropertyType), handling.Value)!;
                // The scoped converter owns these flags now. Strict also prevents
                // inheriting type-level flags that STJ rejects on custom collections.
                property.NumberHandling = JsonNumberHandling.Strict;
            }
        });
        options.Converters.Add(new CollectionNumberHandlingFactory());
        options.Converters.Add(new FiniteDoubleConverter());
        options.Converters.Add(new FiniteSingleConverter());
    }

    internal static bool HasExplicitConverter(JsonSerializerOptions options, Type type)
    {
        var underlying = Nullable.GetUnderlyingType(type) ?? type;
        return options.Converters.Any(converter => converter is not FiniteDoubleConverter and not FiniteSingleConverter and not CollectionNumberHandlingFactory &&
            (converter.CanConvert(type) || converter.CanConvert(underlying)));
    }

    private interface IScopedNumberHandlingConverter { }

    private sealed class ScopedNumberHandlingConverter<T>(JsonNumberHandling handling) : JsonConverter<T>, IScopedNumberHandlingConverter
    {
        public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            JsonSerializer.Deserialize<T>(ref reader, Scope(options));

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options) =>
            JsonSerializer.Serialize(writer, value, Scope(options));

        private JsonSerializerOptions Scope(JsonSerializerOptions options)
        {
            var scoped = new JsonSerializerOptions(options) { NumberHandling = handling };
            for (int i = 0; i < scoped.Converters.Count; i++)
                if (scoped.Converters[i] is CollectionNumberHandlingFactory)
                    // Delegate this contract to STJ without recursively selecting ourselves.
                    // Only this type is excluded: differently annotated nested types retain their policy.
                    scoped.Converters[i] = new CollectionNumberHandlingFactory(typeof(T));
            return scoped;
        }
    }

    private sealed class CollectionNumberHandlingFactory(Type? excludedType = null) : JsonConverterFactory
    {
        internal Type? ExcludedType => excludedType;
        public override bool CanConvert(Type typeToConvert) => typeToConvert != excludedType &&
            typeof(IEnumerable).IsAssignableFrom(typeToConvert) &&
            typeToConvert.GetCustomAttribute<JsonNumberHandlingAttribute>() is not null &&
            typeToConvert.GetCustomAttribute<JsonConverterAttribute>() is null;

        public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options) =>
            (JsonConverter)Activator.CreateInstance(typeof(ScopedNumberHandlingConverter<>).MakeGenericType(typeToConvert),
                typeToConvert.GetCustomAttribute<JsonNumberHandlingAttribute>()!.Handling)!;
    }
}
