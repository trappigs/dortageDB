using System.ComponentModel.DataAnnotations;

namespace dortageDB.Entities
{
    public class Satis
    {
        public int SatisID { get; set; }

        public int SatilanMusteriID { get; set; }
        public Musteri Musteri { get; set; } = null!;

        [Required] public DateTime SatilmaTarihi { get; set; }
        [Required] public decimal ToplamSatisFiyati { get; set; }   // DECIMAL(14,2)
        [Required, StringLength(500)] public string Bolge { get; set; } = null!;
        public bool Taksit { get; set; }
        [Required] public decimal OdenecekKomisyon { get; set; }

        public int TopraktarID { get; set; }
        public AppUser Topraktar { get; set; } = null!;
    }
}
