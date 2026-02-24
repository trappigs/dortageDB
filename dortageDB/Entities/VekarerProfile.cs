using System.ComponentModel.DataAnnotations;

namespace dortageDB.Entities
{
    public class VekarerProfile
    {
        [Key] public int UserId { get; set; }
        public AppUser User { get; set; } = null!;

        [StringLength(34)] public string? IBAN { get; set; }
        public decimal TotalCommission { get; set; } = 0;
        public int TotalSales { get; set; } = 0;
    }
}
