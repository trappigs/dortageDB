namespace dortageDB.Entities
{
    public class Referral
    {
        public int Id { get; set; }

        // "ABC123" gibi. Büyük harf normalize et.
        public string Code { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;
        public int? MaxUses { get; set; } = 1;          // null = sınırsız
        public int UsedCount { get; set; } = 0;
        public DateTime? ExpiresAt { get; set; }

        // İstersen: Kodu kim üretti
        public string? CreatedByUserId { get; set; }
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
