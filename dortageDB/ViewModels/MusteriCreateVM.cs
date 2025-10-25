// ViewModels/Musteri/MusteriCreateVM.cs
using System.ComponentModel.DataAnnotations;

namespace dortageDB.ViewModels
{
    public class MusteriCreateVM
    {
        [StringLength(11)] public string? TcNo { get; set; }
        [Required, StringLength(100)] public string Ad { get; set; } = null!;
        [Required, StringLength(100)] public string Soyad { get; set; } = null!;
        [Required, StringLength(15)] public string Telefon { get; set; } = null!;
        public bool? Cinsiyet { get; set; }
    }
}