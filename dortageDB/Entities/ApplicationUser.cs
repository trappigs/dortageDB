using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace dortageDB.Entities
{
    public class AppUser : IdentityUser<int>
    {
        [StringLength(11)] public string? TcNo { get; set; }
        [Required, StringLength(100)] public string Ad { get; set; } = null!;
        [Required, StringLength(100)] public string Soyad { get; set; } = null!;
        [Required, StringLength(100)] public string Sehir { get; set; } = null!;
        public bool Cinsiyet { get; set; }            // 0=kadın, 1=erkek
        public bool Kvkk { get; set; }
        public bool Pazarlama { get; set; }

        public TopraktarProfile? TopraktarProfile { get; set; }
        public ICollection<Randevu> Randevular { get; set; } = new List<Randevu>(); // topraktar olarak
        public ICollection<Satis> Satislar { get; set; } = new List<Satis>();       // topraktar olarak
    }
}
