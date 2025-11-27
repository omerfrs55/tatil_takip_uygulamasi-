using System;
using System.Collections.Generic;
using System.Globalization; // Tarih ve saat format işlemleri için
using System.Linq; // Listeler üzerinde sorgulama yapmak (Where, Select) için
using System.Net.Http; // İnternet istekleri (API) için
using System.Text.Json; // JSON verisini okumak için
using System.Threading.Tasks; // Asenkron işlemler (Async/Await) için

namespace PublicHolidayTracker
{
    // Soru işaretimiz (?) bu değerleri boş (null) gelebileceğini belirtir, hata almamızı engeller.
    public class Holiday
    {
        public string? date { get; set; }        // Tatil tarihi (YYYY-MM-DD)
        public string? localName { get; set; }   // Yerel isim (Örnek: Cumhuriyet Bayramı)
        public string? name { get; set; }        // İngilizce isim (Örnek: Republic Day)
        public string? countryCode { get; set; } // Ülke kodu (TR)

        // ÖNEMLİ DETAY: 'fixed' C# dilinde rezerve edilmiş bir kelimedir Bu nedenle değişken ismi olarak kullanmak için başına '@' işareti koyuyoruz.
        public bool @fixed { get; set; }

        public bool global { get; set; }
    }

    class Program
    {
        // API isteklerini yönetecek nesnemiz (Static olması bellek yönetimimiz için daha iyi oluyor)
        private static readonly HttpClient client = new HttpClient();

        // Tüm tatil verilerini hafızada tutacağımız ana listemiz
        private static List<Holiday> allHolidays = new List<Holiday>();

        // "Programın giriş noktası" Async Task yapıyoruz çünkü internetten veri çekeceğiz.
        static async Task Main(string[] args)
        {
            // Konsol başlığı
            Console.Title = "Public Holiday Tracker - Türkiye (2023-2025)";

            Console.WriteLine("Veriler API üzerinden çekiliyor, lütfen bekleyiniz...");

            // Verileri İnternetten Yükleme İşlemimiz
            await LoadHolidaysAsync();

            Console.Clear(); // Yükleme bitince ekranı temizleme

            // Eğer hiç veri çekilemediyse programı durdurmak için (İnternet yoksa vb.)
            if (allHolidays.Count == 0)
            {
                Console.WriteLine("HATA: Veriler sunucudan alınamadı. İnternet bağlantınızı kontrol edin.");
                return;
            }

            Console.WriteLine($"Sistem Hazır! Toplam {allHolidays.Count} adet tatil kaydı hafızaya alındı.");

            // Kullanıcı Menüsü
            bool exit = false;
            while (!exit)
            {
                Console.WriteLine("\n========================================");
                Console.WriteLine("      TÜRKİYE RESMİ TATİL TAKİBİ");
                Console.WriteLine("========================================");
                Console.WriteLine("1. Yıla Göre Listele (2023, 2024, 2025)");
                Console.WriteLine("2. Tarihe Göre Ara (Örn: 15-07 veya 1.1)");
                Console.WriteLine("3. İsme Göre Ara (Örn: Ramazan)");
                Console.WriteLine("4. Tüm Listeyi Göster");
                Console.WriteLine("5. Çıkış");
                Console.Write("Seçiminiz: ");

                // Kullanıcıdan seçim alma (Boş gelirse boş string atıyoruz)
                string secim = Console.ReadLine() ?? "";

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
            }
        }

        // API'den verileri çeken asenkron metodumuz
        private static async Task LoadHolidaysAsync()
        {
            int[] years = { 2023, 2024, 2025 };
            string baseUrl = "https://date.nager.at/api/v3/PublicHolidays/";

            // JSON ayarları: Büyük/Küçük harf duyarlılığını kaldırıyoruz (Örn: "Date" gelse de "date" okuyo)
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            foreach (var year in years)
            {
                try
                {
                    // URL oluşturmak için: .../PublicHolidays/2024/TR
                    string url = $"{baseUrl}{year}/TR";

                    // JSON verisini string olarak indirmek için
                    string jsonResponse = await client.GetStringAsync(url);

                    // JSON string'ini C# List<Holiday> nesnesine çevirmek için (Deserialize)
                    var yearHolidays = JsonSerializer.Deserialize<List<Holiday>>(jsonResponse, options);

                    // Listeye ekleme
                    if (yearHolidays != null)
                    {
                        allHolidays.AddRange(yearHolidays);
                    }
                }
                catch (Exception ex)
                {
                    // İnternet hatası veya API hatası olursa konsola yazdırcaz
                    Console.WriteLine($"{year} yılı verisi çekilemedi: {ex.Message}");
                }
            }
        }

        // Yıla göre filtreleme
        private static void YearSelectionMenu()
        {
            Console.Write("Listelemek istediğiniz yılı girin: ");
            string inputYear = Console.ReadLine() ?? "";

            // LINQ Sorgusu: Tarihi girilen yılla başlayanları getiririz
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

        // Tarih Arama
        private static void SearchByDate()
        {
            Console.WriteLine("\n--- Tarihe Göre Arama ---");
            Console.Write("Tarih girin (Gün ve Ay): ");
            string input = Console.ReadLine() ?? "";

            // Kullanıcının girdiği ayraçları (nokta, slaş, boşluk) tire (-) ile değiştiriyoruz
            string cleanInput = input.Replace(".", "-").Replace("/", "-").Replace(" ", "-");

            // Metni parçalara ayırıyoruz (Gün ve Ay)
            string[] parts = cleanInput.Split('-');

            // En az iki parça var mı? (Gün ve Ay)
            if (parts.Length >= 2)
            {
                // String'i sayıya çeviriyoruz (Böylece "07" ile "7" aynı sayılacak)
                if (int.TryParse(parts[0], out int searchDay) && int.TryParse(parts[1], out int searchMonth))
                {
                    // Veritabanında (Listede) arama yapıyoruz
                    var foundHolidays = allHolidays.Where(h =>
                    {
                        if (string.IsNullOrEmpty(h.date)) return false;

                        // API'den gelen tarihi (yyyy-MM-dd) standarta çevirip gün/ay kontrolü yapıyoruz
                        if (DateTime.TryParseExact(h.date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dt))
                        {
                            // Yıl farketmeksizin GÜN ve AY eşleşiyor mu
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

        // İsme göre arama (Büyük/Küçük harf duyarsız)
        private static void SearchByName()
        {
            Console.Write("Tatil adı girin (Örn: Cumhuriyet): ");
            // Girilen metni küçük harfe çeviriyoruz
            string keyword = (Console.ReadLine() ?? "").ToLower();

            // LINQ Sorgusu: Hem Türkçe isme (localName) hem İngilizce ismine (name) bak
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

        // Tüm listeyi dökme
        private static void ListAllHolidays()
        {
            Console.WriteLine("\n--- 2023-2025 Tüm Resmi Tatiller ---");
            PrintTable(allHolidays);
        }

        // Listeyi tablo şeklinde ekrana basarıyoruz
        private static void PrintTable(List<Holiday> holidays)
        {
            // Tablo başlıkları (Sola yaslı hizalama)
            Console.WriteLine("{0,-12} {1,-40} {2,-30}", "TARİH", "YEREL İSİM", "ULUSLARARASI İSİM");
            Console.WriteLine(new string('-', 90));

            foreach (var h in holidays)
            {
                // Null kontrolü: Eğer veri yoksa "-" yazıyoruz
                string d = h.date ?? "-";
                string ln = h.localName ?? "-";
                string n = h.name ?? "-";

                Console.WriteLine("{0,-12} {1,-40} {2,-30}", d, ln, n);
            }
            Console.WriteLine($"\nToplam {holidays.Count} kayıt listelendi.");
        }
    }
}