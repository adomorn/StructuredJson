using System.Globalization;
using System.Text.Json;

namespace StructuredJson;

/// <summary>Limits and conversion settings, copied when a structure is created.</summary>
public sealed class StructuredJsonOptions
{
    /// <summary>Maximum nesting of containers, including the root object.</summary>
    public int MaxDepth { get; init; } = 128;
    /// <summary>Maximum number of characters in a path.</summary>
    public int MaxPathLength { get; init; } = 4096;
    /// <summary>Maximum length of any array, including null-filled gaps.</summary>
    public int MaxArrayLength { get; init; } = 100_000;
    /// <summary>Maximum total values and containers, including null-filled gaps and the root.</summary>
    public int MaxNodeCount { get; init; } = 1_000_000;
    /// <summary>Allow replacing an incompatible intermediate scalar or container. Defaults to false.</summary>
    public bool OverwriteOnTypeConflict { get; init; }
    /// <summary>Culture for numeric string conversions. Thousands separators are not accepted.</summary>
    public CultureInfo NumberCulture { get; init; } = CultureInfo.InvariantCulture;
    /// <summary>Serializer settings for CLR inputs and typed reads. Structural limits take precedence.</summary>
    public JsonSerializerOptions SerializerOptions { get; init; } = new();

    internal JsonSerializerOptions InputSerializerOptions { get; private init; } = new();

    internal static StructuredJsonOptions Snapshot(StructuredJsonOptions options)
    {
        if (options.MaxDepth is < 1 or > 512) throw new ArgumentOutOfRangeException(nameof(options), "Depth must be between 1 and 512.");
        if (options.MaxPathLength < 1) throw new ArgumentOutOfRangeException(nameof(options), "MaxPathLength must be positive.");
        if (options.MaxArrayLength < 1) throw new ArgumentOutOfRangeException(nameof(options), "MaxArrayLength must be positive.");
        if (options.MaxNodeCount < 1) throw new ArgumentOutOfRangeException(nameof(options), "MaxNodeCount must be positive.");
        ArgumentNullException.ThrowIfNull(options.NumberCulture);
        ArgumentNullException.ThrowIfNull(options.SerializerOptions);
        if (options.SerializerOptions.ReferenceHandler is not null) throw new ArgumentException("Reference preservation/ignoring cycles is not supported by the JSON tree.", nameof(options));
        var serializer = new JsonSerializerOptions(options.SerializerOptions) { MaxDepth = options.MaxDepth };
        // Input uses STJ's native contract behavior. Finite converters are needed
        // only for typed reads, where STJ otherwise accepts overflow as infinity.
        var inputSerializer = new JsonSerializerOptions(serializer);
        NumberHandlingPolicy.Configure(serializer);
        return new StructuredJsonOptions
        {
            MaxDepth = options.MaxDepth,
            MaxPathLength = options.MaxPathLength,
            MaxArrayLength = options.MaxArrayLength,
            MaxNodeCount = options.MaxNodeCount,
            OverwriteOnTypeConflict = options.OverwriteOnTypeConflict,
            NumberCulture = CultureInfo.ReadOnly((CultureInfo)options.NumberCulture.Clone()),
            SerializerOptions = serializer,
            InputSerializerOptions = inputSerializer
        };
    }
}
