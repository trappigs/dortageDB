using System.ComponentModel.DataAnnotations;

namespace dortageDB.Entities
{
    public class Referral
    {
        public int Id { get; set; }

        [Required, StringLength(32)]
        public string Code { get; set; } = string.Empty; // "ABC123" - Büyük harf

        public bool IsActive { get; set; } = true;

        public int? MaxUses { get; set; } = 1; // null = sınırsız kullanım

        public int UsedCount { get; set; } = 0;

        public DateTime? ExpiresAt { get; set; }

        public int? CreatedByUserId { get; set; } // Referral kodunu oluşturan user

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        public DateTime? LastUsedAt { get; set; }
    }
}
