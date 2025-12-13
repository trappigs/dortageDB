// ViewModels/Randevu/RandevuCreateVM.cs
using System.ComponentModel.DataAnnotations;

namespace dortageDB.ViewModels
{
    public class RandevuCreateVM
    {
        // Mevcut m��teri se�imi
        public int? MusteriId { get; set; }

        // YEN� M��TER� B�LG�LER� (opsiyonel)
        public bool YeniMusteri { get; set; } // Checkbox için
        [StringLength(100)] public string? YeniMusteriAd { get; set; }
        [StringLength(100)] public string? YeniMusteriSoyad { get; set; }
        [StringLength(15)] public string? YeniMusteriTelefon { get; set; }
        public bool? YeniMusteriCinsiyet { get; set; }
        [StringLength(11)] public string? YeniMusteriTcNo { get; set; }

        // Randevu bilgileri
        [StringLength(500)] public string? Aciklama { get; set; }

        // Tarih ve saat ayrı alanlar (form'dan gelecek)
        [Required(ErrorMessage = "Randevu tarihi zorunludur")]
        public string? RandevuTarih { get; set; }

        [Required(ErrorMessage = "Randevu saati zorunludur")]
        public string? RandevuSaat { get; set; }

        // Birleştirilmiş değer (controller'da doldurulacak)
        public DateTime? RandevuZaman { get; set; }

        [Required, StringLength(50)] public string RandevuTipi { get; set; } = null!;
        public int? VekarerID { get; set; }
    }
}
