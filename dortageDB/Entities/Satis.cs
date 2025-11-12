using System.ComponentModel.DataAnnotations;

namespace dortageDB.Entities
{
    public class Satis
    {
        public int SatisID { get; set; }

        // Müşteri İlişkisi
        public int SatilanMusteriID { get; set; }
        public Musteri Musteri { get; set; } = null!;

        // Visioner İlişkisi
        [Required]
        public int VisionerID { get; set; }
        public AppUser Visioner { get; set; } = null!;

        // Proje İlişkisi (Opsiyonel)
        public int? ProjeID { get; set; }
        public Proje? Proje { get; set; }

        // Satış Bilgileri
        [Required]
        public DateTime SatilmaTarihi { get; set; }

        [Required]
        [Range(0.01, 9999999999.99)]
        public decimal ToplamSatisFiyati { get; set; } // DECIMAL(14,2)

        [Required, StringLength(500)]
        public string Bolge { get; set; } = null!;

        public bool Taksit { get; set; } // false=Peşin, true=Taksitli

        [Required]
        [Range(0, 9999999999.99)]
        public decimal OdenecekKomisyon { get; set; } // DECIMAL(14,2)

        public DateTime OlusturulmaTarihi { get; set; } = DateTime.Now;
    }
}
