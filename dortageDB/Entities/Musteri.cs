using System.ComponentModel.DataAnnotations;

namespace dortageDB.Entities
{
    public class Musteri
    {
        public int IdMusteri { get; set; }

        [StringLength(11)]
        public string? TcNo { get; set; }

        [Required, StringLength(100)]
        public string Ad { get; set; } = null!;

        [Required, StringLength(100)]
        public string Soyad { get; set; } = null!;

        [Required, StringLength(15)]
        public string Telefon { get; set; } = null!;

        [StringLength(200), EmailAddress]
        public string? Eposta { get; set; }

        [Required, StringLength(100)]
        public string Sehir { get; set; } = null!;

        public bool Cinsiyet { get; set; } // false=Kadın, true=Erkek

        public DateTime EklenmeTarihi { get; set; } = DateTime.Now;

        // Müşteriyi ekleyen topraktar
        [Required]
        public int TopraktarID { get; set; }

        // Navigation Properties
        public AppUser Topraktar { get; set; } = null!;
        public ICollection<Randevu> Randevular { get; set; } = new List<Randevu>();
        public ICollection<Satis> Satislar { get; set; } = new List<Satis>();
    }
}
