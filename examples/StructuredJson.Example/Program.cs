using System.Text.Json;
using System.Text.Json.Nodes;
using SJ = StructuredJson.StructuredJson;

namespace StructuredJson.Example;

internal static class Program
{
    private static readonly int[] Scores = { 10, 20 };
    private const string QuickStartInput = """{"user":{"name":"Ada","scores":[10,20]}}""";

    private static int Main(string[] args)
    {
        if (args.Length > 1)
            return Usage();
        string scenario = args.Length == 0 ? "all" : args[0];
        switch (scenario)
        {
            case "all":
                QuickStart();
                Configuration();
                ApiResponse();
                TestFixtures();
                CompareJsonNode();
                break;
            case "configuration": Configuration(); break;
            case "api-response": ApiResponse(); break;
            case "test-fixtures": TestFixtures(); break;
            case "comparison": CompareJsonNode(); break;
            default: return Usage();
        }
        Console.WriteLine("Examples passed: " + scenario);
        return 0;
    }

    private static int Usage()
    {
        Console.Error.WriteLine("Usage: [all|configuration|api-response|test-fixtures|comparison]");
        return 1;
    }

    private static void QuickStart()
    {
        var json = new SJ(QuickStartInput);
        json.Set("user:scores[1]", 30);
        json.Set("user:preferences:theme", "dark");
        Require(json.GetRequired<int>("user:scores[0]") == 10, "The first score must survive the edit.");
        AssertJson(json.ToJson(), """{"user":{"name":"Ada","scores":[10,30],"preferences":{"theme":"dark"}}}""");
        // Keep escaped paths, nested arrays and round trips in the package smoke check.
        json.Set(@"metadata:build\:version", "2.0.0");
        json.Set("matrix[0][1]", 42);
        var copy = new SJ(json.ToJson());
        Require(copy.GetRequired<string>(@"metadata:build\:version") == "2.0.0", "Escaped property must round-trip.");
        Require(copy.GetRequired<int>("matrix[0][1]") == 42, "Nested array must round-trip.");
    }

    private static void Configuration()
    {
        const string input = """{"Logging":{"LogLevel":{"Default":"Information","System":"Warning"}},"AllowedHosts":"*"}""";
        var config = new SJ(input);
        config.Set("Logging:LogLevel:Default", "Debug");
        config.Set("Features:Search:Enabled", true);
        AssertJson(config.ToJson(), """{"Logging":{"LogLevel":{"Default":"Debug","System":"Warning"}},"AllowedHosts":"*","Features":{"Search":{"Enabled":true}}}""");
        Print("Configuration", config);
    }

    private static void ApiResponse()
    {
        const string input = """{"items":[{"id":"p1","price":"19.95","debug":"internal"},{"id":"p2","price":"8.50"}],"meta":{"page":1}}""";
        var response = new SJ(input);
        decimal price = response.GetRequired<decimal>("items[0]:price");
        response.Set("items[0]:price", price);
        response.Set("items[0]:available", true);
        Require(response.Remove("items[0]:debug"), "The example debug field must be removed.");
        Require(!response.TryGet<int>("meta:nextPage", out _), "The next page is absent.");
        AssertJson(response.ToJson(), """{"items":[{"id":"p1","price":19.95,"available":true},{"id":"p2","price":"8.50"}],"meta":{"page":1}}""");
        Print("API response", response);
    }

    private static void TestFixtures()
    {
        var baseline = new SJ("""{"user":{"name":"Ada","active":true}}""");
        baseline.Set("user:scores", Scores);
        var variant = new SJ(baseline.ToJson());
        variant.Set("user:active", false);
        variant.Set("user:scores[1]", 0);
        int[] scores = variant.GetRequired<int[]>("user:scores")!;
        scores[0] = 999; // Typed reads are detached from the stored document.
        AssertJson(baseline.ToJson(), """{"user":{"name":"Ada","active":true,"scores":[10,20]}}""");
        AssertJson(variant.ToJson(), """{"user":{"name":"Ada","active":false,"scores":[10,0]}}""");
        Print("Test fixture", variant);
    }

    private static void CompareJsonNode()
    {
        var structured = new SJ(QuickStartInput);
        structured.Set("user:scores[1]", 30);
        structured.Set("user:preferences:theme", "dark");

        var node = JsonNode.Parse(QuickStartInput)!.AsObject();
        var user = node["user"]!.AsObject();
        user["scores"]!.AsArray()[1] = 30;
        user["preferences"] = new JsonObject { ["theme"] = "dark" };
        // The known input has no preferences. For arbitrary input, check before replacing it.
        AssertJson(structured.ToJson(), node.ToJsonString());
        Console.WriteLine("JsonNode and StructuredJson produce the same JSON for the documented input.");
    }

    private static void AssertJson(string actual, string expected) =>
        Require(JsonNode.DeepEquals(JsonNode.Parse(actual), JsonNode.Parse(expected)), "Unexpected JSON result.");

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static void Print(string title, SJ json)
    {
        Console.WriteLine(title + ":");
        Console.WriteLine(json.ToJson(new JsonSerializerOptions { WriteIndented = true }));
    }
}
