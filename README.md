# PublicHolidayTracker - Türkiye Resmi Tatil Takip Sistemi 🇹🇷

Bu proje, **Görsel Programlama** dersi kapsamında verilen dönem ödevi olarak geliştirilmiştir. Uygulama, belirtilen API servislerini kullanarak 2023, 2024 ve 2025 yıllarına ait Türkiye resmi tatil verilerini çeken, işleyen ve kullanıcıya konsol arayüzü üzerinden filtreleme imkanı sunan bir C# Konsol Uygulamasıdır.

## Projenin Amacı ve Senaryo

Projenin temel amacı, modern C# tekniklerini kullanarak dış bir kaynaktan (API) veri çekmek, bu veriyi Nesne Yönelimli Programlama (OOP) prensiplerine uygun olarak modellemek ve kullanıcı etkileşimli bir arayüz sunmaktır.

Senaryo gereği uygulama:
1.  **Nager.Date API** üzerinden JSON formatında veri çeker.
2.  Verileri deserialize ederek bellekteki nesnelere dönüştürür.
3.  Kullanıcının yıl, tarih veya isim bazlı arama yapmasına olanak tanır.

## Kullanılan Teknolojiler ve Kütüphaneler

* **Geliştirme Ortamı (IDE):** Visual Studio Community 2026
* **Dil:** C# (.NET Core / .NET 6+)
* **Veri İletişimi:** `System.Net.Http.HttpClient` (Asenkron veri çekme işlemi için)
* **Veri İşleme:** `System.Text.Json` (JSON Deserialization için)
* **Sorgulama:** LINQ (Language Integrated Query - Veri filtreleme için)
* **Veri Yapıları:** Generic Collections (`List<T>`)

---

## Teknik Detaylar ve Çözülen Problemler

Proje geliştirme sürecinde karşılaşılan teknik zorluklar ve uygulanan çözümler aşağıda detaylandırılmıştır:

### 1. `fixed` Anahtar Kelimesi Çakışması 
API'den gelen JSON verisinde `fixed` isminde bir boolean alan bulunmaktadır. Ancak `fixed` kelimesi C# dilinde (pointer işlemleri için) rezerve edilmiş özel bir anahtar kelimedir (keyword).
* **Çözüm:** Değişken ismi `public bool @fixed { get; set; }` şeklinde tanımlanarak C# derleyicisine bunun bir değişken olduğu (`verbatim identifier`) belirtilmiş ve model yapısı bozulmadan API uyumluluğu sağlanmıştır.

### 2. Asenkron Veri Çekme (Async/Await) 
Ağ işlemleri programın ana akışını bloklayabileceği için `HttpClient` istekleri senkron (beklemeli) değil, **asenkron** (`async/await`) yapıda kurgulanmıştır. Bu sayede veri çekilirken uygulamanın donması engellenmiştir.

### 3. Akıllı Tarih Arama Algoritması 
Kullanıcıların tarih girerken farklı formatlar (Örn: `15.07`, `15/07`, `15-7`, `15 07`) kullanabileceği öngörülmüştür.
* **Çözüm:** Girilen input önce temizlenmekte (tüm ayıraçlar `-` işaretine çevrilmekte), ardından `Split` edilerek gün ve ay sayısal değerlere (`int`) dönüştürülmektedir. Bu sayede "07" ile "7" arasındaki string farkı ortadan kaldırılarak %100 doğru eşleşme sağlanmıştır.

### 4. JSON Case Insensitive Ayarı 
API'den gelen verilerin özellik isimlerinin büyük/küçük harf değişkenliği gösterebileceği (Örn: `Date` veya `date`) riskine karşı:
```csharp
var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
````

ayarı kullanılarak veri kaybı önlenmiştir.

-----

## Sınıf Yapısı (Class Structure)

Oluşturulan `Holiday` sınıfı aşağıdaki gibidir:

```csharp
public class Holiday
{
    public string? date { get; set; }        // Tatil Tarihi
    public string? localName { get; set; }   // Yerel Ad (Türkçe)
    public string? name { get; set; }        // Uluslararası Ad
    public string? countryCode { get; set; } // Ülke Kodu
    public bool @fixed { get; set; }         // Sabit/Değişken Tatil Durumu
    public bool global { get; set; }         // Ulusal Tatil Durumu
}
```

*(Not: Nullable types (`?`) kullanılarak API'den boş gelebilecek değerlere karşı "Null Reference Exception" hatası önlenmiştir.)*

-----

## Uygulama Menüsü

Uygulama çalıştırıldığında kullanıcıyı aşağıdaki gibi bir menü karşılamaktadır:

```text
===== PublicHolidayTracker =====
1. Tatil listesini göster (Yıl Seçmeli - 2023/24/25)
2. Tarihe göre tatil ara (Akıllı Arama: gg-aa)
3. İsme göre tatil ara (Örn: Cumhuriyet)
4. Tüm tatilleri 3 yıl boyunca göster (2023–2025)
5. Çıkış
```

## Kurulum ve Çalıştırma

1.  Projeyi klonlayın veya zip olarak indirin.
2.  **Visual Studio Community 2026** ile `PublicHolidayTracker.sln` dosyasını açın.
3.  İnternet bağlantınızın aktif olduğundan emin olun (Veriler anlık çekilmektedir).
4.  `F5` tuşuna basarak veya "Start" butonuna tıklayarak uygulamayı derleyip çalıştırın.

-----

**Geliştirici Notu:** Bu proje, API tabanlı veri işleme mantığını kavramak ve C\# konsol uygulamalarında kullanıcı deneyimini (UX) iyileştirmek amacıyla hazırlanmıştır. Kod içerisindeki tüm metodlar modüler yapıda olup, geliştirilmeye açıktır.

```
```
