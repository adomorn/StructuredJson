using System.Globalization;
using System.Text.Json;
using SJ = StructuredJson.StructuredJson;
static void Check(string name, Action action) { Console.WriteLine($"CASE {name}"); try { action(); } catch (Exception e) { Console.WriteLine($"THREW {e.GetType().Name}: {e.Message}"); } }
static string Json(SJ sj) => sj.ToJson(new JsonSerializerOptions { WriteIndented = false });
Check("malformed-paths", () => {
    foreach (var path in new[] { "items[", "items[0]junk", "]items", "user::name", ":user:name:", "items[0]]" }) {
        var sj = new SJ(); sj.Set(path, 42); Console.WriteLine($"{path} => {Json(sj)}");
    }
    var victim = new SJ("{\"secret\":42}"); Console.WriteLine($"Remove(secret[)={victim.Remove("secret[")}; remaining={Json(victim)}");
});
Check("numeric-roundtrip", () => {
    foreach (var token in new[] { "0.1234567890123456789012345678", "18446744073709551615", "79228162514264337593543950335", "1e400" }) {
        var sj = new SJ("{\"n\":"+token+"}");
        try { Console.WriteLine($"{token} => {Json(sj)}; decimal={sj.Get<decimal>("n")}; ulong={sj.Get<ulong>("n")}"); }
        catch(Exception e) { Console.WriteLine($"{token} parsed as {sj.Get("n")}; serialize throws {e.GetType().Name}"); }
    }
});
Check("unsupported-roots", () => { foreach(var json in new[]{"[1,2]", "42", "true", "\"text\"", "null", " "}) Console.WriteLine($"{json} => {Json(new SJ(json))}"); });
Check("typed-array", () => {
    var sj = new SJ(); sj.Set("items", new[] {10,20}); Console.WriteLine($"before={Json(sj)}; Get[0]={sj.Get("items[0]") ?? "NULL"}; Has[0]={sj.HasPath("items[0]")}; Remove[0]={sj.Remove("items[0]")}");
    sj.Set("items[1]",30); Console.WriteLine($"after={Json(sj)}");
});
Check("poco-and-typed-dictionary", () => {
    foreach(var obj in new object[] {new { Name="Ada", Age=30 }, new Dictionary<string,string>{{"Name","Ada"},{"Age","30"}}}) {
        var sj=new SJ(); sj.Set("user",obj); Console.WriteLine($"before={Json(sj)}; GetName={sj.Get("user:Name") ?? "NULL"}"); sj.Set("user:Name","Grace"); Console.WriteLine($"after={Json(sj)}");
    }
});
Check("reserved-keys-and-collision", () => {
    var sj=new SJ("{\"a:b\":1,\"a\":{\"b\":2},\"x[0]\":3,\"\":4}"); Console.WriteLine($"json={Json(sj)}; ListPaths={JsonSerializer.Serialize(sj.ListPaths())}; Get(a:b)={sj.Get("a:b")}; Has(x[0])={sj.HasPath("x[0]")}");
});
Check("true-nested-arrays", () => {var sj=new SJ("{\"m\":[[1,2],[3,4]]}"); Console.WriteLine($"json={Json(sj)}; ListPaths={JsonSerializer.Serialize(sj.ListPaths())}"); Console.WriteLine(sj.Get("m[0][1]"));});
Check("culture", () => {
    var old=CultureInfo.CurrentCulture; try {foreach(var culture in new[]{"en-US","tr-TR"}) {CultureInfo.CurrentCulture=new CultureInfo(culture); var sj=new SJ(); sj.Set("n","123.45"); Console.WriteLine($"{culture}: {sj.Get<double>("n").ToString(CultureInfo.InvariantCulture)}");}} finally {CultureInfo.CurrentCulture=old;}
});
Check("numeric-type-matrix", () => {var sj=new SJ(); sj.Set("n","42"); Console.WriteLine($"int={sj.Get<int>("n")}; short={sj.Get<short>("n")}; byte={sj.Get<byte>("n")}; uint={sj.Get<uint>("n")}; ulong={sj.Get<ulong>("n")}");});
Check("null-and-empty-discovery", () => {var sj=new SJ("{\"obj\":{},\"arr\":[],\"nulls\":[null],\"n\":null}"); Console.WriteLine($"has nulls[0]={sj.HasPath("nulls[0]")}; ListPaths={JsonSerializer.Serialize(sj.ListPaths())}");});
Check("mutable-alias", () => {var dict=new Dictionary<string,object?>{{"value",1}}; var sj=new SJ(); sj.Set("x",dict); dict["value"]=2; var read=(Dictionary<string,object?>)sj.Get("x")!; read["other"]=3; Console.WriteLine(Json(sj));});
Check("disposed-json-element", () => {var sj=new SJ(); using(var doc=JsonDocument.Parse("{\"value\":1}")) sj.Set("x",doc.RootElement); Console.WriteLine(Json(sj));});
Check("sparse-allocation", () => {var sj=new SJ(); var before=GC.GetAllocatedBytesForCurrentThread(); sj.Set("items[999999]",1); var allocated=GC.GetAllocatedBytesForCurrentThread()-before; Console.WriteLine($"count={((List<object>)sj.Get("items")!).Count}; allocatedBytes={allocated}");});
Check("depth-mismatch", () => {var sj=new SJ(); var path=string.Join(":",Enumerable.Range(0,100).Select(i=>$"p{i}")); sj.Set(path,1); Console.WriteLine($"Get={sj.Get<int>(path)}; Has={sj.HasPath(path)}; ListPaths.Count={sj.ListPaths().Count}"); Console.WriteLine(Json(sj));});
