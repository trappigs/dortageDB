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

        public bool? Cinsiyet { get; set; } // false=Kadın, true=Erkek

        public DateTime EklenmeTarihi { get; set; } = DateTime.Now;

        // Müşteriyi ekleyen visioner
        [Required]
        public int VisionerID { get; set; }

        // Navigation Properties
        public AppUser Visioner { get; set; } = null!;
        public ICollection<Randevu> Randevular { get; set; } = new List<Randevu>();
        public ICollection<Satis> Satislar { get; set; } = new List<Satis>();
    }
}
