using System.Globalization;
using System.Text.Json;

namespace StructuredJson;

/// <summary>A bounded, lossless JSON object editor with escaped property paths and array indices.</summary>
/// <remarks>Operations are synchronized. Reads return detached snapshots. Multi-call sequences are not atomic.</remarks>
public class StructuredJson
{
    private readonly object _gate = new();
    private readonly StructuredJsonOptions _options;
    private readonly JsonSerializerOptions _outputOptions;
    private readonly Dictionary<string, object?> _data;
    private int _nodeCount = 1;

    /// <summary>Creates an empty JSON object.</summary>
    public StructuredJson() : this(new StructuredJsonOptions()) { }

    /// <summary>Creates an empty JSON object using a snapshot of the supplied settings.</summary>
    public StructuredJson(StructuredJsonOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options.Snapshot();
        _outputOptions = new JsonSerializerOptions(_options.SerializerOptions) { WriteIndented = true };
        _data = new(StringComparer.Ordinal);
    }

    /// <summary>Parses a JSON object. Non-object roots, duplicate properties and invalid/empty input are rejected.</summary>
    public StructuredJson(string json) : this(json, new StructuredJsonOptions()) { }

    /// <summary>Parses a bounded JSON object with the supplied settings.</summary>
    public StructuredJson(string json, StructuredJsonOptions options) : this(options)
    {
        if (string.IsNullOrWhiteSpace(json)) throw new ArgumentException("A JSON object is required.", nameof(json));
        try
        {
            using var document = JsonDocument.Parse(json, new JsonDocumentOptions
            {
                MaxDepth = _options.MaxDepth,
                AllowTrailingCommas = _options.SerializerOptions.AllowTrailingCommas,
                CommentHandling = _options.SerializerOptions.ReadCommentHandling
            });
            if (document.RootElement.ValueKind != JsonValueKind.Object) throw new ArgumentException("The JSON root must be an object.", nameof(json));
            var normalized = (Dictionary<string, object?>)ValueTree.Normalize(document.RootElement, _options)!;
            ValueTree.ValidatePaths(normalized, "", _options.MaxPathLength);
            _data = normalized;
            _nodeCount = ValueTree.Count(_data);
        }
        catch (JsonException e) { throw new ArgumentException("Invalid JSON string provided.", nameof(json), e); }
    }

    /// <summary>Sets a value atomically. CLR values are copied into the JSON model; incompatible containers throw unless explicitly enabled.</summary>
    public void Set(string path, object? value)
    {
        var tokens = PathParser.Parse(path, _options);
        object? normalized;
        try { normalized = ValueTree.Normalize(value, _options); }
        catch (Exception e) when (e is JsonException or NotSupportedException or ObjectDisposedException)
        { throw new ArgumentException("Value cannot be represented as bounded JSON.", nameof(value), e); }
        if (tokens.Count + ValueTree.Height(normalized) > _options.MaxDepth) throw new ArgumentException("Value exceeds MaxDepth.", nameof(path));
        ValueTree.ValidatePaths(normalized, Format(tokens), _options.MaxPathLength);
        lock (_gate)
        {
            object container = _data;
            for (int i = 0; i < tokens.Count; i++)
            {
                var token = tokens[i];
                bool exists = TryChild(container, token, out var old);
                if (i == tokens.Count - 1)
                { Assign(container, token, old, exists, normalized); return; }
                bool needsArray = tokens[i + 1].IsIndex;
                if (old is not null && (needsArray ? old is List<object?> : old is Dictionary<string, object?>))
                { container = old; continue; }
                if (old is not null && !_options.OverwriteOnTypeConflict)
                    throw new InvalidOperationException("An intermediate value has an incompatible type. Enable OverwriteOnTypeConflict to replace it.");
                // Build only the missing/conflicting suffix. Attach once all limits have passed.
                long count = ValueTree.Count(normalized);
                object? branch = normalized;
                for (int j = tokens.Count - 1; j > i; j--)
                {
                    var next = tokens[j];
                    count += next.IsIndex ? (long)next.Index + 1 : 1;
                    if (count > _options.MaxNodeCount) throw new ArgumentException("Value exceeds MaxNodeCount.", nameof(path));
                    if (next.IsIndex)
                    {
                        var list = new List<object?>(next.Index + 1);
                        for (int k = 0; k <= next.Index; k++) list.Add(null);
                        list[next.Index] = branch; branch = list;
                    }
                    else branch = new Dictionary<string, object?>(StringComparer.Ordinal) { [next.Key!] = branch };
                }
                Assign(container, token, old, exists, branch);
                return;
            }
        }
    }

    /// <summary>Gets a detached value, or null when absent. Non-integral and large numbers are lossless JsonElement tokens.</summary>
    public object? Get(string path)
    {
        var tokens = PathParser.Parse(path, _options);
        lock (_gate) return Resolve(tokens, out var value) ? ValueTree.Export(value) : null;
    }

    /// <summary>Gets a typed value or default when missing, null for a nonnullable type, or not convertible.</summary>
    public T? Get<T>(string path) => TryGet<T>(path, out var value) ? value : default;

    /// <summary>Distinguishes missing or invalid conversion from valid default values. Invalid path syntax throws.</summary>
    public bool TryGet<T>(string path, out T? value)
    {
        var tokens = PathParser.Parse(path, _options);
        lock (_gate)
        {
            value = default;
            return Resolve(tokens, out var node) && ValueTree.TryConvert(node, _options, out value);
        }
    }

    /// <summary>Gets a required typed value, throwing KeyNotFoundException or InvalidCastException instead of silently defaulting.</summary>
    public T? GetRequired<T>(string path)
    {
        var tokens = PathParser.Parse(path, _options);
        lock (_gate)
        {
            if (!Resolve(tokens, out var node)) throw new KeyNotFoundException("The requested path does not exist.");
            if (!ValueTree.TryConvert<T>(node, _options, out var result)) throw new InvalidCastException("The value cannot be converted to the requested type.");
            return result;
        }
    }

    /// <summary>Tests presence, including null values. Invalid paths return false.</summary>
    public bool HasPath(string path)
    {
        List<PathToken> tokens;
        try { tokens = PathParser.Parse(path, _options); }
        catch (ArgumentException) { return false; }
        lock (_gate) return Resolve(tokens, out _);
    }

    /// <summary>Removes a property or array element, shifting subsequent indices. Invalid or absent paths return false.</summary>
    public bool Remove(string path)
    {
        List<PathToken> tokens;
        try { tokens = PathParser.Parse(path, _options); }
        catch (ArgumentException) { return false; }
        lock (_gate)
        {
            object? parent = _data;
            for (int i = 0; i < tokens.Count - 1; i++)
                if (!TryChild(parent, tokens[i], out parent)) return false;
            var last = tokens[^1];
            if (!TryChild(parent, last, out var old)) return false;
            if (last.IsIndex) ((List<object?>)parent!).RemoveAt(last.Index);
            else ((Dictionary<string, object?>)parent!).Remove(last.Key!);
            _nodeCount -= ValueTree.Count(old);
            return true;
        }
    }

    /// <summary>Returns escaped leaf paths, including nulls and empty containers. The empty root has no path.</summary>
    public Dictionary<string, object?> ListPaths()
    {
        lock (_gate)
        {
            var result = new Dictionary<string, object?>(StringComparer.Ordinal);
            foreach (var pair in _data) Visit(pair.Value, PathParser.Escape(pair.Key), result);
            return result;
        }
    }

    /// <summary>Serializes with lossless number tokens. The configured structural depth remains enforced.</summary>
    public string ToJson(JsonSerializerOptions? options = null)
    {
        var settings = options is null ? _outputOptions : new JsonSerializerOptions(options) { MaxDepth = _options.MaxDepth };
        lock (_gate) return ValueTree.Serialize(_data, settings);
    }

    /// <summary>Removes all properties.</summary>
    public void Clear() { lock (_gate) { _data.Clear(); _nodeCount = 1; } }

    private void Assign(object container, PathToken token, object? old, bool exists, object? value)
    {
        int gaps = token.IsIndex ? Math.Max(0, token.Index - ((List<object?>)container).Count) : 0;
        long size = (long)_nodeCount - (exists ? ValueTree.Count(old) : 0) + ValueTree.Count(value) + gaps;
        if (size > _options.MaxNodeCount) throw new ArgumentException("Operation exceeds MaxNodeCount.");
        if (token.IsIndex)
        {
            var list = (List<object?>)container;
            // Allocate before changing the existing list so allocation failure cannot partially append gaps.
            list.EnsureCapacity(token.Index + 1);
            while (list.Count <= token.Index) list.Add(null);
            list[token.Index] = value;
        }
        else ((Dictionary<string, object?>)container)[token.Key!] = value;
        _nodeCount = (int)size;
    }

    private bool Resolve(List<PathToken> tokens, out object? value)
    {
        value = _data;
        foreach (var token in tokens) if (!TryChild(value, token, out value)) return false;
        return true;
    }

    private static bool TryChild(object? container, PathToken token, out object? value)
    {
        value = null;
        if (!token.IsIndex) return container is Dictionary<string, object?> map && map.TryGetValue(token.Key!, out value);
        if (container is not List<object?> list || token.Index >= list.Count) return false;
        value = list[token.Index]; return true;
    }

    private static string Format(IEnumerable<PathToken> tokens)
    {
        string path = "";
        foreach (var token in tokens) path += token.IsIndex ? "[" + token.Index.ToString(CultureInfo.InvariantCulture) + "]" : (path.Length == 0 ? "" : ":") + PathParser.Escape(token.Key!);
        return path;
    }

    private static void Visit(object? node, string path, Dictionary<string, object?> result)
    {
        if (node is Dictionary<string, object?> map && map.Count > 0)
            foreach (var pair in map) Visit(pair.Value, path + ":" + PathParser.Escape(pair.Key), result);
        else if (node is List<object?> list && list.Count > 0)
            for (int i = 0; i < list.Count; i++) Visit(list[i], path + "[" + i.ToString(CultureInfo.InvariantCulture) + "]", result);
        else result.Add(path, ValueTree.Export(node));
    }
}
