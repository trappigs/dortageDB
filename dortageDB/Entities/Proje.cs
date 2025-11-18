namespace dortageDB.Entities;

public class Proje
{
    public int ProjeID { get; set; }

    public required string ProjeAdi { get; set; }

    public string? Slug { get; set; } // SEO-friendly URL

    public required string Aciklama { get; set; }

    public string? KisaAciklama { get; set; } // Kart görünümünde gösterilecek kısa açıklama

    public required string Konum { get; set; }

    public string? Sehir { get; set; }

    public string? Ilce { get; set; }

    public string YatirimTuru { get; set; } = "Arsa"; // "Arsa" veya "Konut"

    public decimal MinFiyat { get; set; }

    public decimal MaxFiyat { get; set; }

    public int ToplamParsel { get; set; }

    public int SatilanParsel { get; set; }

    public string? KapakGorseli { get; set; }

    public string? GaleriGorselleri { get; set; } // JSON array olarak saklanacak

    public string? KapakVideosu { get; set; } // Galerinin ilk öğesi olarak gösterilecek video URL (YouTube, Vimeo vb.)

    public string? Tour360Url { get; set; } // 360 derece sanal tur iframe URL'i veya turlar klasöründeki proje adı

    public string? SunumDosyaUrl { get; set; } // Proje sunumu (PDF, PPT vb.)

    public bool Imarlimi { get; set; } = true;

    public bool MustakilTapu { get; set; } = true;

    public bool TaksitImkani { get; set; } = true;

    public bool TakasImkani { get; set; } = false;

    public string? Altyapi { get; set; } // Elektrik, Su, Kanalizasyon vb.

    public int? MetreKare { get; set; } // Eski alan - geriye dönük uyumluluk için
    public int? MinMetreKare { get; set; } // Minimum metrekare
    public int? MaxMetreKare { get; set; } // Maximum metrekare

    public string? KrediyeUygunluk { get; set; }

    public string? OzelliklerJson { get; set; } // Diğer özellikler için JSON

    public DateTime KayitTarihi { get; set; } = DateTime.Now;

    public DateTime? GuncellemeTarihi { get; set; }

    public bool AktifMi { get; set; } = true;

    public int Oncelik { get; set; } = 0; // Sıralama için

    public bool YeniBadge { get; set; } = false; // YENİ badge gösterimi için

    // Yakınlık Bilgileri
    public string? YakinProjeler { get; set; } // Yakında gerçekleşecek projeler
    public string? YakinBolgeler { get; set; } // Yakınındaki önemli bölgeler (siteler, OSB, otoyol vs.)
    public string? UlasimBilgileri { get; set; } // Ulaşım bilgileri
    public string? SosyalTesisler { get; set; } // Yakınındaki sosyal tesisler (hastane, okul, AVM vs.)

    // Navigation Properties
    public ICollection<Satis>? Satislar { get; set; }
    public ICollection<Randevu>? Randevular { get; set; }
}
