using System.ComponentModel.DataAnnotations;

namespace dortageDB.ViewModels;

public class IletisimVM
{
    [Required(ErrorMessage = "Ad Soyad zorunludur")]
    [StringLength(100, ErrorMessage = "Ad Soyad en fazla 100 karakter olabilir")]
    public string AdSoyad { get; set; } = string.Empty;

    [Required(ErrorMessage = "E-posta zorunludur")]
    [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Telefon numarası zorunludur")]
    [Phone(ErrorMessage = "Geçerli bir telefon numarası giriniz")]
    public string Telefon { get; set; } = string.Empty;

    [Required(ErrorMessage = "Konu zorunludur")]
    [StringLength(200, ErrorMessage = "Konu en fazla 200 karakter olabilir")]
    public string Konu { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mesaj zorunludur")]
    [StringLength(1000, MinimumLength = 10, ErrorMessage = "Mesaj 10-1000 karakter arasında olmalıdır")]
    public string Mesaj { get; set; } = string.Empty;
}
