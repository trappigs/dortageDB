using System.ComponentModel.DataAnnotations;

namespace dortageDB.Entities
{

    public enum RandevuDurum { pending, confirmed, completed, no_show, cancelled }

    public class Randevu
    {
        public int RandevuID { get; set; }

        // Müşteri İlişkisi
        public int MusteriId { get; set; }
        public Musteri Musteri { get; set; } = null!;

        // Topraktar İlişkisi
        [Required]
        public int TopraktarID { get; set; }
        public AppUser Topraktar { get; set; } = null!;

        // Randevu Bilgileri
        [Required, StringLength(100)]
        public string Bolge { get; set; } = null!; // Şehir/İl bilgisi

        [StringLength(500)]
        public string? Aciklama { get; set; } // Adres detayı veya not

        [Required]
        public DateTime RandevuZaman { get; set; }

        [Required, StringLength(50)]
        public string RandevuTipi { get; set; } = null!;

        public RandevuDurum RandevuDurum { get; set; } = RandevuDurum.pending;

        public DateTime OlusturulmaTarihi { get; set; } = DateTime.Now;
    }
}
