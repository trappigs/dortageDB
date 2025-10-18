// ViewModels/Auth/RegisterVM.cs
using System.ComponentModel.DataAnnotations;
namespace dortageDB.ViewModels
{
    public class RegisterVM
    {
        [Required, StringLength(100)] public string Ad { get; set; } = null!;
        [Required, StringLength(100)] public string Soyad { get; set; } = null!;
        [Required, Phone, StringLength(15)] public string PhoneNumber { get; set; } = null!;
        [Required, EmailAddress, StringLength(200)] public string Email { get; set; } = null!;
        [Required, StringLength(100)] public string Sehir { get; set; } = null!;
        public bool Cinsiyet { get; set; }
        public bool Kvkk { get; set; }
        public bool Pazarlama { get; set; }
        [StringLength(11)] public string? TcNo { get; set; }

        [Required, DataType(DataType.Password)] public string Password { get; set; } = null!;
        [Required, DataType(DataType.Password), Compare(nameof(Password))] public string ConfirmPassword { get; set; } = null!;

        public bool TopraktarMi { get; set; } = true;
        // RegisterVM.cs
        [StringLength(32)]
        public string? Code { get; set; }
        public IList<string> Roller { get; set; } = new List<string>(); // örn: "admin","topraktar"
    }
}