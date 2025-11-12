// ViewModels/Randevu/RandevuCreateVM.cs
using System.ComponentModel.DataAnnotations;

namespace dortageDB.ViewModels
{
    public class RandevuCreateVM
    {
        // Mevcut müşteri seçimi
        public int? MusteriId { get; set; }

        // YENİ MÜŞTERİ BİLGİLERİ (opsiyonel)
        public bool YeniMusteri { get; set; } // Checkbox için
        [StringLength(100)] public string? YeniMusteriAd { get; set; }
        [StringLength(100)] public string? YeniMusteriSoyad { get; set; }
        [StringLength(15)] public string? YeniMusteriTelefon { get; set; }
        public bool? YeniMusteriCinsiyet { get; set; }
        [StringLength(11)] public string? YeniMusteriTcNo { get; set; }

        // Randevu bilgileri
        [StringLength(500)] public string? Aciklama { get; set; }
        [Required] public DateTime RandevuZaman { get; set; }
        [Required, StringLength(50)] public string RandevuTipi { get; set; } = null!;
        public int? VisionerID { get; set; }
    }
}