namespace dortageDB.Services
{
    public interface IReferralService
    {
        /// <summary>Referans kodunu doğrular ve geçerliyse kullanımını tüketir.</summary>
        Task<(bool ok, string? error)> ValidateAndConsumeAsync(string rawCode);
    }
}
