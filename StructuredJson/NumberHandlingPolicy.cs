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
        options.TypeInfoResolver = (options.TypeInfoResolver ?? new DefaultJsonTypeInfoResolver())
            .WithAddedModifier(info => ConfigureType(info, options.NumberHandling));
        options.Converters.Add(new CollectionNumberHandlingFactory());
        options.Converters.Add(new FiniteDoubleConverter());
        options.Converters.Add(new FiniteSingleConverter());
        options.Converters.Add(new FiniteHalfConverter());
    }

    private static void ConfigureType(JsonTypeInfo info, JsonNumberHandling globalHandling)
    {
        if (info.Converter is IScopedNumberHandlingConverter)
            info.NumberHandling = JsonNumberHandling.Strict;
        else if (info.Kind is JsonTypeInfoKind.Enumerable or JsonTypeInfoKind.Dictionary &&
            info.Options.Converters.OfType<CollectionNumberHandlingFactory>().Any(factory => factory.ExcludedType == info.Type))
            info.NumberHandling = info.Options.NumberHandling;
        if (info.Kind != JsonTypeInfoKind.Object) return;
        foreach (var property in info.Properties) ConfigureProperty(info, property, globalHandling);
    }

    private static void ConfigureProperty(JsonTypeInfo info, JsonPropertyInfo property, JsonNumberHandling globalHandling)
    {
        // Extension data uses STJ's dictionary population contract and holds
        // raw JSON values, so it must not become a normal property converter.
        if (property.IsExtensionData) return;
        // STJ removes both accessors from ignored members. Their creation and
        // number policies must not affect the containing object's contract.
        if (property.Get is null && property.Set is null) return;
        var underlying = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
        if (!SupportsScopedHandling(underlying) || property.CustomConverter is not null || HasExplicitConverter(info.Options, property.PropertyType)) return;
        // A member override applies to that value, not unrelated members of
        // nested POCOs. Restore the caller's global policy at object contracts.
        var handling = property.NumberHandling ?? info.NumberHandling ??
            (info.Options.NumberHandling != globalHandling ? globalHandling : (JsonNumberHandling?)null);
        ValidateCreationHandling(info, property, underlying, handling);
        if (handling is null) return;
        // STJ does not pass member-level NumberHandling into custom converters.
        // Re-enter this property's declared contract with scoped global settings.
        property.CustomConverter = (JsonConverter)Activator.CreateInstance(
            typeof(ScopedNumberHandlingConverter<>).MakeGenericType(property.PropertyType), handling.Value)!;
        // The scoped converter owns these flags now. Strict also prevents
        // inheriting type-level flags that STJ rejects on custom collections.
        property.NumberHandling = JsonNumberHandling.Strict;
    }

    private static bool SupportsScopedHandling(Type type)
    {
        bool numeric = type == typeof(Half) || (!type.IsEnum && Type.GetTypeCode(type) is >= TypeCode.SByte and <= TypeCode.Decimal);
        return numeric || type == typeof(object) || (type != typeof(string) && typeof(IEnumerable).IsAssignableFrom(type));
    }

    private static void ValidateCreationHandling(JsonTypeInfo info, JsonPropertyInfo property, Type underlying, JsonNumberHandling? handling)
    {
        var creation = property.ObjectCreationHandling ?? info.PreferredPropertyObjectCreationHandling ?? info.Options.PreferredObjectCreationHandling;
        bool scopedType = info.Options.Converters.OfType<CollectionNumberHandlingFactory>().Any(factory => factory.CanConvert(property.PropertyType));
        if (creation == JsonObjectCreationHandling.Populate && typeof(IEnumerable).IsAssignableFrom(underlying) && (handling is not null || scopedType))
            throw new NotSupportedException("Populate cannot be combined with scoped number handling on a collection. Use Replace with a writable property.");
    }

    internal static bool HasExplicitConverter(JsonSerializerOptions options, Type type)
    {
        var underlying = Nullable.GetUnderlyingType(type) ?? type;
        return options.Converters.Any(converter => converter is not FiniteDoubleConverter and not FiniteSingleConverter and not FiniteHalfConverter and not CollectionNumberHandlingFactory &&
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
