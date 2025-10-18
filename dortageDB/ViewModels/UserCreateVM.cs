using System.ComponentModel.DataAnnotations;

namespace dortageDB.ViewModels
{
    public class UserCreateVM
    {
        [Required, StringLength(100)] public string Ad { get; set; } = null!;
        [Required, StringLength(100)] public string Soyad { get; set; } = null!;
        [Required, StringLength(15)] public string Telefon { get; set; } = null!;
        [Required, EmailAddress, StringLength(200)] public string Eposta { get; set; } = null!;
        [Required, StringLength(100)] public string Sehir { get; set; } = null!;
        public bool Cinsiyet { get; set; }
        public bool Kvkk { get; set; }
        public bool Pazarlama { get; set; }
        [Required, StringLength(255)] public string Sifre { get; set; } = null!;
        public bool TopraktarMi { get; set; } = true;
        public IList<string> Roller { get; set; } = new List<string>(); // "admin","topraktar" vb.
    }
}
