using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using SJ = StructuredJson.StructuredJson;

BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);

[MemoryDiagnoser]
public class JsonBenchmarks
{
    private SJ _json = null!;
    [GlobalSetup]
    public void Setup()
    {
        _json = new SJ();
        for (int i = 0; i < 1000; i++) _json.Set($"items[{i}]:value", i);
    }
    [Benchmark] public int ReadPath() => _json.Get<int>("items[500]:value");
    [Benchmark] public void UpdatePath() => _json.Set("items[500]:value", 42);
    [Benchmark] public string Serialize() => _json.ToJson();
    [Benchmark] public Dictionary<string, object?> Discover() => _json.ListPaths();
}
