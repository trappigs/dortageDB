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

        // GET: Admin/AllSatislar - GÜNCELLEME
        public async Task<IActionResult> AllSatislar()
        {
            var satislar = await _context.Satislar
                .Include(s => s.Musteri)
                .Include(s => s.Topraktar)
                .OrderByDescending(s => s.SatilmaTarihi)
                .ToListAsync();

            // Dropdown'lar için veri hazırla
            ViewBag.Musteriler = await _context.Musteriler
                .OrderBy(m => m.Ad)
                .ThenBy(m => m.Soyad)
                .ToListAsync();

            ViewBag.Topraktarlar = await _userManager.GetUsersInRoleAsync("topraktar");

            return View(satislar);
        }
        // POST: Admin/CreateSatis
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateSatis(int SatilanMusteriID, int TopraktarID,
            DateTime SatilmaTarihi, decimal ToplamSatisFiyati, string Bolge, bool Taksit, decimal OdenecekKomisyon)
        {
            try
            {
                // Validate inputs
                if (string.IsNullOrWhiteSpace(Bolge) || ToplamSatisFiyati <= 0 || OdenecekKomisyon < 0)
                {
                    TempData["ErrorMessage"] = "Geçersiz veri girişi.";
                    return RedirectToAction(nameof(AllSatislar));
                }

                // Müşteri ve Topraktar kontrolü
                var musteri = await _context.Musteriler.FindAsync(SatilanMusteriID);
                var topraktar = await _userManager.FindByIdAsync(TopraktarID.ToString());

                if (musteri == null || topraktar == null)
                {
                    TempData["ErrorMessage"] = "Geçersiz müşteri veya topraktar.";
                    return RedirectToAction(nameof(AllSatislar));
                }

                var satis = new Satis
                {
                    SatilanMusteriID = SatilanMusteriID,
                    TopraktarID = TopraktarID,
                    SatilmaTarihi = SatilmaTarihi,
                    ToplamSatisFiyati = ToplamSatisFiyati,
                    Bolge = Bolge.Trim(),
                    Taksit = Taksit,
                    OdenecekKomisyon = OdenecekKomisyon
                };

                _context.Satislar.Add(satis);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"✅ Yeni satış oluşturuldu: #{satis.SatisID} - ₺{satis.ToplamSatisFiyati} (Admin: {User.Identity.Name})");
                TempData["SuccessMessage"] = "Satış başarıyla eklendi.";
                return RedirectToAction(nameof(AllSatislar));
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ Satış oluşturma hatası: {ex.Message}");
                TempData["ErrorMessage"] = "Bir hata oluştu. Lütfen tekrar deneyin.";
                return RedirectToAction(nameof(AllSatislar));
            }
        }


        // POST: Admin/EditSatis
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditSatis(int SatisID, int SatilanMusteriID, int TopraktarID,
            DateTime SatilmaTarihi, decimal ToplamSatisFiyati, string Bolge, bool Taksit, decimal OdenecekKomisyon)
        {
            try
            {
                var satis = await _context.Satislar.FindAsync(SatisID);
                if (satis == null)
                {
                    TempData["ErrorMessage"] = "Satış bulunamadı.";
                    return RedirectToAction(nameof(AllSatislar));
                }

                // Validate inputs
                if (string.IsNullOrWhiteSpace(Bolge) || ToplamSatisFiyati <= 0 || OdenecekKomisyon < 0)
                {
                    TempData["ErrorMessage"] = "Geçersiz veri girişi.";
                    return RedirectToAction(nameof(AllSatislar));
                }

                // Müşteri ve Topraktar kontrolü
                var musteri = await _context.Musteriler.FindAsync(SatilanMusteriID);
                var topraktar = await _userManager.FindByIdAsync(TopraktarID.ToString());

                if (musteri == null || topraktar == null)
                {
                    TempData["ErrorMessage"] = "Geçersiz müşteri veya topraktar.";
                    return RedirectToAction(nameof(AllSatislar));
                }

                // Update satis
                satis.SatilanMusteriID = SatilanMusteriID;
                satis.TopraktarID = TopraktarID;
                satis.SatilmaTarihi = SatilmaTarihi;
                satis.ToplamSatisFiyati = ToplamSatisFiyati;
                satis.Bolge = Bolge.Trim();
                satis.Taksit = Taksit;
                satis.OdenecekKomisyon = OdenecekKomisyon;

                await _context.SaveChangesAsync();

                _logger.LogInformation($"✅ Satış #{SatisID} güncellendi - ₺{ToplamSatisFiyati} (Admin: {User.Identity.Name})");
                TempData["SuccessMessage"] = "Satış başarıyla güncellendi.";
                return RedirectToAction(nameof(AllSatislar));
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ Satış güncelleme hatası: {ex.Message}");
                TempData["ErrorMessage"] = "Bir hata oluştu. Lütfen tekrar deneyin.";
                return RedirectToAction(nameof(AllSatislar));
            }
        }

        // POST: Admin/DeleteSatis
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSatis(int id)
        {
            try
            {
                var satis = await _context.Satislar.FindAsync(id);
                if (satis == null)
                {
                    TempData["ErrorMessage"] = "Satış bulunamadı.";
                    return RedirectToAction(nameof(AllSatislar));
                }

                var satisInfo = $"#{satis.SatisID} - ₺{satis.ToplamSatisFiyati}";

                _context.Satislar.Remove(satis);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"✅ Satış {satisInfo} silindi (Admin: {User.Identity.Name})");
                TempData["SuccessMessage"] = "Satış başarıyla silindi.";
                return RedirectToAction(nameof(AllSatislar));
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ Satış silme hatası: {ex.Message}");
                TempData["ErrorMessage"] = "Bir hata oluştu. Lütfen tekrar deneyin.";
                return RedirectToAction(nameof(AllSatislar));
            }
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


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateRandevuStatus(int id, string status)
        {
            try
            {
                var randevu = await _context.Randevular.FindAsync(id);
                if (randevu == null)
                {
                    TempData["ErrorMessage"] = "Randevu bulunamadı.";
                    return RedirectToAction(nameof(AllRandevular));
                }

                // Parse status to enum
                if (Enum.TryParse<RandevuDurum>(status, true, out var randevuDurum))
                {
                    randevu.RandevuDurum = randevuDurum;
                    await _context.SaveChangesAsync();

                    _logger.LogInformation($"✅ Randevu #{id} durumu güncellendi: {randevuDurum} (Admin: {User.Identity.Name})");
                    TempData["SuccessMessage"] = "Randevu durumu başarıyla güncellendi.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Geçersiz durum değeri.";
                }

                return RedirectToAction(nameof(AllRandevular));
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ Randevu durum güncelleme hatası: {ex.Message}");
                TempData["ErrorMessage"] = "Bir hata oluştu. Lütfen tekrar deneyin.";
                return RedirectToAction(nameof(AllRandevular));
            }
        }

        // POST: Admin/EditRandevu
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditRandevu(int id, DateTime randevuZaman, string bolge, string randevuTipi)
        {
            try
            {
                var randevu = await _context.Randevular.FindAsync(id);
                if (randevu == null)
                {
                    TempData["ErrorMessage"] = "Randevu bulunamadı.";
                    return RedirectToAction(nameof(AllRandevular));
                }

                // Validate inputs
                if (string.IsNullOrWhiteSpace(bolge) || string.IsNullOrWhiteSpace(randevuTipi))
                {
                    TempData["ErrorMessage"] = "Tüm alanlar doldurulmalıdır.";
                    return RedirectToAction(nameof(AllRandevular));
                }

                // Update randevu
                randevu.RandevuZaman = randevuZaman;
                randevu.Bolge = bolge.Trim();
                randevu.RandevuTipi = randevuTipi.Trim();

                await _context.SaveChangesAsync();

                _logger.LogInformation($"✅ Randevu #{id} düzenlendi (Admin: {User.Identity.Name})");
                TempData["SuccessMessage"] = "Randevu başarıyla güncellendi.";
                return RedirectToAction(nameof(AllRandevular));
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ Randevu düzenleme hatası: {ex.Message}");
                TempData["ErrorMessage"] = "Bir hata oluştu. Lütfen tekrar deneyin.";
                return RedirectToAction(nameof(AllRandevular));
            }
        }


        // POST: Admin/DeleteRandevu
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteRandevu(int id)
        {
            try
            {
                var randevu = await _context.Randevular.FindAsync(id);
                if (randevu == null)
                {
                    TempData["ErrorMessage"] = "Randevu bulunamadı.";
                    return RedirectToAction(nameof(AllRandevular));
                }

                _context.Randevular.Remove(randevu);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"✅ Randevu #{id} silindi (Admin: {User.Identity.Name})");
                TempData["SuccessMessage"] = "Randevu başarıyla silindi.";
                return RedirectToAction(nameof(AllRandevular));
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ Randevu silme hatası: {ex.Message}");
                TempData["ErrorMessage"] = "Bir hata oluştu. Lütfen tekrar deneyin.";
                return RedirectToAction(nameof(AllRandevular));
            }
        }
    }
}