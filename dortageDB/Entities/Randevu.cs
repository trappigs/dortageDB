using System.ComponentModel.DataAnnotations;

namespace dortageDB.Entities
{

    public enum RandevuDurum { pending, confirmed, completed, no_show, cancelled }

    public class Randevu
    {
        public int RandevuID { get; set; }

        public int MusteriId { get; set; }
        public Musteri Musteri { get; set; } = null!;

        [Required, StringLength(500)] public string Bolge { get; set; } = null!;
        [Required] public DateTime RandevuZaman { get; set; }      // datetime2
        [Required, StringLength(50)] public string RandevuTipi { get; set; } = null!;

        public int TopraktarID { get; set; }
        public AppUser Topraktar { get; set; } = null!;

        public RandevuDurum RandevuDurum { get; set; } = RandevuDurum.pending;
    }
}
