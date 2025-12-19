using dortageDB.Data;
using dortageDB.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Threading; // SemaphoreSlim için gerekli

namespace dortageDB.Services
{
    public class SeoService : ISeoService
    {
        private readonly AppDbContext _context;
        private readonly IMemoryCache _cache;
        private readonly ILogger<SeoService> _logger;

        // Cache süreleri
        private const int CacheDurationHours = 1; // Bulunan kayıtlar için
        private const int EmptyCacheDurationMinutes = 10; // Bulunamayan (Null) kayıtlar için

        // AĞAM, İŞTE SİLAHIMIZ BU: Statik Kilit (Tüm istekler bu kilidi ortak kullanır)
        private static readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        public SeoService(AppDbContext context, IMemoryCache cache, ILogger<SeoService> logger)
        {
            _context = context;
            _cache = cache;
            _logger = logger;
        }

        public async Task<SeoSetting?> GetSeoSettingAsync(string pagePath)
        {
            if (string.IsNullOrWhiteSpace(pagePath))
            {
                return null;
            }

            // Normalize path
            var normalizedPath = pagePath == "/" ? "/" : pagePath.TrimEnd('/').ToLowerInvariant();
            var cacheKey = $"SeoSetting_{normalizedPath}";

            // 1. AŞAMA: Hızlı Kontrol (Kilit yok, herkes bakar)
            if (_cache.TryGetValue(cacheKey, out SeoSetting? cachedSetting))
            {
                // _logger.LogInformation($"SEO: Cache'den hızlıca getirildi: '{normalizedPath}'"); // Log kirliliği olmasın diye kapattım
                return cachedSetting;
            }

            // Cache boş! Şimdi kilidi devreye sokuyoruz.
            // Aynı anda 100 kişi de gelse, sadece 1'i içeri girecek.
            await _semaphore.WaitAsync();

            try
            {
                // 2. AŞAMA: Çifte Kontrol (Double-Check Locking)
                // Belki biz kilidi beklerken, bizden önceki arkadaş veriyi getirip Cache'e koymuştur?
                if (_cache.TryGetValue(cacheKey, out cachedSetting))
                {
                    _logger.LogInformation($"SEO: Kilit sonrası Cache'den alındı: '{normalizedPath}'");
                    return cachedSetting;
                }

                // Veritabanına gerçekten gitme vakti
                _logger.LogInformation($"SEO: Veritabanına gidiliyor... Path: '{normalizedPath}'");

                var setting = await _context.SeoSettings
                    .AsNoTracking() // Performans artışı: Sadece okuma yapıyoruz, takibe gerek yok
                    .Where(s => s.IsActive && s.PagePath.ToLower() == normalizedPath)
                    .FirstOrDefaultAsync();

                var cacheEntryOptions = new MemoryCacheEntryOptions();

                if (setting != null)
                {
                    // Veri bulundu -> Uzun süreli cache
                    cacheEntryOptions.SetAbsoluteExpiration(TimeSpan.FromHours(CacheDurationHours));
                    cacheEntryOptions.SetPriority(CacheItemPriority.High);
                    _logger.LogInformation($"SEO: DB'den çekildi ve Cache'lendi (Dolu).");
                }
                else
                {
                    // Veri YOK -> Kısa süreli cache (Saldırı koruması)
                    // Eğer bunu yapmazsak, kötü niyetli biri "/olmayan-sayfa-1", "/olmayan-sayfa-2" diye
                    // sürekli DB'ye sorgu attırabilir.
                    cacheEntryOptions.SetAbsoluteExpiration(TimeSpan.FromMinutes(EmptyCacheDurationMinutes));
                    cacheEntryOptions.SetPriority(CacheItemPriority.Low);
                    _logger.LogWarning($"SEO: Veri bulunamadı, NULL olarak cache'lendi: '{normalizedPath}'");
                }

                // Cache'e yaz (Setting null olsa bile yazıyoruz ki tekrar aramayalım)
                _cache.Set(cacheKey, setting, cacheEntryOptions);

                return setting;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"SEO: Veritabanı hatası! Path: {normalizedPath}");
                return null; // Site patlamasın diye null dönüyoruz
            }
            finally
            {
                // İşimiz bitti, kilidi açıyoruz ki sıradakiler (eğer varsa) işine baksın.
                _semaphore.Release();
            }
        }
    }
}