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
        ValidateUnicode(path);
        var tokens = new List<PathToken>();
        int position = 0;
        while (position < path.Length)
        {
            tokens.Add(PathToken.Property(ReadProperty(path, ref position)));
            while (position < path.Length && path[position] == '[')
                tokens.Add(PathToken.Array(ReadIndex(path, ref position, options.MaxArrayLength)));
            if (tokens.Count > options.MaxDepth) throw Invalid(nameof(path));
            if (position == path.Length) break;
            if (path[position++] != ':' || position == path.Length) throw Invalid(nameof(path));
        }
        return tokens;
    }

    private static void ValidateUnicode(string path)
    {
        // JSON replaces unpaired UTF-16 surrogates, which would alias distinct
        // keys on serialization. Reject them before any lookup or mutation.
        ReadOnlySpan<char> remaining = path;
        while (!remaining.IsEmpty)
        {
            if (Rune.DecodeFromUtf16(remaining, out _, out int consumed) != OperationStatus.Done) throw Invalid(nameof(path));
            remaining = remaining[consumed..];
        }
    }

    private static string ReadProperty(string path, ref int position)
    {
        var key = new StringBuilder();
        bool empty = false;
        while (position < path.Length && path[position] != ':' && path[position] != '[')
        {
            char c = path[position++];
            if (c == ']') throw Invalid(nameof(path));
            if (c == '\\')
            {
                c = ReadEscape(path, ref position, key.Length, out empty);
                if (empty) break;
            }
            key.Append(c);
        }
        if (key.Length == 0 && !empty) throw Invalid(nameof(path));
        return key.ToString();
    }

    private static char ReadEscape(string path, ref int position, int keyLength, out bool empty)
    {
        if (position == path.Length) throw Invalid(nameof(path));
        char c = path[position++];
        empty = c == 'e' && keyLength == 0 && (position == path.Length || path[position] is ':' or '[');
        if (!empty && c is not (':' or '[' or ']' or '\\')) throw Invalid(nameof(path));
        return c;
    }

    private static int ReadIndex(string path, ref int position, int maxArrayLength)
    {
        position++;
        int start = position;
        while (position < path.Length && path[position] is >= '0' and <= '9') position++;
        if (position == start || position == path.Length || path[position] != ']' ||
            !int.TryParse(path.AsSpan(start, position - start), NumberStyles.None, CultureInfo.InvariantCulture, out int index) ||
            index >= maxArrayLength) throw Invalid(nameof(path));
        position++;
        return index;
    }

    internal static string Escape(string key) => key.Length == 0 ? @"\e" : key.Replace("\\", "\\\\").Replace(":", @"\:").Replace("[", @"\[").Replace("]", @"\]");
    private static ArgumentException Invalid(string parameterName) => new("Invalid path syntax or path exceeds configured limits.", parameterName);
}
