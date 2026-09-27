using System.Buffers;
using System.Globalization;
using System.Text;

namespace StructuredJson;

internal readonly record struct PathToken(string? Key, int Index)
{
    internal bool IsIndex => Key is null;
    internal static PathToken Property(string key) => new(key, -1);
    internal static PathToken Array(int index) => new(null, index);
}

internal static class PathParser
{
    internal static List<PathToken> Parse(string path, StructuredJsonOptions options)
    {
        if (string.IsNullOrEmpty(path) || path.Length > options.MaxPathLength)
            throw new ArgumentException("Path must be nonempty and within MaxPathLength.", nameof(path));
        // JSON replaces unpaired UTF-16 surrogates, which would alias distinct
        // keys on serialization. Reject them before any lookup or mutation.
        ReadOnlySpan<char> remaining = path;
        while (!remaining.IsEmpty)
        {
            if (Rune.DecodeFromUtf16(remaining, out _, out int consumed) != OperationStatus.Done) throw Invalid(path);
            remaining = remaining[consumed..];
        }
        var tokens = new List<PathToken>();
        int position = 0;
        while (position < path.Length)
        {
            var key = new StringBuilder();
            bool empty = false;
            while (position < path.Length && path[position] != ':' && path[position] != '[')
            {
                char c = path[position++];
                if (c == ']') throw Invalid(path);
                if (c == '\\')
                {
                    if (position == path.Length) throw Invalid(path);
                    c = path[position++];
                    if (c == 'e' && key.Length == 0 && (position == path.Length || path[position] is ':' or '['))
                    { empty = true; break; }
                    if (c is not (':' or '[' or ']' or '\\')) throw Invalid(path);
                }
                key.Append(c);
            }
            if (key.Length == 0 && !empty) throw Invalid(path);
            tokens.Add(PathToken.Property(key.ToString()));
            while (position < path.Length && path[position] == '[')
            {
                position++;
                int start = position;
                while (position < path.Length && path[position] is >= '0' and <= '9') position++;
                if (position == start || position == path.Length || path[position] != ']' ||
                    !int.TryParse(path.AsSpan(start, position - start), NumberStyles.None, CultureInfo.InvariantCulture, out int index) ||
                    index >= options.MaxArrayLength) throw Invalid(path);
                tokens.Add(PathToken.Array(index));
                position++;
            }
            if (tokens.Count > options.MaxDepth) throw Invalid(path);
            if (position == path.Length) break;
            if (path[position++] != ':' || position == path.Length) throw Invalid(path);
        }
        return tokens;
    }

    internal static string Escape(string key) => key.Length == 0 ? @"\e" : key.Replace("\\", "\\\\").Replace(":", @"\:").Replace("[", @"\[").Replace("]", @"\]");
    private static ArgumentException Invalid(string path) => new("Invalid path syntax or path exceeds configured limits.", nameof(path));
}
