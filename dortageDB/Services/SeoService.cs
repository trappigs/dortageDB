using dortageDB.Data;
using dortageDB.Entities;
using Microsoft.EntityFrameworkCore;

namespace dortageDB.Services
{
    public class SeoService : ISeoService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<SeoService> _logger;

        public SeoService(AppDbContext context, ILogger<SeoService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<SeoSetting?> GetSeoSettingAsync(string pagePath)
        {
            if (string.IsNullOrWhiteSpace(pagePath))
            {
                _logger.LogDebug("SEO: PagePath boş, null döndürülüyor");
                return null;
            }

            // Normalize path: remove trailing slash except for root, and convert to lowercase
            var normalizedPath = pagePath == "/" ? "/" : pagePath.TrimEnd('/').ToLowerInvariant();

            _logger.LogInformation($"SEO: Sayfa yolu aranıyor: '{pagePath}' -> normalize: '{normalizedPath}'");

            // Try case-insensitive match
            var setting = await _context.SeoSettings
                .Where(s => s.IsActive && s.PagePath.ToLower() == normalizedPath)
                .FirstOrDefaultAsync();

            if (setting != null)
            {
                _logger.LogInformation($"SEO: Ayar bulundu! PagePath: '{setting.PagePath}', Title: '{setting.PageTitle}'");
            }
            else
            {
                _logger.LogWarning($"SEO: '{normalizedPath}' için aktif ayar bulunamadı");

                // Debug: Tüm aktif ayarları listele
                var allActive = await _context.SeoSettings.Where(s => s.IsActive).ToListAsync();
                _logger.LogDebug($"SEO: Veritabanında {allActive.Count} aktif ayar var: {string.Join(", ", allActive.Select(s => s.PagePath))}");
            }

            return setting;
        }
    }
}
