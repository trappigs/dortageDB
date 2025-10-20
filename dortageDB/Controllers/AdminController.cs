using dortageDB.Data;
using dortageDB.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace dortageDB.Controllers
{
    [Authorize(Roles = "admin")]
    public class AdminController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly ILogger<AdminController> _logger;

        public AdminController(
            AppDbContext context,
            UserManager<AppUser> userManager,
            ILogger<AdminController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        // Admin Dashboard
        public async Task<IActionResult> Index()
        {
            var stats = new
            {
                TotalTopraktars = await _userManager.GetUsersInRoleAsync("topraktar"),
                TotalMusteriler = await _context.Musteriler.CountAsync(),
                TotalRandevular = await _context.Randevular.CountAsync(),
                PendingRandevular = await _context.Randevular.CountAsync(r => r.RandevuDurum == RandevuDurum.pending),
                TotalSatislar = await _context.Satislar.CountAsync(),
                TotalKomisyon = await _context.Satislar.SumAsync(s => (decimal?)s.OdenecekKomisyon) ?? 0,
                ActiveReferrals = await _context.Referrals.CountAsync(r => r.IsActive)
            };

            ViewBag.Stats = stats;
            return View();
        }

        // ====================================
        // REFERRAL CODE MANAGEMENT
        // ====================================

        // GET: Admin/Referrals
        public async Task<IActionResult> Referrals()
        {
            var referrals = await _context.Referrals
                .OrderByDescending(r => r.CreatedAtUtc)
                .ToListAsync();

            return View(referrals);
        }

        // POST: Admin/CreateReferral
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateReferral(string code, int? maxUses, DateTime? expiresAt)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(code))
                {
                    TempData["ErrorMessage"] = "Referans kodu boş olamaz.";
                    return RedirectToAction(nameof(Referrals));
                }

                // Normalize code to uppercase
                code = code.Trim().ToUpper();

                // Check if code already exists
                if (await _context.Referrals.AnyAsync(r => r.Code == code))
                {
                    TempData["ErrorMessage"] = "Bu referans kodu zaten mevcut.";
                    return RedirectToAction(nameof(Referrals));
                }

                var referral = new Referral
                {
                    Code = code,
                    IsActive = true,
                    MaxUses = maxUses,
                    UsedCount = 0,
                    ExpiresAt = expiresAt,
                    CreatedByUserId = _userManager.GetUserId(User),
                    CreatedAtUtc = DateTime.UtcNow
                };

                _context.Referrals.Add(referral);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"✅ Referans kodu oluşturuldu: {code} (Admin: {User.Identity.Name})");
                TempData["SuccessMessage"] = "Referans kodu başarıyla oluşturuldu.";
                return RedirectToAction(nameof(Referrals));
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ Referans kodu oluşturma hatası: {ex.Message}");
                TempData["ErrorMessage"] = "Bir hata oluştu. Lütfen tekrar deneyin.";
                return RedirectToAction(nameof(Referrals));
            }
        }

        // POST: Admin/ToggleReferralStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleReferralStatus(int id)
        {
            try
            {
                var referral = await _context.Referrals.FindAsync(id);
                if (referral == null)
                {
                    TempData["ErrorMessage"] = "Referans kodu bulunamadı.";
                    return RedirectToAction(nameof(Referrals));
                }

                referral.IsActive = !referral.IsActive;
                await _context.SaveChangesAsync();

                _logger.LogInformation($"✅ Referans kodu durumu değiştirildi: {referral.Code} -> {referral.IsActive}");
                TempData["SuccessMessage"] = $"Referans kodu {(referral.IsActive ? "aktif" : "pasif")} edildi.";
                return RedirectToAction(nameof(Referrals));
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ Referans kodu durum değiştirme hatası: {ex.Message}");
                TempData["ErrorMessage"] = "Bir hata oluştu. Lütfen tekrar deneyin.";
                return RedirectToAction(nameof(Referrals));
            }
        }

        // POST: Admin/DeleteReferral
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteReferral(int id)
        {
            try
            {
                var referral = await _context.Referrals.FindAsync(id);
                if (referral == null)
                {
                    TempData["ErrorMessage"] = "Referans kodu bulunamadı.";
                    return RedirectToAction(nameof(Referrals));
                }

                _context.Referrals.Remove(referral);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"✅ Referans kodu silindi: {referral.Code}");
                TempData["SuccessMessage"] = "Referans kodu başarıyla silindi.";
                return RedirectToAction(nameof(Referrals));
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ Referans kodu silme hatası: {ex.Message}");
                TempData["ErrorMessage"] = "Bir hata oluştu. Lütfen tekrar deneyin.";
                return RedirectToAction(nameof(Referrals));
            }
        }

        // ====================================
        // TÜM KAYITLARA ERİŞİM
        // ====================================

        // GET: Admin/AllMusteriler
        public async Task<IActionResult> AllMusteriler()
        {
            var musteriler = await _context.Musteriler
                .Include(m => m.Randevular)
                .Include(m => m.Satislar)
                .OrderByDescending(m => m.IdMusteri)
                .ToListAsync();

            return View(musteriler);
        }

        // GET: Admin/AllRandevular
        public async Task<IActionResult> AllRandevular()
        {
            var randevular = await _context.Randevular
                .Include(r => r.Musteri)
                .Include(r => r.Topraktar)
                .OrderByDescending(r => r.RandevuZaman)
                .ToListAsync();

            return View(randevular);
        }

        // GET: Admin/AllSatislar
        public async Task<IActionResult> AllSatislar()
        {
            var satislar = await _context.Satislar
                .Include(s => s.Musteri)
                .Include(s => s.Topraktar)
                .OrderByDescending(s => s.SatilmaTarihi)
                .ToListAsync();

            return View(satislar);
        }

        // GET: Admin/AllTopraktars
        public async Task<IActionResult> AllTopraktars()
        {
            var topraktars = await _userManager.GetUsersInRoleAsync("topraktar");

            var topraktarData = new List<dynamic>();
            foreach (var topraktar in topraktars)
            {
                var randevuCount = await _context.Randevular.CountAsync(r => r.TopraktarID == topraktar.Id);
                var satisCount = await _context.Satislar.CountAsync(s => s.TopraktarID == topraktar.Id);
                var totalKomisyon = await _context.Satislar
                    .Where(s => s.TopraktarID == topraktar.Id)
                    .SumAsync(s => (decimal?)s.OdenecekKomisyon) ?? 0;

                topraktarData.Add(new
                {
                    User = topraktar,
                    RandevuCount = randevuCount,
                    SatisCount = satisCount,
                    TotalKomisyon = totalKomisyon
                });
            }

            ViewBag.TopraktarData = topraktarData;
            return View();
        }
    }
}