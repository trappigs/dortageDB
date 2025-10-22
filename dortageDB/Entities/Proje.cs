namespace dortageDB.Entities;

public class Proje
{
    public int ProjeID { get; set; }

    public required string ProjeAdi { get; set; }

    public required string Aciklama { get; set; }

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

    public bool Imarlimi { get; set; } = true;

    public bool MustakilTapu { get; set; } = true;

    public bool TaksitImkani { get; set; } = true;

    public bool TakasImkani { get; set; } = false;

    public string? Altyapi { get; set; } // Elektrik, Su, Kanalizasyon vb.

    public int? MetreKare { get; set; }

    public string? KrediyeUygunluk { get; set; }

    public string? OzelliklerJson { get; set; } // Diğer özellikler için JSON

    public DateTime KayitTarihi { get; set; } = DateTime.Now;

    public DateTime? GuncellemeTarihi { get; set; }

    public bool AktifMi { get; set; } = true;

    public int Oncelik { get; set; } = 0; // Sıralama için

    // Navigation Properties
    public ICollection<Satis>? Satislar { get; set; }
    public ICollection<Randevu>? Randevular { get; set; }
}
