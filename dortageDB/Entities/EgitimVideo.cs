using System.ComponentModel.DataAnnotations;

namespace dortageDB.Entities
{
    public class EgitimVideo
    {
        [Key]
        public int VideoID { get; set; }

        [Required(ErrorMessage = "Video başlığı gereklidir")]
        [StringLength(200)]
        public string Baslik { get; set; } = string.Empty;

        [Required(ErrorMessage = "Açıklama gereklidir")]
        [StringLength(500)]
        public string Aciklama { get; set; } = string.Empty;

        [Required(ErrorMessage = "YouTube video ID gereklidir")]
        [StringLength(50)]
        public string YoutubeVideoID { get; set; } = string.Empty;

        [Required(ErrorMessage = "Kategori seçimi gereklidir")]
        [StringLength(50)]
        public string Kategori { get; set; } = string.Empty;

        [Required(ErrorMessage = "Video süresi gereklidir")]
        [StringLength(10)]
        public string Sure { get; set; } = string.Empty; // Format: "12:30"

        public int IzlenmeSayisi { get; set; } = 0;

        public int BegeniSayisi { get; set; } = 0;

        public bool OneEikan { get; set; } = false;

        public bool Yeni { get; set; } = false;

        public bool Populer { get; set; } = false;

        public int Sira { get; set; } = 0; // Sıralama için

        public bool Aktif { get; set; } = true;

        public DateTime EklenmeTarihi { get; set; } = DateTime.Now;
    }

    // Kategori seçenekleri için enum
    public static class EgitimKategorileri
    {
        public const string Baslangic = "baslangic";
        public const string Satis = "satis";
        public const string Arsa = "arsa";
        public const string Musteri = "musteri";
        public const string Pazarlama = "pazarlama";
        public const string Basari = "basari";

        public static Dictionary<string, string> GetAll()
        {
            return new Dictionary<string, string>
            {
                { Baslangic, "🚀 Başlangıç Rehberi" },
                { Satis, "💼 Satış Teknikleri" },
                { Arsa, "🏞️ Arsa & Emlak Bilgisi" },
                { Musteri, "👥 Müşteri İlişkileri" },
                { Pazarlama, "📱 Dijital Pazarlama" },
                { Basari, "⭐ Başarı Hikayeleri" }
            };
        }
    }
}
