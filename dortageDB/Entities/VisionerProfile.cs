using System.ComponentModel.DataAnnotations;

namespace dortageDB.Entities
{
    public class VisionerProfile
    {
        [Key] public int UserId { get; set; }
        public AppUser User { get; set; } = null!;

        [StringLength(34)] public string? IBAN { get; set; }
        [StringLength(50)] public string? ReferralCode { get; set; }
        [StringLength(50)] public string? UsedReferralCode { get; set; } // Kayıt olurken kullandığı referans kodu
        public decimal TotalCommission { get; set; } = 0;
        public int TotalSales { get; set; } = 0;
    }
}
