using System;
using StructuredJson;

namespace StructuredJsonExample
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("StructuredJson Kütüphanesi Örnek Kullanım");
            Console.WriteLine("==========================================");
            
            // Yeni bir StructuredJson instance oluştur
            var sj = new StructuredJson();
            
            // Basit değerler ekle
            sj.Set("user:name", "Ahmet Yılmaz");
            sj.Set("user:age", 30);
            sj.Set("user:isActive", true);
            
            // Adres bilgileri ekle (nested objects ve arrays)
            sj.Set("user:addresses[0]:type", "ev");
            sj.Set("user:addresses[0]:city", "Ankara");
            sj.Set("user:addresses[0]:country", "Türkiye");
            sj.Set("user:addresses[0]:postalCode", "06100");
            
            sj.Set("user:addresses[1]:type", "iş");
            sj.Set("user:addresses[1]:city", "İstanbul");
            sj.Set("user:addresses[1]:country", "Türkiye");
            sj.Set("user:addresses[1]:postalCode", "34000");
            
            // Hobiler ekle
            sj.Set("user:hobbies[0]", "kitap okuma");
            sj.Set("user:hobbies[1]", "yüzme");
            sj.Set("user:hobbies[2]", "programlama");
            
            // Değerleri oku
            Console.WriteLine("\n=== Değerleri Okuma ===");
            Console.WriteLine($"İsim: {sj.Get("user:name")}");
            Console.WriteLine($"Yaş: {sj.Get<int>("user:age")}");
            Console.WriteLine($"Aktif: {sj.Get<bool>("user:isActive")}");
            Console.WriteLine($"Ev Adresi: {sj.Get("user:addresses[0]:city")}, {sj.Get("user:addresses[0]:country")}");
            Console.WriteLine($"İş Adresi: {sj.Get("user:addresses[1]:city")}, {sj.Get("user:addresses[1]:country")}");
            Console.WriteLine($"İlk Hobi: {sj.Get("user:hobbies[0]")}");
            
            // Tüm path'leri listele
            Console.WriteLine("\n=== Tüm Path'ler ===");
            var paths = sj.ListPaths();
            foreach (var kvp in paths)
            {
                Console.WriteLine($"{kvp.Key}: {kvp.Value}");
            }
            
            // JSON çıktısı
            Console.WriteLine("\n=== JSON Çıktısı ===");
            var json = sj.ToJson();
            Console.WriteLine(json);
            
            // JSON'dan yeni instance oluştur
            Console.WriteLine("\n=== JSON'dan Yükleme ===");
            var existingJson = """
            {
                "config": {
                    "database": {
                        "host": "localhost",
                        "port": 5432,
                        "name": "myapp"
                    },
                    "features": ["auth", "logging", "caching"]
                }
            }
            """;
            
            var configSj = new StructuredJson(existingJson);
            Console.WriteLine($"Database Host: {configSj.Get("config:database:host")}");
            Console.WriteLine($"Database Port: {configSj.Get<int>("config:database:port")}");
            Console.WriteLine($"İlk Feature: {configSj.Get("config:features[0]")}");
            
            // Path kontrolü
            Console.WriteLine("\n=== Path Kontrolü ===");
            Console.WriteLine($"user:name var mı? {sj.HasPath("user:name")}");
            Console.WriteLine($"user:email var mı? {sj.HasPath("user:email")}");
            
            // Değer silme
            Console.WriteLine("\n=== Değer Silme ===");
            Console.WriteLine($"user:hobbies[1] siliniyor...");
            bool removed = sj.Remove("user:hobbies[1]");
            Console.WriteLine($"Silme başarılı: {removed}");
            Console.WriteLine($"Yeni hobbies[1]: {sj.Get("user:hobbies[1]")}"); // Artık "programlama" olacak
            
            Console.WriteLine("\nÖrnek tamamlandı!");
        }
    }
} 