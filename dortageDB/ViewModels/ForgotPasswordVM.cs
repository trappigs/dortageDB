using System.ComponentModel.DataAnnotations;

namespace dortageDB.ViewModels
{
    public class ForgotPasswordVM
    {
        [Required(ErrorMessage = "E-posta adresi gereklidir")]
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz")]
        [Display(Name = "E-posta")]
        public string Email { get; set; } = null!;
    }
}
