using StructuredJson; var sj = new StructuredJson(); sj.Set("items[0]", "first"); Console.WriteLine("items[-1]: " + (sj.Get("items[-1]") ?? "null"));
