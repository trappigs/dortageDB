// ViewModels/Satis/SatisCreateVM.cs
using System.ComponentModel.DataAnnotations;

namespace dortageDB.ViewModels
{
    public class SatisCreateVM
    {
        [Required] public int SatilanMusteriID { get; set; }
        [Required] public int VisionerID { get; set; }
        [Required, DataType(DataType.Date)] public DateTime SatilmaTarihi { get; set; }
        [Required, Range(0, 999999999999.99)] public decimal ToplamSatisFiyati { get; set; }
        [Required, StringLength(500)] public string Bolge { get; set; } = null!;
        public bool Taksit { get; set; }
    }
}