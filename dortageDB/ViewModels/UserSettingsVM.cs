// dortageDB/ViewModels/UserSettingsVM.cs
using System.ComponentModel.DataAnnotations;

namespace dortageDB.ViewModels
{
    public class UserSettingsVM
    {
        [Required, StringLength(100)]
        public string Ad { get; set; } = null!;

        [Required, StringLength(100)]
        public string Soyad { get; set; } = null!;

        [Required, EmailAddress, StringLength(200)]
        public string Email { get; set; } = null!;

        [Required, Phone, StringLength(15)]
        public string PhoneNumber { get; set; } = null!;

        [Required, StringLength(100)]
        public string Sehir { get; set; } = null!;

        [StringLength(11)]
        public string? TcNo { get; set; }

        public bool Cinsiyet { get; set; }

        // Şifre değişikliği için
        [DataType(DataType.Password)]
        public string? CurrentPassword { get; set; }

        [DataType(DataType.Password)]
        [StringLength(100, MinimumLength = 6)]
        public string? NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Compare(nameof(NewPassword))]
        public string? ConfirmNewPassword { get; set; }
    }
}