using System;
using System.Collections.Generic;
using System.Globalization; // Tarih ve saat format işlemleri için gerekli
using System.Linq; // Listeler üzerinde sorgulama yapmak (Where, Select) için gerekli
using System.Net.Http; // İnternet istekleri (API bağlantısı) için gerekli
using System.Text.Json; // JSON verisini okuyup C# nesnesine çevirmek için gerekli
using System.Threading.Tasks; // Asenkron işlemler (Async/Await) için gerekli

namespace PublicHolidayTracker
{
    // Soru işareti (?) bu değerlerin boş (null) gelebileceğini belirtir, böylece program hata vermez.
    public class Holiday
    {
        public string? date { get; set; }        // Tatil tarihi (Format: yyyy-MM-dd)
        public string? localName { get; set; }   // Yerel isim (Örn: Cumhuriyet Bayramı)
        public string? name { get; set; }        // İngilizce isim (Örn: Republic Day)
        public string? countryCode { get; set; } // Ülke kodu (TR)

        // ÖNEMLİ: 'fixed' C# dilinde özel bir kelimedir (keyword) bu yüzden değişken adı olarak kullanmak için başına '@' işareti koyduk.
        public bool @fixed { get; set; }

        public bool global { get; set; }         // Ulusal tatil mi?
    }

    class Program
    {
        // API isteklerini yönetecek nesnemiz (Static tanımladık ki her istekte yeniden oluşup belleği yormasın)
        private static readonly HttpClient client = new HttpClient();

        // Tüm tatil verilerini hafızada tutacağımız ana listemiz
        private static List<Holiday> allHolidays = new List<Holiday>();

        // Programımın başlangıç noktası
        // İnternetten veri çekeceğimiz için 'async Task' yapısını kullandım
        static async Task Main(string[] args)
        {
            Console.Title = "Public Holiday Tracker - Türkiye (2023-2025)";

            Console.WriteLine("Veriler API üzerinden çekiliyor, lütfen bekleyiniz...");

            // Uygulama açılırken verileri internetten indirip hafızaya alıyoruz
            await LoadHolidaysAsync();

            // Eğer hiç veri gelmediyse (İnternet yoksa vs.) kullanıcıyı uyarıp kapat
            if (allHolidays.Count == 0)
            {
                Console.WriteLine("HATA: Veriler sunucudan alınamadı. İnternet bağlantınızı kontrol edin.");
                Console.ReadLine(); // Hemen kapanmasın diye bekle
                return;
            }

            // Menü Döngüsü
            bool exit = false;
            while (!exit)
            {
                Console.Clear(); // Her işlemden sonra ekranı temizliyoruz ki karışıklık olmasın

                Console.WriteLine("========================================");
                Console.WriteLine("      TÜRKİYE RESMİ TATİL TAKİBİ");
                Console.WriteLine($"      (Hafızadaki Kayıt Sayısı: {allHolidays.Count})");
                Console.WriteLine("========================================");
                Console.WriteLine("1. Yıla Göre Listele (2023, 2024, 2025)");
                Console.WriteLine("2. Tarihe Göre Ara (Örn: 15-07 veya 1.1)");
                Console.WriteLine("3. İsme Göre Ara (Örn: Ramazan)");
                Console.WriteLine("4. Tüm Listeyi Göster");
                Console.WriteLine("5. Çıkış");
                Console.Write("Seçiminiz: ");

                string secim = Console.ReadLine() ?? ""; // Boş gelirse hata vermesin

                switch (secim)
                {
                    case "1":
                        YearSelectionMenu();
                        break;
                    case "2":
                        SearchByDate();
                        break;
                    case "3":
                        SearchByName();
                        break;
                    case "4":
                        ListAllHolidays();
                        break;
                    case "5":
                        exit = true;
                        Console.WriteLine("Program kapatılıyor...");
                        break;
                    default:
                        Console.WriteLine("Geçersiz seçim, lütfen tekrar deneyin.");
                        break;
                }

                // EĞER ÇIKIŞ YAPILMADIYSA BEKLİYORUZ:
                // Kullanıcı sonuçları görsün ve hemen ekranı silmeyelim.
                if (!exit)
                {
                    Console.WriteLine("\nAna menüye dönmek için Enter'a basınız...");
                    Console.ReadLine();
                }
            }
        }

        // API'den verileri çeken asenkron metodumuz
        private static async Task LoadHolidaysAsync()
        {
            int[] years = { 2023, 2024, 2025 }; // Çekilecek yıllar
            string baseUrl = "https://date.nager.at/api/v3/PublicHolidays/";

            // JSON Ayarı: Büyük/Küçük harf duyarlılığını kaldırıyoruz
            // (API bazen "Date" bazen "date" gönderebilir, ikisini de kabul etsin)
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            foreach (var year in years)
            {
                try
                {
                    // URL oluşturma: .../PublicHolidays/2024/TR
                    string url = $"{baseUrl}{year}/TR";

                    // JSON verisini metin (string) olarak indiriyoruz
                    string jsonResponse = await client.GetStringAsync(url);

                    // İndirilen metni bizim C# 'Holiday' sınıfımıza dönüştürüyoruz (Deserialize)
                    var yearHolidays = JsonSerializer.Deserialize<List<Holiday>>(jsonResponse, options);

                    // Listeye ekliyoruz
                    if (yearHolidays != null)
                    {
                        allHolidays.AddRange(yearHolidays);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"{year} yılı verisi çekilemedi: {ex.Message}");
                }
            }
        }

        // Yıl filtreleme
        private static void YearSelectionMenu()
        {
            Console.Write("\nListelemek istediğiniz yılı girin: ");
            string inputYear = Console.ReadLine() ?? "";

            // LINQ ile filtreleme: Tarihi girilen yılla başlayanları getiriyoruz
            var filteredHolidays = allHolidays
                .Where(h => h.date != null && h.date.StartsWith(inputYear))
                .ToList();

            if (filteredHolidays.Count > 0)
            {
                Console.WriteLine($"\n--- {inputYear} Yılı Resmi Tatilleri ---");
                PrintTable(filteredHolidays);
            }
            else
            {
                Console.WriteLine("Bu yıla ait veri bulunamadı.");
            }
        }

        // Akıllı Tarih Arama (Hata toleranslı)
        private static void SearchByDate()
        {
            Console.WriteLine("\n--- Tarihe Göre Arama ---");
            Console.Write("Tarih girin (Gün ve Ay): ");
            string input = Console.ReadLine() ?? "";

            // 1. Kullanıcının girdiği noktaları, slashları boşlukları tire (-) yapıyoruz.
            // Örn: "15.07" -> "15-07" olur.
            string cleanInput = input.Replace(".", "-").Replace("/", "-").Replace(" ", "-");

            // 2. Tireden parçalara ayırıyoruz (Gün ve Ay olarak)
            string[] parts = cleanInput.Split('-');

            if (parts.Length >= 2)
            {
                // Sayıya çevirmeyi deniyoruz (Böylece "07" ile "7" aynı sayılır)
                if (int.TryParse(parts[0], out int searchDay) && int.TryParse(parts[1], out int searchMonth))
                {
                    // Listede arama yapıyoruz
                    var foundHolidays = allHolidays.Where(h =>
                    {
                        if (string.IsNullOrEmpty(h.date)) return false;

                        // API'den gelen tarihi (yyyy-MM-dd) C# tarih formatına çeviriyoruz
                        if (DateTime.TryParseExact(h.date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dt))
                        {
                            // Yıl fark etmeksizin Gün ve Ay tutuyor mu?
                            return dt.Day == searchDay && dt.Month == searchMonth;
                        }
                        return false;
                    }).ToList();

                    if (foundHolidays.Count > 0)
                    {
                        Console.WriteLine($"\n--- {searchDay}.{searchMonth} Tarihindeki Tatiller ---");
                        PrintTable(foundHolidays);
                    }
                    else
                    {
                        Console.WriteLine("Bu tarihte bir resmi tatil bulunamadı.");
                    }
                }
                else
                {
                    Console.WriteLine("Geçersiz format! Lütfen sayısal tarih giriniz.");
                }
            }
            else
            {
                Console.WriteLine("Hatalı giriş! Lütfen Gün-Ay şeklinde giriniz (Örn: 15-07).");
            }
        }

        // İsme göre arama
        private static void SearchByName()
        {
            Console.Write("\nTatil adı girin (Örn: Cumhuriyet): ");
            string keyword = (Console.ReadLine() ?? "").ToLower(); // Hepsini küçük harfe çeviriyoruz

            // Hem Türkçe isminde (localName) hem İngilizce isminde (name) arama yapıyoruz
            var foundHolidays = allHolidays.Where(h =>
                (h.localName != null && h.localName.ToLower().Contains(keyword)) ||
                (h.name != null && h.name.ToLower().Contains(keyword))
            ).ToList();

            if (foundHolidays.Count > 0)
            {
                Console.WriteLine($"\n--- '{keyword}' İçeren Tatiller ---");
                PrintTable(foundHolidays);
            }
            else
            {
                Console.WriteLine("Bu isimle eşleşen tatil bulunamadı.");
            }
        }

        // Tüm listeyi yazdırıyoruz
        private static void ListAllHolidays()
        {
            Console.WriteLine("\n--- 2023-2025 Tüm Resmi Tatiller ---");
            PrintTable(allHolidays);
        }

        // Yardımcı Metod: Verileri tablo düzeninde ekrana basaıyoruz
        private static void PrintTable(List<Holiday> holidays)
        {
            // Tablo başlıkları (Sola yaslı ve belli boşluklarla)
            Console.WriteLine("{0,-12} {1,-40} {2,-30}", "TARİH", "YEREL İSİM", "ULUSLARARASI İSİM");
            Console.WriteLine(new string('-', 90));

            foreach (var h in holidays)
            {
                // Değer null ise yerine "-" çizgisini koyuyoruz
                string d = h.date ?? "-";
                string ln = h.localName ?? "-";
                string n = h.name ?? "-";

                Console.WriteLine("{0,-12} {1,-40} {2,-30}", d, ln, n);
            }
            Console.WriteLine($"\nToplam {holidays.Count} kayıt listelendi.");
        }
    }
}