using System.ComponentModel.DataAnnotations;

namespace dortageDB.Entities
{

    public enum RandevuDurum
    {
        OnayBekliyor,           // 1- Onay Bekliyor
        GorusmeBekleniyor,      // 2- Görüşme Bekleniyor
        KararBekleniyor,        // 3- Görüşüldü - Karar Bekleniyor
        Olumsuz,                // 4- Görüşüldü - Olumsuz
        KaporaAlindi,           // 5- Görüşüldü - Kapora Alındı
        OdemeAlindi,            // 6- Görüşüldü - Ödeme Alındı
        Gerceklesmedi,          // 7- Gerçekleşmedi
        Iptal                   // 8- İptal
    }

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

        // Proje İlişkisi (Opsiyonel)
        public int? ProjeID { get; set; }
        public Proje? Proje { get; set; }

        // Randevu Bilgileri
        [StringLength(500)]
        public string? Aciklama { get; set; } // Adres detayı veya not

        [Required]
        public DateTime RandevuZaman { get; set; }

        [Required, StringLength(50)]
        public string RandevuTipi { get; set; } = null!;

        public RandevuDurum RandevuDurum { get; set; } = RandevuDurum.OnayBekliyor;

        public DateTime OlusturulmaTarihi { get; set; } = DateTime.Now;
    }
}
