# Vekarer - Network Arsa Satış Platformu (DortageDB) Teknik Dokümantasyon

## 1. Proje Özeti
**Vekarer**, paylaşımlı network satış modelini kullanan bir gayrimenkul (arsa) satış platformudur. Sistem, "Vekarer" olarak adlandırılan serbest satış temsilcilerinin, sisteme müşteri getirerek satış üzerinden komisyon kazanması prensibine dayanır. Proje, hem genel kullanıcılara (Landing Page) hem de yetkili kullanıcılara (Admin/Vekarer Paneli) hitap eden modüller içerir.

## 2. Teknoloji Yığını (Tech Stack)
*   **Framework:** .NET 9.0 (ASP.NET Core MVC)
*   **Veritabanı:** Microsoft SQL Server
*   **ORM:** Entity Framework Core
*   **Kimlik Doğrulama:** ASP.NET Core Identity
*   **Önyüz:** HTML5, CSS3 (Custom + Bootstrap 5), JavaScript (jQuery)
*   **E-posta Servisi:** MailKit (SMTP)
*   **Versiyon Kontrol:** Git

## 3. Kurulum ve Başlangıç

### Gereksinimler
*   .NET 9.0 SDK
*   SQL Server (LocalDB veya Production)

### Kurulum Adımları
1.  `appsettings.json` dosyasındaki `ConnectionStrings:Default` alanını kendi veritabanı sunucunuza göre düzenleyin.
2.  Mail gönderimi için `MailSettings` alanını yapılandırın.
3.  Terminali açın ve veritabanını oluşturun:
    ```bash
    dotnet ef database update
    ```
4.  Projeyi çalıştırın:
    ```bash
    dotnet run
    ```

## 4. Proje Mimarisi ve Klasör Yapısı

*   **Controllers/**: İş mantığının yönetildiği denetleyiciler.
    *   `AccountController`: Giriş, kayıt, şifre sıfırlama işlemleri.
    *   `RandevuController`: Randevu oluşturma, listeleme ve durum yönetimi.
    *   `ProjeController`: Arsa projelerinin listelenmesi ve detayları.
    *   `BasvuruController`: Referans kodu olmayan kullanıcıların başvuruları.
*   **Entities/**: Veritabanı tablolarına karşılık gelen sınıflar (`AppUser`, `Proje`, `Randevu`, `Musteri` vb.).
*   **Services/**: Yardımcı servisler (`EmailService`, `ReferralService`, `SeoService`).
*   **Views/**: Kullanıcı arayüzü dosyaları (.cshtml).
*   **wwwroot/**: Statik dosyalar (CSS, JS, Resimler, Uploads).

## 5. Temel Modüller ve İş Kuralları

### 5.1. Kimlik Doğrulama ve Yetkilendirme
*   **Roller:**
    *   `Admin`: Tüm sisteme tam erişim.
    *   `Vekarer`: Müşteri ekleyebilir, randevu oluşturabilir, kendi satışlarını görebilir.
*   **Kayıt Ol (`/kayit`):**
    *   Kullanıcılar sadece geçerli bir **Referans Kodu** ile kayıt olabilir.
    *   Referans kodu olmayanlar için "Başvuru Formu" (Modal) bulunur.
*   **Giriş Yap:** Standart e-posta/şifre girişi.

### 5.2. Randevu Yönetimi (`RandevuController`)
*   **Oluşturma:** Vekarer, sistemi kullanarak potansiyel yatırımcılar (müşteriler) için randevu oluşturur.
*   **Bildirim:** Randevu oluşturulduğunda `info@dortage.com` adresine otomatik e-posta gönderilir.
*   **Kısıtlama (2 Saat Kuralı):**
    *   Vekarerler, randevu saatine **2 saatten az kaldığında** veya randevu saati geçtiğinde randevuyu **güncelleyemez**.
    *   Admin kullanıcıları bu kısıtlamadan muaftır.
*   **Durum Yönetimi:** Randevu durumu (Onaylandı, İptal, Tamamlandı vb.) Admin tarafından değiştirildiğinde, ilgili Vekarer'e otomatik bilgilendirme e-postası gider.

### 5.3. Başvuru ve Referans Sistemi (`BasvuruController`)
*   Referans kodu olmayan kullanıcılar başvuru formu doldurur.
*   Formda CV (PDF) yüklenebilir.
*   Başvuru yapıldığında:
    1.  Kullanıcıya "Başvurunuz alındı" e-postası gider.
    2.  Yöneticiye (`info@dortage.com`) başvuru detayları ve **ekli CV dosyası** ile birlikte bildirim e-postası gider.

### 5.4. Projeler ve Detay Sayfası
*   Projeler veritabanından dinamik olarak listelenir.
*   **Detay Sayfası Özellikleri:**
    *   Fotoğraf Galerisi (16:9 sabit oran).
    *   360 Derece Sanal Tur entegrasyonu.
    *   Konum ve yakınlık bilgileri.
    *   "Randevu Al" butonu (Giriş yapmamış kullanıcıyı önce login'e, sonra randevuya yönlendirir).

## 6. Önemli Yapılandırmalar

### Route Yapılandırması (`Program.cs`)
Varsayılan MVC rotalarına ek olarak özel rotalar tanımlanmıştır:
```csharp
// /kayit adresi Account/Register'a gider
app.MapControllerRoute(
    name: "kayit",
    pattern: "kayit",
    defaults: new { controller = "Account", action = "Register" });

// Proje slug'ları için dinamik rota (en sonda yer alır)
app.MapControllerRoute(
    name: "proje-slug",
    pattern: "{slug}",
    defaults: new { controller = "Proje", action = "Details" });
```

### E-posta Servisi (`EmailService`)
*   `SendEmailAsync` metodu dosya eki (attachment) destekleyecek şekilde güncellenmiştir.
*   HTML formatında UTF-8 kodlamasıyla e-posta gönderir.

## 7. Frontend Notları
*   **Responsive:** Tüm sayfalar mobil uyumludur (`@media` sorguları ile özelleştirilmiştir).
*   **Mobil Hero Bölümü:** Anasayfa giriş bölümü mobilde tam ekran (100vh) görünecek şekilde ayarlanmıştır.
*   **Yüzen Buton:** Sağ alt köşede animasyonlu "Randevu Al" butonu tüm sayfalarda mevcuttur.
*   **SEO:** Link yapılarında gereksiz query string'lerden kaçınılmış, giriş gerektiren linklere `rel="nofollow"` eklenmiştir.

## 8. İletişim & Destek
Proje ile ilgili teknik sorunlar için geliştirici ekibiyle iletişime geçiniz.
