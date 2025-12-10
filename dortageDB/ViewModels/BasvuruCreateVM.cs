using System.ComponentModel.DataAnnotations;

namespace dortageDB.ViewModels
{
    public class BasvuruCreateVM
    {
        [Required(ErrorMessage = "Ad Soyad zorunludur.")]
        [StringLength(100)]
        public string AdSoyad { get; set; }

        [Required(ErrorMessage = "Telefon zorunludur.")]
        [StringLength(20)]
        public string Telefon { get; set; }

        [Required(ErrorMessage = "E-posta zorunludur.")]
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz.")]
        [StringLength(100)]
        public string Email { get; set; }

        [Required(ErrorMessage = "İl seçimi zorunludur.")]
        public string Il { get; set; }

        [Required(ErrorMessage = "İlçe seçimi zorunludur.")]
        public string Ilce { get; set; }

        [Required(ErrorMessage = "Meslek bilgisi zorunludur.")]
        [StringLength(100)]
        public string Meslek { get; set; }

        [Required(ErrorMessage = "Eğitim durumu zorunludur.")]
        public string EgitimDurumu { get; set; }

        [Required(ErrorMessage = "Tecrübe bilgisi zorunludur.")]
        public string GayrimenkulTecrubesi { get; set; }

        [Required(ErrorMessage = "Bu alan zorunludur.")]
        [StringLength(100)]
        public string NeredenDuydunuz { get; set; }

        [Required(ErrorMessage = "Kendinizi tanıtmanız zorunludur.")]
        public string KendiniziTanitin { get; set; }

        public string? Beklentiniz { get; set; }

        public IFormFile? CvDosyasi { get; set; }

        public string? SosyalMedyaLink { get; set; }

        [Range(typeof(bool), "true", "true", ErrorMessage = "KVKK metnini onaylamanız gerekmektedir.")]
        public bool KvkkOnay { get; set; }
    }
}
