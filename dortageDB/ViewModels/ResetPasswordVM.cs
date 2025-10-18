using System.ComponentModel.DataAnnotations;

namespace dortageDB.ViewModels
{
    public class ResetPasswordVM
    {
        [Required]
        public string Token { get; set; } = null!;

        [Required(ErrorMessage = "E-posta adresi gereklidir")]
        [EmailAddress]
        public string Email { get; set; } = null!;

        [Required(ErrorMessage = "Yeni şifre gereklidir")]
        [StringLength(100, ErrorMessage = "{0} en az {2} karakter uzunluğunda olmalıdır.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Yeni Şifre")]
        public string NewPassword { get; set; } = null!;

        [Required(ErrorMessage = "Şifre tekrarı gereklidir")]
        [DataType(DataType.Password)]
        [Display(Name = "Yeni Şifre (Tekrar)")]
        [Compare("NewPassword", ErrorMessage = "Şifreler eşleşmiyor.")]
        public string ConfirmPassword { get; set; } = null!;
    }
}
