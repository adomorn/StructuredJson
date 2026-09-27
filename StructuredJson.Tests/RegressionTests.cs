using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using Xunit;

namespace StructuredJson.Tests;

public class RegressionTests
{
    [Theory]
    [InlineData("secret[")]
    [InlineData("]secret")]
    [InlineData("secret[0]junk")]
    [InlineData("secret::name")]
    [InlineData(":secret")]
    [InlineData("secret:")]
    public void InvalidPathsCannotMutateData(string path)
    {
        var sj = new StructuredJson("{\"secret\":42}");
        Assert.False(sj.Remove(path));
        Assert.Throws<ArgumentException>(() => sj.Set(path, 99));
        Assert.Equal(42, sj.Get<int>("secret"));
    }

    [Theory]
    [InlineData("0.1234567890123456789012345678")]
    [InlineData("18446744073709551615")]
    [InlineData("79228162514264337593543950335")]
    [InlineData("1e400")]
    public void NumberTokensRoundTripWithoutLoss(string number)
    {
        var sj = new StructuredJson("{\"n\":" + number + "}");
        using var doc = JsonDocument.Parse(sj.ToJson());
        Assert.Equal(number, doc.RootElement.GetProperty("n").GetRawText());
    }

    [Theory]
    [InlineData("[1,2]")]
    [InlineData("true")]
    [InlineData("42")]
    [InlineData("null")]
    [InlineData("\"text\"")]
    public void NonObjectRootsAreRejected(string json) => Assert.Throws<ArgumentException>(() => new StructuredJson(json));

    [Fact]
    public void ClrArraysCanBeEditedWithoutLosingSiblings()
    {
        var sj = new StructuredJson();
        sj.Set("items", new[] { 10, 20 });
        Assert.Equal(10, sj.Get<int>("items[0]"));
        sj.Set("items[1]", 30);
        Assert.Equal(10, sj.Get<int>("items[0]"));
        Assert.Equal(30, sj.Get<int>("items[1]"));
    }

    [Fact]
    public void PocoPropertiesCanBeEditedWithoutLosingSiblings()
    {
        var sj = new StructuredJson();
        sj.Set("user", new { Name = "Ada", Age = 30 });
        sj.Set("user:Name", "Grace");
        Assert.Equal(30, sj.Get<int>("user:Age"));
    }

    [Fact]
    public void JsonElementSurvivesItsDocument()
    {
        var sj = new StructuredJson();
        using (var document = JsonDocument.Parse("{\"n\":42}")) sj.Set("x", document.RootElement);
        Assert.Equal(42, sj.Get<int>("x:n"));
        Assert.Contains("42", sj.ToJson());
    }

    [Fact]
    public void EscapedAndEmptyKeysAreDistinct()
    {
        var sj = new StructuredJson("{\"a:b\":1,\"a\":{\"b\":2},\"x[0]\":3,\"\":4}");
        Assert.Equal(1, sj.Get<int>(@"a\:b"));
        Assert.Equal(2, sj.Get<int>("a:b"));
        Assert.Equal(3, sj.Get<int>(@"x\[0\]"));
        Assert.Equal(4, sj.Get<int>(@"\e"));
        Assert.Equal(4, sj.ListPaths().Count);
    }

    [Fact]
    public void NestedArraysSupportAllOperations()
    {
        var sj = new StructuredJson("{\"m\":[[1,2],[3,4]]}");
        Assert.Equal(2, sj.Get<int>("m[0][1]"));
        sj.Set("m[0][1]", 5);
        Assert.True(sj.Remove("m[1][0]"));
        Assert.Equal(4, sj.Get<int>("m[1][0]"));
        Assert.Equal(5, sj.ListPaths()["m[0][1]"]);
    }

    [Fact]
    public void AllIntegerStringConversionsWork()
    {
        var sj = new StructuredJson(); sj.Set("n", "42");
        Assert.Equal((byte)42, sj.Get<byte>("n"));
        Assert.Equal((short)42, sj.Get<short>("n"));
        Assert.Equal(42u, sj.Get<uint>("n"));
        Assert.Equal(42ul, sj.Get<ulong>("n"));
    }
}
