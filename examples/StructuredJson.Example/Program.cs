using SJ = StructuredJson.StructuredJson;

var json = new SJ();
json.Set("user:name", "Ada");
json.Set("user:scores", new[] { 10, 20 });
json.Set("user:scores[1]", 30);
json.Set(@"metadata:build\:version", "2.0.0");
json.Set("matrix[0][1]", 42);
if (json.GetRequired<int>("user:scores[0]") != 10 || json.GetRequired<int>("matrix[0][1]") != 42)
    throw new InvalidOperationException("Package smoke test failed.");
var copy = new SJ(json.ToJson());
if (copy.GetRequired<string>(@"metadata:build\:version") != "2.0.0")
    throw new InvalidOperationException("Package round-trip failed.");
Console.WriteLine(json.ToJson());
