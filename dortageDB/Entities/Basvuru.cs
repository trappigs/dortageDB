using System;
using System.ComponentModel.DataAnnotations;

namespace dortageDB.Entities
{
    public class Basvuru
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string AdSoyad { get; set; }

        [Required]
        [StringLength(20)]
        public string Telefon { get; set; }

        [Required]
        [EmailAddress]
        [StringLength(100)]
        public string Email { get; set; }

        [Required]
        [StringLength(50)]
        public string Il { get; set; }

        [Required]
        [StringLength(50)]
        public string Ilce { get; set; }

        [Required]
        [StringLength(100)]
        public string Meslek { get; set; }

        [Required]
        [StringLength(50)]
        public string EgitimDurumu { get; set; }

        [Required]
        [StringLength(50)]
        public string GayrimenkulTecrubesi { get; set; }

        [Required]
        [StringLength(100)]
        public string NeredenDuydunuz { get; set; }

        [Required]
        public string KendiniziTanitin { get; set; }

        public string? Beklentiniz { get; set; }

        public string? CvDosyaYolu { get; set; }

        public string? SosyalMedyaLink { get; set; }

        public bool KvkkOnay { get; set; }

        public DateTime BasvuruTarihi { get; set; } = DateTime.Now;

        public BasvuruDurum Durum { get; set; } = BasvuruDurum.Bekliyor;
    }

    public enum BasvuruDurum
    {
        Bekliyor,
        Onaylandi,
        Reddedildi
    }
}
