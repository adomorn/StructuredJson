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

    internal StructuredJsonOptions Snapshot()
    {
        if (MaxDepth is < 1 or > 512) throw new ArgumentOutOfRangeException(nameof(MaxDepth), "Depth must be between 1 and 512.");
        if (MaxPathLength < 1) throw new ArgumentOutOfRangeException(nameof(MaxPathLength));
        if (MaxArrayLength < 1) throw new ArgumentOutOfRangeException(nameof(MaxArrayLength));
        if (MaxNodeCount < 1) throw new ArgumentOutOfRangeException(nameof(MaxNodeCount));
        ArgumentNullException.ThrowIfNull(NumberCulture);
        ArgumentNullException.ThrowIfNull(SerializerOptions);
        if (SerializerOptions.ReferenceHandler is not null) throw new ArgumentException("Reference preservation/ignoring cycles is not supported by the JSON tree.", nameof(SerializerOptions));
        var serializer = new JsonSerializerOptions(SerializerOptions) { MaxDepth = MaxDepth };
        NumberHandlingPolicy.Configure(serializer);
        return new StructuredJsonOptions
        {
            MaxDepth = MaxDepth,
            MaxPathLength = MaxPathLength,
            MaxArrayLength = MaxArrayLength,
            MaxNodeCount = MaxNodeCount,
            OverwriteOnTypeConflict = OverwriteOnTypeConflict,
            NumberCulture = CultureInfo.ReadOnly((CultureInfo)NumberCulture.Clone()),
            SerializerOptions = serializer
        };
    }
}
