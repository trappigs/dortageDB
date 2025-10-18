// ViewModels/Randevu/RandevuCreateVM.cs
using System.ComponentModel.DataAnnotations;

namespace dortageDB.ViewModels
{
    public class RandevuCreateVM
    {
        [Required] public int MusteriId { get; set; }
        [Required, StringLength(500)] public string Bolge { get; set; } = null!;
        [Required] public DateTime RandevuZaman { get; set; }
        [Required, StringLength(50)] public string RandevuTipi { get; set; } = null!;
        [Required] public int TopraktarID { get; set; }
    }
}