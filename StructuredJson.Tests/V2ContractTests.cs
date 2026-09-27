using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Xunit;

namespace StructuredJson.Tests;

public class V2ContractTests
{
    [Fact]
    public void TypeConflictRejectsWithoutMutation()
    {
        var sj = new StructuredJson("{\"a\":[1,2],\"b\":3}"); var before = sj.ToJson();
        Assert.Throws<InvalidOperationException>(() => sj.Set("a[1]:x:y", 1));
        Assert.Equal(before, sj.ToJson());
        var replace = new StructuredJson("{\"a\":[1,2]}", new() { OverwriteOnTypeConflict = true });
        replace.Set("a:x", 3); Assert.Equal(3, replace.Get<int>("a:x"));
    }

    [Fact]
    public void BoundsAreCheckedBeforeAnyMutation()
    {
        var sj = new StructuredJson(new StructuredJsonOptions() { MaxArrayLength = 4, MaxNodeCount = 8, MaxDepth = 4, MaxPathLength = 20 });
        sj.Set("keep", 1); var before = sj.ToJson();
        Assert.Throws<ArgumentException>(() => sj.Set("a[4]", 1));
        Assert.Throws<ArgumentException>(() => sj.Set("a[3][3]", 1));
        Assert.Throws<ArgumentException>(() => sj.Set("a:b:c:d:e", 1));
        Assert.Throws<ArgumentException>(() => sj.Set("a", new[] { 1, 2, 3, 4, 5 }));
        Assert.Throws<ArgumentException>(() => sj.Set("a", new Dictionary<string, object?> { { new string('x', 21), 1 } }));
        Assert.Equal(before, sj.ToJson());
    }

    [Fact]
    public void NodeBudgetRecoversAfterRemoveAndClear()
    {
        var sj = new StructuredJson(new StructuredJsonOptions() { MaxNodeCount = 5 });
        sj.Set("a[2]", 1);
        Assert.Throws<ArgumentException>(() => sj.Set("b", 2));
        sj.Remove("a[0]"); sj.Set("b", 2);
        Assert.Equal(2, sj.Get<int>("b"));
        sj.Clear(); sj.Set("c[2]", 3); Assert.Equal(3, sj.Get<int>("c[2]"));
    }

    [Fact]
    public void SnapshotsAndInputValuesCannotMutateStoredData()
    {
        var input = new Dictionary<string, object?> { { "n", 1 } };
        var sj = new StructuredJson(); sj.Set("x", input); input["n"] = 7;
        var read = sj.Get<Dictionary<string, int>>("x")!; read["n"] = 8;
        ((Dictionary<string, object?>)sj.Get("x")!)["n"] = 9;
        Assert.Equal(1, sj.Get<int>("x:n"));
    }

    [Fact]
    public void CyclicInputIsRejectedWithoutPartialState()
    {
        var cycle = new Dictionary<string, object?>(); cycle["self"] = cycle;
        var sj = new StructuredJson(); sj.Set("keep", 1);
        Assert.Throws<ArgumentException>(() => sj.Set("a:b", cycle));
        Assert.False(sj.HasPath("a")); Assert.Equal(1, sj.Get<int>("keep"));
    }

    [Fact]
    public void NullEmptyAndReservedPathsAreDiscoverable()
    {
        var sj = new StructuredJson("{\"\":{},\"a:b\":[],\"x[0]\":[null],\"slash\\\\e\":1}");
        var paths = sj.ListPaths(); Assert.Equal(4, paths.Count);
        foreach (var path in paths.Keys) Assert.True(sj.HasPath(path));
        Assert.Null(paths[@"x\[0\][0]"]);
        Assert.Equal(1, sj.Get<int>(@"slash\\e"));
    }

    [Fact]
    public void TypedReadsDistinguishMissingNullAndBadConversion()
    {
        var sj = new StructuredJson("{\"zero\":0,\"null\":null,\"bad\":\"oops\"}");
        Assert.True(sj.TryGet<int>("zero", out var zero)); Assert.Equal(0, zero);
        Assert.False(sj.TryGet<int>("missing", out _)); Assert.False(sj.TryGet<int>("bad", out _));
        Assert.True(sj.TryGet<int?>("null", out var nullable)); Assert.Null(nullable);
        Assert.False(sj.TryGet<int>("null", out _));
        Assert.Throws<KeyNotFoundException>(() => sj.GetRequired<int>("missing"));
        Assert.Throws<InvalidCastException>(() => sj.GetRequired<int>("bad"));
    }

    [Fact]
    public void NumberConversionIsInvariantAndDoesNotAcceptThousands()
    {
        var old = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("tr-TR");
            var sj = new StructuredJson(); sj.Set("n", "123.45");
            Assert.Equal(123.45, sj.Get<double>("n"));
            var tr = new StructuredJson(new StructuredJsonOptions() { NumberCulture = new CultureInfo("tr-TR") });
            tr.Set("n", "123,45"); Assert.Equal(123.45, tr.Get<double>("n"));
            tr.Set("n", "123.45"); Assert.False(tr.TryGet<double>("n", out _));
        }
        finally { CultureInfo.CurrentCulture = old; }
    }

    [Theory]
    [InlineData("NaN")]
    [InlineData("Infinity")]
    [InlineData("1e400")]
    public void NonFiniteTypedConversionsFail(string text)
    {
        var sj = new StructuredJson(); sj.Set("n", text); Assert.False(sj.TryGet<double>("n", out _));
    }

    [Fact]
    public void NumberBoundariesConvertExactly()
    {
        var sj = new StructuredJson("{\"u\":18446744073709551615,\"d\":79228162514264337593543950335,\"huge\":1e400}");
        Assert.Equal(ulong.MaxValue, sj.GetRequired<ulong>("u"));
        Assert.Equal(decimal.MaxValue, sj.GetRequired<decimal>("d"));
        Assert.False(sj.TryGet<double>("huge", out _));
        Assert.False(sj.TryGet<int>("u", out _));
    }

    [Theory]
    [InlineData("{\"a\":1,\"a\":2}")]
    [InlineData("{\"x\":[{\"a\":1,\"a\":2}]}")]
    [InlineData("")]
    [InlineData(" ")]
    public void AmbiguousOrEmptyJsonIsRejected(string json) => Assert.Throws<ArgumentException>(() => new StructuredJson(json));

    [Fact]
    public void DepthLimitsAgreeBetweenSetAndSerialization()
    {
        var sj = new StructuredJson(new StructuredJsonOptions() { MaxDepth = 3 }); sj.Set("a:b:c", 1);
        Assert.Equal(1, new StructuredJson(sj.ToJson(), new() { MaxDepth = 3 }).Get<int>("a:b:c"));
        Assert.Throws<ArgumentException>(() => sj.Set("a:b:c:d", 1));
        Assert.Throws<ArgumentException>(() => sj.Set("a:b", new { c = new { d = 1 } }));
    }

    [Fact]
    public void SerializerOptionsAreSharedAndSnapshotted()
    {
        var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, PropertyNameCaseInsensitive = true };
        var sj = new StructuredJson(new StructuredJsonOptions() { SerializerOptions = jsonOptions });
        jsonOptions.PropertyNamingPolicy = null;
        sj.Set("person", new Person { Name = "Ada" });
        Assert.Equal("Ada", sj.Get<string>("person:name"));
        Assert.Equal("Ada", sj.GetRequired<Person>("person")!.Name);
    }

    [Fact]
    public void ConcurrentOperationsProduceAConsistentSnapshot()
    {
        var sj = new StructuredJson();
        Parallel.For(0, 200, i => { sj.Set("p" + i, i); _ = sj.ListPaths(); _ = sj.ToJson(); });
        Assert.Equal(200, sj.ListPaths().Count);
        for (int i = 0; i < 200; i++) Assert.Equal(i, sj.Get<int>("p" + i));
    }

    [Fact]
    public void LongEscapedKeysAndNestedArraysRemainRoundTrippable()
    {
        var sj = new StructuredJson();
        sj.Set(@"a\:b[1][2]:\e", 42);
        var copy = new StructuredJson(sj.ToJson());
        Assert.Equal(42, copy.Get<int>(@"a\:b[1][2]:\e"));
        foreach (var path in copy.ListPaths().Keys) Assert.True(copy.HasPath(path));
    }

    public sealed class Person { public string Name { get; set; } = ""; }
}
