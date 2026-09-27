using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Xunit;

namespace StructuredJson.Tests;

public class AdversarialTests
{
    [Theory]
    [InlineData(7)]
    [InlineData(81)]
    [InlineData(20260927)]
    public void GeneratedArrayOperationsMatchIndependentListModel(int seed)
    {
        var random = new Random(seed);
        var expected = new List<int?>();
        var actual = new StructuredJson();
        actual.Set("items", Array.Empty<int>());
        for (int step = 0; step < 500; step++)
        {
            int index = random.Next(30);
            if (random.Next(3) == 0)
            {
                bool exists = index < expected.Count;
                Assert.Equal(exists, actual.Remove($"items[{index}]"));
                if (exists) expected.RemoveAt(index);
            }
            else
            {
                int? value = random.Next(4) == 0 ? null : random.Next(-1000, 1000);
                while (expected.Count <= index) expected.Add(null);
                expected[index] = value;
                actual.Set($"items[{index}]", value);
            }
            using var document = JsonDocument.Parse(actual.ToJson());
            var array = document.RootElement.GetProperty("items");
            Assert.Equal(expected.Count, array.GetArrayLength());
            for (int i = 0; i < expected.Count; i++)
            {
                Assert.True(actual.HasPath($"items[{i}]"));
                Assert.Equal(expected[i], actual.Get<int?>($"items[{i}]"));
                if (expected[i] is int n) Assert.Equal(n, array[i].GetInt32());
                else Assert.Equal(JsonValueKind.Null, array[i].ValueKind);
            }
        }
    }

    [Theory]
    [InlineData(@"a\q")]
    [InlineData(@"a\")]
    [InlineData(@"\efoo")]
    [InlineData("a[+1]")]
    [InlineData("a[ 1]")]
    [InlineData("a[١]")]
    [InlineData("a[1]:[2]")]
    [InlineData("a[1]x")]
    [InlineData("a[1]]")]
    [InlineData("a[2147483648]")]
    public void MalformedPathsAreRejectedByAllEntryPoints(string path)
    {
        var sj = new StructuredJson("{\"a\":[1,2]}"); var before = sj.ToJson();
        Assert.Throws<ArgumentException>(() => sj.Set(path, 9));
        Assert.Throws<ArgumentException>(() => sj.Get(path));
        Assert.False(sj.HasPath(path)); Assert.False(sj.Remove(path));
        Assert.Equal(before, sj.ToJson());
    }

    [Fact]
    public void EmptyContainersCountTowardDepthAndNodeLimits()
    {
        var sj = new StructuredJson(new StructuredJsonOptions { MaxDepth = 2, MaxNodeCount = 4 });
        sj.Set("a", new Dictionary<string, object?>());
        Assert.Throws<ArgumentException>(() => sj.Set("a:b", Array.Empty<int>()));
        sj.Set("a:b", 1); sj.Set("a:c", 2);
        Assert.Throws<ArgumentException>(() => sj.Set("a:d", 3));
        Assert.False(sj.HasPath("a:d"));
    }

    [Fact]
    public void IncompatibleValuesAndOversizedUpdatesDoNotConsumeBudget()
    {
        var sj = new StructuredJson("{\"a\":1}", new() { MaxNodeCount = 3 });
        Assert.Throws<InvalidOperationException>(() => sj.Set("a:b", 2));
        Assert.Throws<ArgumentException>(() => sj.Set("b[2]", 2));
        sj.Set("c", 2); Assert.Equal(2, sj.GetRequired<int>("c"));
        sj.Remove("a"); sj.Set("d", 3); Assert.Equal(3, sj.GetRequired<int>("d"));
    }

    [Fact]
    public void JsonSerializerReferencePoliciesAreRejected()
    {
        Assert.Throws<ArgumentException>(() => new StructuredJson(new StructuredJsonOptions
        { SerializerOptions = new JsonSerializerOptions { ReferenceHandler = ReferenceHandler.IgnoreCycles } }));
    }

    [Fact]
    public void CustomConverterRunsOnClrBoundariesButNotStoredJson()
    {
        var settings = new JsonSerializerOptions(); settings.Converters.Add(new MarkerConverter());
        var sj = new StructuredJson(new StructuredJsonOptions { SerializerOptions = settings });
        sj.Set("marker", new Marker(7));
        Assert.Equal("v7", sj.Get<string>("marker"));
        Assert.Equal(7, sj.GetRequired<Marker>("marker")!.Value);
        using var doc = JsonDocument.Parse(sj.ToJson()); Assert.Equal("v7", doc.RootElement.GetProperty("marker").GetString());
    }

    [Fact]
    public void SetRejectsNonFiniteClrNumbersAtomically()
    {
        var sj = new StructuredJson(); sj.Set("x", 1);
        Assert.Throws<ArgumentException>(() => sj.Set("x", double.NaN));
        Assert.Throws<ArgumentException>(() => sj.Set("x", double.PositiveInfinity));
        Assert.Equal(1, sj.Get<int>("x"));
    }

    [Fact]
    public void NonFiniteConversionsFailInsideContainersAndPocos()
    {
        var sj = new StructuredJson("{\"x\":{\"Value\":1e400},\"a\":[1e400]}");
        Assert.False(sj.TryGet<Dictionary<string, double>>("x", out _));
        Assert.False(sj.TryGet<double[]>("a", out _));
        Assert.False(sj.TryGet<FloatHolder>("x", out _));
    }

    [Fact]
    public void NumericConvertersTakePrecedenceOverBuiltInStringParsing()
    {
        var settings = new JsonSerializerOptions(); settings.Converters.Add(new IntPrefixConverter());
        var sj = new StructuredJson(new StructuredJsonOptions { SerializerOptions = settings });
        sj.Set("n", 7);
        Assert.Equal(7, sj.GetRequired<int>("n"));
        Assert.Equal(7, sj.GetRequired<int?>("n"));
    }

    public sealed class FloatHolder { public double Value { get; set; } }
    private sealed class IntPrefixConverter : JsonConverter<int>
    {
        public override int Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => int.Parse(reader.GetString()![1..], System.Globalization.CultureInfo.InvariantCulture);
        public override void Write(Utf8JsonWriter writer, int value, JsonSerializerOptions options) => writer.WriteStringValue("v" + value);
    }

    [Fact]
    public void PropertyNumberHandlingRemainsEffective()
    {
        var sj = new StructuredJson();
        sj.Set("x", new NumberHandlingHolder { Amount = 1.25, Optional = 2.5f });
        using var output = JsonDocument.Parse(sj.ToJson());
        Assert.Equal(JsonValueKind.String, output.RootElement.GetProperty("x").GetProperty("Amount").ValueKind);
        var read = new StructuredJson("{\"x\":{\"Amount\":\"1.25\",\"Optional\":\"2.5\"}}");
        Assert.Equal(1.25, read.GetRequired<NumberHandlingHolder>("x")!.Amount);
        Assert.Equal(2.5f, read.GetRequired<NumberHandlingHolder>("x")!.Optional);
    }

    [Fact]
    public void ExplicitStringConverterPrecedesNumericConvenienceConversion()
    {
        var settings = new JsonSerializerOptions(); settings.Converters.Add(new NumberStringConverter());
        var sj = new StructuredJson("{\"n\":42}", new() { SerializerOptions = settings });
        Assert.Equal("number:42", sj.GetRequired<string>("n"));
    }

    public sealed class NumberHandlingHolder
    {
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
        public double Amount { get; set; }
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
        public float? Optional { get; set; }
    }
    private sealed class NumberStringConverter : JsonConverter<string>
    {
        public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => "number:" + reader.GetInt32();
        public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options) => writer.WriteStringValue(value);
    }

    [Fact]
    public void CollectionAndTypeNumberHandlingRemainEffective()
    {
        var sj = new StructuredJson();
        sj.Set("x", new NumberHandlingCollections { Values = new[] { 1.25 }, Map = new() { ["n"] = 2.5f } });
        using var output = JsonDocument.Parse(sj.ToJson());
        Assert.Equal("1.25", output.RootElement.GetProperty("x").GetProperty("Values")[0].GetString());
        Assert.Equal("2.5", output.RootElement.GetProperty("x").GetProperty("Map").GetProperty("n").GetString());
        var restored = sj.GetRequired<NumberHandlingCollections>("x")!;
        Assert.Equal(1.25, restored.Values[0]);
        Assert.Equal(2.5f, restored.Map["n"]);
        var overflow = new StructuredJson("{\"x\":{\"Values\":[\"1e400\"]}}");
        Assert.False(overflow.TryGet<NumberHandlingCollections>("x", out _));
    }

    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
    public sealed class NumberHandlingCollections
    {
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
        public double[] Values { get; set; } = [];
        public Dictionary<string, float?> Map { get; set; } = new();
    }

    [Fact]
    public void AnnotatedCollectionTypesPreserveNumberHandling()
    {
        var sj = new StructuredJson();
        sj.Set("list", new StringNumbers { 1.25 });
        sj.Set("map", new StringNumberMap { ["n"] = 2.5 });
        using var output = JsonDocument.Parse(sj.ToJson());
        Assert.Equal("1.25", output.RootElement.GetProperty("list")[0].GetString());
        Assert.Equal("2.5", output.RootElement.GetProperty("map").GetProperty("n").GetString());
        Assert.Equal(1.25, sj.GetRequired<StringNumbers>("list")![0]);
        Assert.Equal(2.5, sj.GetRequired<StringNumberMap>("map")!["n"]);
        var overflow = new StructuredJson("{\"list\":[\"1e400\"],\"map\":{\"n\":\"1e400\"}}");
        Assert.False(overflow.TryGet<StringNumbers>("list", out _));
        Assert.False(overflow.TryGet<StringNumberMap>("map", out _));
    }

    [Fact]
    public void MemberHandlingOverridesCollectionTypeAndNestedTypesKeepTheirOwnPolicy()
    {
        var sj = new StructuredJson();
        sj.Set("holder", new StrictCollectionHolder { Values = new() { 1.25 } });
        sj.Set("nested", new StringNumberObjects { new StrictNumberObjects { new StringNumbers { 2.5 } } });
        using var output = JsonDocument.Parse(sj.ToJson());
        Assert.Equal(1.25, output.RootElement.GetProperty("holder").GetProperty("Values")[0].GetDouble());
        Assert.Equal("2.5", output.RootElement.GetProperty("nested")[0][0][0].GetString());
        Assert.Equal(1.25, sj.GetRequired<StrictCollectionHolder>("holder")!.Values[0]);
        var invalid = new StructuredJson("{\"holder\":{\"Values\":[\"1.25\"]}}");
        Assert.False(invalid.TryGet<StrictCollectionHolder>("holder", out _));
    }

    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
    public sealed class StringNumbers : List<double> { }
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
    public sealed class StringNumberMap : Dictionary<string, double> { }
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
    public sealed class StringNumberObjects : List<object> { }
    [JsonNumberHandling(JsonNumberHandling.Strict)]
    public sealed class StrictNumberObjects : List<object> { }
    public sealed class StrictCollectionHolder
    {
        [JsonNumberHandling(JsonNumberHandling.Strict)]
        public StringNumbers Values { get; set; } = new();
    }

    [Fact]
    public void BoxedNumberHandlingMatchesSystemTextJson()
    {
        object[] values = [1.25, 2.5f, 3, 4m, new FloatHolder { Value = 1.25 }, new PlainNumbers(),
            new[] { 1.25 }, new List<FloatHolder> { new() { Value = 1.25 } }, new[] { new[] { 1.25 } },
            new List<object> { 1.25, new PlainNumbers() }, new Dictionary<string, object> { ["n"] = new PlainNumbers() }];
        foreach (var value in values)
        {
            var holder = new BoxedNumberHolder { Value = value };
            var sj = new StructuredJson();
            sj.Set("x", holder);
            Assert.Equal(JsonSerializer.Serialize(holder), sj.GetRequired<JsonElement>("x").GetRawText());
        }
        var settings = new JsonSerializerOptions { NumberHandling = JsonNumberHandling.WriteAsString };
        var strict = new StrictBoxedNumberHolder { Value = 1.25 };
        var configured = new StructuredJson(new StructuredJsonOptions { SerializerOptions = settings });
        configured.Set("x", strict);
        Assert.Equal(JsonSerializer.Serialize(strict, settings), configured.GetRequired<JsonElement>("x").GetRawText());
    }

    public sealed class BoxedNumberHolder
    {
        [JsonNumberHandling(JsonNumberHandling.WriteAsString | JsonNumberHandling.AllowReadingFromString)]
        public object Value { get; set; } = null!;
    }
    public sealed class StrictBoxedNumberHolder
    {
        [JsonNumberHandling(JsonNumberHandling.Strict)]
        public object Value { get; set; } = null!;
    }
    public sealed class PlainNumbers
    {
        public int Integer { get; set; } = 3;
        public decimal Decimal { get; set; } = 4m;
        public double Double { get; set; } = 1.25;
    }

    [Fact]
    public void ExtensionDataWithNumberHandlingMatchesSystemTextJson()
    {
        var input = new ExtensionNumberHolder { Value = 1.25, Extra = new() { ["a"] = 2.5, ["nested"] = new PlainNumbers() } };
        var sj = new StructuredJson();
        sj.Set("x", input);
        Assert.Equal(JsonSerializer.Serialize(input), sj.GetRequired<JsonElement>("x").GetRawText());
        var restored = sj.GetRequired<ExtensionNumberHolder>("x")!;
        Assert.Equal(1.25, restored.Value);
        Assert.Equal("2.5", ((JsonElement)restored.Extra["a"]).GetString());
        var overflow = new StructuredJson("{\"x\":{\"Value\":\"1e400\",\"a\":2.5}}");
        Assert.False(overflow.TryGet<ExtensionNumberHolder>("x", out _));
    }

    [JsonNumberHandling(JsonNumberHandling.WriteAsString | JsonNumberHandling.AllowReadingFromString)]
    public sealed class ExtensionNumberHolder
    {
        public double Value { get; set; }
        [JsonExtensionData]
        public Dictionary<string, object> Extra { get; set; } = new();
    }

    [Fact]
    public void NonFiniteNumericDictionaryKeysAreRejected()
    {
        foreach (var key in new[] { "NaN", "Infinity", "-Infinity" })
        {
            var sj = new StructuredJson();
            sj.Set("x", new Dictionary<string, int> { [key] = 1 });
            Assert.False(sj.TryGet<Dictionary<double, int>>("x", out _));
            Assert.False(sj.TryGet<Dictionary<float, int>>("x", out _));
        }
        var valid = new StructuredJson("{\"x\":{\"1.25\":1}}");
        Assert.Equal(1, valid.GetRequired<Dictionary<double, int>>("x")![1.25]);
        Assert.Equal(1, valid.GetRequired<Dictionary<float, int>>("x")![1.25f]);
    }

    [Fact]
    public void PopulateWithScopedNumberPoliciesHasExplicitUnsupportedStatus()
    {
        const string json = "{\"Values\":[\"1.25\"]}";
        Assert.Equal(1.25, JsonSerializer.Deserialize<PopulateNumberHolder>(json)!.Values[0]);
        var sj = new StructuredJson("{\"x\":" + json + "}");
        Assert.False(sj.TryGet<PopulateNumberHolder>("x", out _));
        Assert.Throws<InvalidCastException>(() => sj.GetRequired<PopulateNumberHolder>("x"));
        Assert.Equal(1.25, JsonSerializer.Deserialize<PopulateTypedNumberHolder>(json)!.Values[0]);
        Assert.False(sj.TryGet<PopulateTypedNumberHolder>("x", out _));
        var plain = new StructuredJson("{\"x\":{\"Values\":[1.25]}}");
        Assert.Equal(1.25, plain.GetRequired<PlainPopulateHolder>("x")!.Values[0]);
    }

    public sealed class PopulateNumberHolder
    {
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
        public List<double> Values { get; } = new();
    }
    public sealed class PlainPopulateHolder
    {
        [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
        public List<double> Values { get; } = new();
    }
    public sealed class PopulateTypedNumberHolder
    {
        [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
        public StringNumbers Values { get; } = new();
    }

    [Fact]
    public void NestedNumericStringsFollowNativeSyntaxAndRejectNonFiniteResults()
    {
        var settings = new JsonSerializerOptions { NumberHandling = JsonNumberHandling.AllowReadingFromString };
        foreach (var text in new[] { "1.25", "+1.25", " 1.25", "1.25 ", "01.25", "1,25", "NaN", "Infinity", "1e400", "1e-9999", "" })
        {
            string json = JsonSerializer.Serialize(new { Value = text });
            bool expected;
            try { expected = double.IsFinite(JsonSerializer.Deserialize<FloatHolder>(json, settings)!.Value); }
            catch (JsonException) { expected = false; }
            var sj = new StructuredJson("{\"x\":" + json + "}", new() { SerializerOptions = settings });
            Assert.Equal(expected, sj.TryGet<FloatHolder>("x", out _));
        }
    }

    [Fact]
    public void HalfConversionsRejectNonFiniteKeysAndHonorMemberPolicies()
    {
        foreach (var key in new[] { "NaN", "Infinity", "-Infinity" })
        {
            var sj = new StructuredJson();
            sj.Set("x", new Dictionary<string, int> { [key] = 1 });
            Assert.False(sj.TryGet<Dictionary<Half, int>>("x", out _));
        }
        var valid = new StructuredJson("{\"map\":{\"1.25\":1},\"holder\":{\"Value\":\"1.25\"},\"overflow\":[1e400]}");
        Assert.Equal(1, valid.GetRequired<Dictionary<Half, int>>("map")![(Half)1.25]);
        Assert.Equal((Half)1.25, valid.GetRequired<HalfHolder>("holder")!.Value);
        Assert.False(valid.TryGet<Half[]>("overflow", out _));
    }

    public sealed class HalfHolder
    {
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public Half Value { get; set; }
    }

    [Fact]
    public void MalformedUtf16PathsCannotCreateRoundTripKeyCollisions()
    {
        string high = new((char)0xD800, 1), low = new((char)0xDC00, 1);
        var sj = new StructuredJson();
        sj.Set("\uFFFD", 2);
        string before = sj.ToJson();
        foreach (var path in new[] { high, low, "a" + high + "b", low + high, high + ":" + low })
        {
            Assert.Throws<ArgumentException>(() => sj.Set(path, 1));
            Assert.Throws<ArgumentException>(() => sj.Get(path));
            Assert.False(sj.HasPath(path));
            Assert.False(sj.Remove(path));
            Assert.Equal(before, sj.ToJson());
        }
        sj.Set("\U0001F600:value", 3);
        var restored = new StructuredJson(sj.ToJson());
        Assert.Equal(2, restored.GetRequired<int>("\uFFFD"));
        Assert.Equal(3, restored.GetRequired<int>("\U0001F600:value"));
    }

    public sealed record Marker(int Value);
    private sealed class MarkerConverter : JsonConverter<Marker>
    {
        public override Marker Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => new(int.Parse(reader.GetString()![1..], System.Globalization.CultureInfo.InvariantCulture));
        public override void Write(Utf8JsonWriter writer, Marker value, JsonSerializerOptions options) => writer.WriteStringValue("v" + value.Value);
    }
}
