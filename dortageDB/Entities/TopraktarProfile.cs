using System.ComponentModel.DataAnnotations;

namespace dortageDB.Entities
{
    public class TopraktarProfile
    {
        [Key] public int UserId { get; set; }
        public AppUser User { get; set; } = null!;
        // İleride: IBAN, ReferralCode, vb.
    }
}
