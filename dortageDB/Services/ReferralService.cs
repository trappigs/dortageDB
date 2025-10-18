using dortageDB.Data;
using Microsoft.EntityFrameworkCore;

namespace dortageDB.Services;

public class ReferralService : IReferralService
{
    private readonly AppDbContext _db;
    public ReferralService(AppDbContext db) => _db = db;

    public async Task<(bool ok, string? error)> ValidateAndConsumeAsync(string rawCode)
    {
        if (string.IsNullOrWhiteSpace(rawCode))
            return (false, "Geçersiz referans kodu.");

        var code = rawCode.Trim().ToUpperInvariant();

        var r = await _db.Referrals.SingleOrDefaultAsync(x => x.Code == code);
        if (r is null || !r.IsActive) return (false, "Geçersiz referans kodu.");
        if (r.ExpiresAt is { } dt && dt < DateTime.UtcNow) return (false, "Geçersiz referans kodu.");
        if (r.MaxUses is { } m && r.UsedCount >= m) return (false, "Geçersiz referans kodu.");

        r.UsedCount += 1;
        if (r.MaxUses is { } mm && r.UsedCount >= mm) r.IsActive = false;

        await _db.SaveChangesAsync();
        return (true, null);
    }
}