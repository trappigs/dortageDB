using dortageDB.Data;
using dortageDB.Entities;
using dortageDB.Helpers;
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
        private readonly IWebHostEnvironment _webHostEnvironment;

        public AdminController(
            AppDbContext context,
            UserManager<AppUser> userManager,
            ILogger<AdminController> logger,
            IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
            _webHostEnvironment = webHostEnvironment;
        }

        // Admin Dashboard
        public async Task<IActionResult> Index()
        {
            var stats = new
            {
                TotalVekarers = await _userManager.GetUsersInRoleAsync("Vekarer"),
                TotalMusteriler = await _context.Musteriler.CountAsync(),
                TotalRandevular = await _context.Randevular.CountAsync(),
                PendingRandevular = await _context.Randevular.CountAsync(r => r.RandevuDurum == RandevuDurum.OnayBekliyor),
                TotalSatislar = await _context.Satislar.CountAsync(),
                TotalSatisMiktari = await _context.Satislar.SumAsync(s => (decimal?)s.ToplamSatisFiyati) ?? 0,
                TotalKomisyon = await _context.Satislar.SumAsync(s => (decimal?)s.OdenecekKomisyon) ?? 0,
                ActiveReferrals = await _context.Referrals.CountAsync(r => r.IsActive),
                TotalVideos = await _context.EgitimVideolar.CountAsync(),
                ActiveVideos = await _context.EgitimVideolar.CountAsync(v => v.Aktif)
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

                var userIdString = _userManager.GetUserId(User);
                var referral = new Referral
                {
                    Code = code,
                    IsActive = true,
                    MaxUses = maxUses,
                    UsedCount = 0,
                    ExpiresAt = expiresAt,
                    CreatedByUserId = userIdString != null ? int.Parse(userIdString) : null,
                    CreatedAtUtc = DateTime.UtcNow
                };

                _context.Referrals.Add(referral);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"? Referans kodu oluşturuldu: {code} (Admin: {User.Identity.Name})");
                TempData["SuccessMessage"] = "Referans kodu başarıyla oluşturuldu.";
                return RedirectToAction(nameof(Referrals));
            }
            catch (Exception ex)
            {
                _logger.LogError($"? Referans kodu oluşturma hatası: {ex.Message}");
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

                _logger.LogInformation($"Referans kodu durumu değiştirildi: {referral.Code} -> {referral.IsActive}");
                TempData["SuccessMessage"] = $"Referans kodu {(referral.IsActive ? "aktif" : "pasif")} edildi.";
                return RedirectToAction(nameof(Referrals));
            }
            catch (Exception ex)
            {
                _logger.LogError($"Referans kodu durum değiştirme hatası: {ex.Message}");
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

                _logger.LogInformation($"? Referans kodu silindi: {referral.Code}");
                TempData["SuccessMessage"] = "Referans kodu başarıyla silindi.";
                return RedirectToAction(nameof(Referrals));
            }
            catch (Exception ex)
            {
                _logger.LogError($"? Referans kodu silme hatası: {ex.Message}");
                TempData["ErrorMessage"] = "Bir hata oluştu. Lütfen tekrar deneyin.";
                return RedirectToAction(nameof(Referrals));
            }
        }

        // ====================================
        // T�M KAYITLARA ER���M
        // ====================================

        // GET: Admin/AllMusteriler
        public async Task<IActionResult> AllMusteriler()
        {
            var musteriler = await _context.Musteriler
                .Include(m => m.Randevular)
                .Include(m => m.Satislar)
                .Include(m => m.Vekarer)
                .OrderByDescending(m => m.IdMusteri)
                .ToListAsync();

            return View(musteriler);
        }

        // GET: Admin/AllRandevular
        public async Task<IActionResult> AllRandevular()
        {
            var randevular = await _context.Randevular
                .Include(r => r.Musteri)
                .Include(r => r.Vekarer)
                .OrderByDescending(r => r.RandevuZaman)
                .ToListAsync();

            return View(randevular);
        }

        // GET: Admin/AllSatislar - G�NCELLEME
        public async Task<IActionResult> AllSatislar()
        {
            var satislar = await _context.Satislar
                .Include(s => s.Musteri)
                .Include(s => s.Vekarer)
                .OrderByDescending(s => s.SatilmaTarihi)
                .ToListAsync();

            // Dropdown'lar i�in veri haz�rla
            ViewBag.Musteriler = await _context.Musteriler
                .OrderBy(m => m.Ad)
                .ThenBy(m => m.Soyad)
                .ToListAsync();

            ViewBag.Vekarerlar = await _userManager.GetUsersInRoleAsync("Vekarer");

            return View(satislar);
        }
        // POST: Admin/CreateSatis
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateSatis(int SatilanMusteriID, int VekarerID,
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

                // M��teri ve Vekarer kontrol�
                var musteri = await _context.Musteriler.FindAsync(SatilanMusteriID);
                var Vekarer = await _userManager.FindByIdAsync(VekarerID.ToString());

                if (musteri == null || Vekarer == null)
                {
                    TempData["ErrorMessage"] = "Geçersiz müşteri veya Vekarer.";
                    return RedirectToAction(nameof(AllSatislar));
                }

                var satis = new Satis
                {
                    SatilanMusteriID = SatilanMusteriID,
                    VekarerID = VekarerID,
                    SatilmaTarihi = SatilmaTarihi,
                    ToplamSatisFiyati = ToplamSatisFiyati,
                    Bolge = Bolge.Trim(),
                    Taksit = Taksit,
                    OdenecekKomisyon = OdenecekKomisyon
                };

                _context.Satislar.Add(satis);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"? Yeni satış oluşturuldu: #{satis.SatisID} - ?{satis.ToplamSatisFiyati} (Admin: {User.Identity.Name})");
                TempData["SuccessMessage"] = "Satış başarıyla eklendi.";
                return RedirectToAction(nameof(AllSatislar));
            }
            catch (Exception ex)
            {
                _logger.LogError($"? Satış oluşturma hatası: {ex.Message}");
                TempData["ErrorMessage"] = "Bir hata oluştu. Lütfen tekrar deneyin.";
                return RedirectToAction(nameof(AllSatislar));
            }
        }


        // POST: Admin/EditSatis
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditSatis(int SatisID, int SatilanMusteriID, int VekarerID,
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

                // M��teri ve Vekarer kontrol�
                var musteri = await _context.Musteriler.FindAsync(SatilanMusteriID);
                var Vekarer = await _userManager.FindByIdAsync(VekarerID.ToString());

                if (musteri == null || Vekarer == null)
                {
                    TempData["ErrorMessage"] = "Geçersiz müşteri veya Vekarer.";
                    return RedirectToAction(nameof(AllSatislar));
                }

                // Update satis
                satis.SatilanMusteriID = SatilanMusteriID;
                satis.VekarerID = VekarerID;
                satis.SatilmaTarihi = SatilmaTarihi;
                satis.ToplamSatisFiyati = ToplamSatisFiyati;
                satis.Bolge = Bolge.Trim();
                satis.Taksit = Taksit;
                satis.OdenecekKomisyon = OdenecekKomisyon;

                await _context.SaveChangesAsync();

                _logger.LogInformation($"? Satış #{SatisID} güncellendi - ?{ToplamSatisFiyati} (Admin: {User.Identity.Name})");
                TempData["SuccessMessage"] = "Satış başarıyla güncellendi.";
                return RedirectToAction(nameof(AllSatislar));
            }
            catch (Exception ex)
            {
                _logger.LogError($"? Satış güncelleme hatası: {ex.Message}");
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

                var satisInfo = $"#{satis.SatisID} - ?{satis.ToplamSatisFiyati}";

                _context.Satislar.Remove(satis);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"? Satış {satisInfo} silindi (Admin: {User.Identity.Name})");
                TempData["SuccessMessage"] = "Satış başarıyla silindi.";
                return RedirectToAction(nameof(AllSatislar));
            }
            catch (Exception ex)
            {
                _logger.LogError($"? Satış silme hatası: {ex.Message}");
                TempData["ErrorMessage"] = "Bir hata oluştu. Lütfen tekrar deneyin.";
                return RedirectToAction(nameof(AllSatislar));
            }
        }



        // GET: Admin/AllVekarers
        public async Task<IActionResult> AllVekarers()
        {
            var Vekarers = await _userManager.GetUsersInRoleAsync("Vekarer");

            var VekarerData = new List<dynamic>();
            foreach (var Vekarer in Vekarers)
            {
                var randevuCount = await _context.Randevular.CountAsync(r => r.VekarerID == Vekarer.Id);
                var satisCount = await _context.Satislar.CountAsync(s => s.VekarerID == Vekarer.Id);
                var totalKomisyon = await _context.Satislar
                    .Where(s => s.VekarerID == Vekarer.Id)
                    .SumAsync(s => (decimal?)s.OdenecekKomisyon) ?? 0;

                // Calculate total sales (ciro)
                var totalCiro = await _context.Satislar
                    .Where(s => s.VekarerID == Vekarer.Id)
                    .SumAsync(s => (decimal?)s.ToplamSatisFiyati) ?? 0;

                // Get VekarerProfile for ReferralCode and UsedReferralCode
                var profile = await _context.VekarerProfiles
                    .FirstOrDefaultAsync(p => p.UserId == Vekarer.Id);

                VekarerData.Add(new
                {
                    User = Vekarer,
                    RandevuCount = randevuCount,
                    SatisCount = satisCount,
                    TotalKomisyon = totalKomisyon,
                    TotalCiro = totalCiro,
                    ReferralCode = profile?.ReferralCode,
                    UsedReferralCode = profile?.UsedReferralCode // Kay�t olurken kulland��� kod
                });
            }

            ViewBag.VekarerData = VekarerData;
            return View();
        }

        // POST: Admin/CreateRandevu
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateRandevu(int MusteriId, int VekarerID,
            DateTime RandevuZaman, string? Aciklama, string RandevuTipi)
        {
            try
            {
                // Validate inputs
                if (string.IsNullOrWhiteSpace(RandevuTipi))
                {
                    TempData["ErrorMessage"] = "Randevu tipi gereklidir.";
                    return RedirectToAction(nameof(AllRandevular));
                }

                // M��teri ve Vekarer kontrol�
                var musteri = await _context.Musteriler.FindAsync(MusteriId);
                var Vekarer = await _userManager.FindByIdAsync(VekarerID.ToString());

                if (musteri == null || Vekarer == null)
                {
                    TempData["ErrorMessage"] = "Geçersiz müşteri veya Vekarer.";
                    return RedirectToAction(nameof(AllRandevular));
                }

                var randevu = new Randevu
                {
                    MusteriId = MusteriId,
                    VekarerID = VekarerID,
                    RandevuZaman = RandevuZaman,
                    RandevuTipi = RandevuTipi.Trim(),
                    Aciklama = string.IsNullOrWhiteSpace(Aciklama) ? null : Aciklama.Trim(),
                    RandevuDurum = RandevuDurum.OnayBekliyor
                };

                _context.Randevular.Add(randevu);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"? Yeni randevu oluşturuldu: #{randevu.RandevuID} (Admin: {User.Identity.Name})");
                TempData["SuccessMessage"] = "Randevu başarıyla eklendi.";
                return RedirectToAction(nameof(AllRandevular));
            }
            catch (Exception ex)
            {
                _logger.LogError($"? Randevu oluşturma hatası: {ex.Message}");
                TempData["ErrorMessage"] = "Bir hata oluştu. Lütfen tekrar deneyin.";
                return RedirectToAction(nameof(AllRandevular));
            }
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

                    _logger.LogInformation($"? Randevu #{id} durumu güncellendi: {randevuDurum} (Admin: {User.Identity.Name})");
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
                _logger.LogError($"? Randevu durum güncelleme hatası: {ex.Message}");
                TempData["ErrorMessage"] = "Bir hata oluştu. Lütfen tekrar deneyin.";
                return RedirectToAction(nameof(AllRandevular));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditRandevu(int id, DateTime randevuZaman, string? aciklama, string randevuTipi, string randevuDurum)
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
                if (string.IsNullOrWhiteSpace(randevuTipi) || string.IsNullOrWhiteSpace(randevuDurum))
                {
                    TempData["ErrorMessage"] = "Randevu tipi ve durum gereklidir.";
                    return RedirectToAction(nameof(AllRandevular));
                }

                // Parse and validate randevu durum
                if (!Enum.TryParse<RandevuDurum>(randevuDurum, true, out var durum))
                {
                    TempData["ErrorMessage"] = "Geçersiz randevu durumu.";
                    return RedirectToAction(nameof(AllRandevular));
                }

                // Update randevu
                randevu.RandevuZaman = randevuZaman;
                randevu.RandevuTipi = randevuTipi.Trim();
                randevu.Aciklama = string.IsNullOrWhiteSpace(aciklama) ? null : aciklama.Trim();
                randevu.RandevuDurum = durum;

                await _context.SaveChangesAsync();

                _logger.LogInformation($"? Randevu #{id} düzenlendi - Durum: {durum} (Admin: {User.Identity.Name})");
                TempData["SuccessMessage"] = "Randevu başarıyla güncellendi.";
                return RedirectToAction(nameof(AllRandevular));
            }
            catch (Exception ex)
            {
                _logger.LogError($"? Randevu düzenleme hatası: {ex.Message}");
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

                _logger.LogInformation($"? Randevu #{id} silindi (Admin: {User.Identity.Name})");
                TempData["SuccessMessage"] = "Randevu başarıyla silindi.";
                return RedirectToAction(nameof(AllRandevular));
            }
            catch (Exception ex)
            {
                _logger.LogError($"? Randevu silme hatası: {ex.Message}");
                TempData["ErrorMessage"] = "Bir hata oluştu. Lütfen tekrar deneyin.";
                return RedirectToAction(nameof(AllRandevular));
            }
        }

        // ====================================
        // PROJE Y�NET�M�
        // ====================================

        // GET: Admin/Projeler
        public async Task<IActionResult> Projeler()
        {
            var projeler = await _context.Projeler
                .OrderByDescending(p => p.Oncelik)
                .ThenByDescending(p => p.KayitTarihi)
                .ToListAsync();

            return View(projeler);
        }

        // GET: Admin/CreateProje
        public IActionResult CreateProje()
        {
            return View();
        }

        // POST: Admin/CreateProje
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateProje(Proje proje)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(proje.ProjeAdi) || string.IsNullOrWhiteSpace(proje.Konum))
                {
                    TempData["ErrorMessage"] = "Proje adı ve konum zorunludur.";
                    return View(proje);
                }

                proje.KayitTarihi = DateTime.Now;
                proje.AktifMi = true;

                // Slug boşsa otomatik oluştur, doluysa kullanıcının yazdığını kullan
                if (string.IsNullOrWhiteSpace(proje.Slug))
                {
                    proje.Slug = SlugHelper.GenerateSlug(proje.ProjeAdi);
                }
                else
                {
                    proje.Slug = SlugHelper.GenerateSlug(proje.Slug); // Temizle
                }

                _context.Projeler.Add(proje);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"? Yeni proje oluşturuldu: {proje.ProjeAdi} (Admin: {User.Identity.Name})");
                TempData["SuccessMessage"] = "Proje başarıyla oluşturuldu.";
                return RedirectToAction(nameof(Projeler));
            }
            catch (Exception ex)
            {
                _logger.LogError($"? Proje oluşturma hatası: {ex.Message}");
                TempData["ErrorMessage"] = "Bir hata oluştu. Lütfen tekrar deneyin.";
                return View(proje);
            }
        }

        // GET: Admin/EditProje/5
        public async Task<IActionResult> EditProje(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var proje = await _context.Projeler.FindAsync(id);
            if (proje == null)
            {
                return NotFound();
            }

            return View(proje);
        }

        // POST: Admin/EditProje/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProje(int id, Proje proje)
        {
            if (id != proje.ProjeID)
            {
                return NotFound();
            }

            try
            {
                if (string.IsNullOrWhiteSpace(proje.ProjeAdi) || string.IsNullOrWhiteSpace(proje.Konum))
                {
                    TempData["ErrorMessage"] = "Proje adı ve konum zorunludur.";
                    return View(proje);
                }

                var existingProje = await _context.Projeler.FindAsync(id);
                if (existingProje == null)
                {
                    return NotFound();
                }

                // Update properties
                existingProje.ProjeAdi = proje.ProjeAdi;

                // Slug boşsa otomatik oluştur, doluysa kullanıcının yazdığını kullan
                if (string.IsNullOrWhiteSpace(proje.Slug))
                {
                    existingProje.Slug = SlugHelper.GenerateSlug(proje.ProjeAdi);
                }
                else
                {
                    existingProje.Slug = SlugHelper.GenerateSlug(proje.Slug); // Temizle
                }

                existingProje.Aciklama = proje.Aciklama;
                existingProje.KisaAciklama = proje.KisaAciklama;
                existingProje.Konum = proje.Konum;
                existingProje.Sehir = proje.Sehir;
                existingProje.Ilce = proje.Ilce;
                existingProje.YatirimTuru = proje.YatirimTuru;
                existingProje.MinFiyat = proje.MinFiyat;
                existingProje.MaxFiyat = proje.MaxFiyat;
                existingProje.ToplamParsel = proje.ToplamParsel;
                existingProje.SatilanParsel = proje.SatilanParsel;
                existingProje.KapakGorseli = proje.KapakGorseli;
                existingProje.KapakGorseliAlt = proje.KapakGorseliAlt;
                existingProje.GaleriGorselleri = proje.GaleriGorselleri;
                existingProje.GaleriGorselleriAlt = proje.GaleriGorselleriAlt;
                existingProje.KapakVideosu = proje.KapakVideosu;
                existingProje.Tour360Url = proje.Tour360Url;
                existingProje.SunumDosyaUrl = proje.SunumDosyaUrl;
                existingProje.Imarlimi = proje.Imarlimi;
                existingProje.MustakilTapu = proje.MustakilTapu;
                existingProje.TaksitImkani = proje.TaksitImkani;
                existingProje.TakasImkani = proje.TakasImkani;
                existingProje.Altyapi = proje.Altyapi;
                existingProje.MetreKare = proje.MetreKare;
                existingProje.MinMetreKare = proje.MinMetreKare;
                existingProje.MaxMetreKare = proje.MaxMetreKare;
                existingProje.KrediyeUygunluk = proje.KrediyeUygunluk;
                existingProje.OzelliklerJson = proje.OzelliklerJson;
                existingProje.AktifMi = proje.AktifMi;
                existingProje.Oncelik = proje.Oncelik;
                existingProje.YeniBadge = proje.YeniBadge;
                existingProje.YakinProjeler = proje.YakinProjeler;
                existingProje.YakinBolgeler = proje.YakinBolgeler;
                existingProje.UlasimBilgileri = proje.UlasimBilgileri;
                existingProje.SosyalTesisler = proje.SosyalTesisler;
                existingProje.GuncellemeTarihi = DateTime.Now;

                await _context.SaveChangesAsync();

                _logger.LogInformation($"? Proje #{id} güncellendi: {proje.ProjeAdi} (Admin: {User.Identity.Name})");
                TempData["SuccessMessage"] = "Proje başarıyla güncellendi.";
                return RedirectToAction(nameof(Projeler));
            }
            catch (Exception ex)
            {
                _logger.LogError($"? Proje güncelleme hatası: {ex.Message}");
                TempData["ErrorMessage"] = "Bir hata oluştu. Lütfen tekrar deneyin.";
                return View(proje);
            }
        }

        // GET: Admin/DeleteProje/5
        public async Task<IActionResult> DeleteProje(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var proje = await _context.Projeler
                .FirstOrDefaultAsync(m => m.ProjeID == id);

            if (proje == null)
            {
                return NotFound();
            }

            return View(proje);
        }

        // POST: Admin/DeleteProje/5
        [HttpPost, ActionName("DeleteProje")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteProjeConfirmed(int id)
        {
            try
            {
                var proje = await _context.Projeler.FindAsync(id);
                if (proje == null)
                {
                    TempData["ErrorMessage"] = "Proje bulunamadı.";
                    return RedirectToAction(nameof(Projeler));
                }

                var projeAdi = proje.ProjeAdi;
                _context.Projeler.Remove(proje);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"? Proje silindi: {projeAdi} (Admin: {User.Identity.Name})");
                TempData["SuccessMessage"] = "Proje başarıyla silindi.";
                return RedirectToAction(nameof(Projeler));
            }
            catch (Exception ex)
            {
                _logger.LogError($"? Proje silme hatası: {ex.Message}");
                TempData["ErrorMessage"] = "Bir hata oluştu. Lütfen tekrar deneyin.";
                return RedirectToAction(nameof(Projeler));
            }
        }

        // GET: Admin/DetailsProje/5
        public async Task<IActionResult> DetailsProje(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var proje = await _context.Projeler
                .Include(p => p.Satislar)
                .Include(p => p.Randevular)
                .FirstOrDefaultAsync(m => m.ProjeID == id);

            if (proje == null)
            {
                return NotFound();
            }

            return View(proje);
        }

        // ====================================
        // DOSYA Y�KLEME
        // ====================================

        // POST: Admin/UploadGalleryImage
        [HttpPost]
        public async Task<IActionResult> UploadGalleryImage(IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return Json(new { success = false, message = "Dosya seçilmedi." });
                }

                // Dosya boyutu kontrol� (5MB)
                if (file.Length > 5 * 1024 * 1024)
                {
                    return Json(new { success = false, message = "Dosya boyutu 5MB'dan küçük olmalıdır." });
                }

                // Dosya t�r� kontrol�
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

                if (!allowedExtensions.Contains(extension))
                {
                    return Json(new { success = false, message = "Sadece resim dosyaları yüklenebilir (.jpg, .jpeg, .png, .webp, .gif)" });
                }

                // Uploads klas�r�n� olu�tur
                var uploadsPath = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "projeler");
                if (!Directory.Exists(uploadsPath))
                {
                    Directory.CreateDirectory(uploadsPath);
                }

                // Benzersiz dosya ad� olu�tur
                var fileName = $"{Guid.NewGuid()}{extension}";
                var filePath = Path.Combine(uploadsPath, fileName);

                // Dosyay� kaydet
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // URL'i d�nd�r
                var fileUrl = $"/uploads/projeler/{fileName}";

                _logger.LogInformation($"? Görsel yüklendi: {fileName} (Admin: {User.Identity.Name})");

                return Json(new { success = true, url = fileUrl });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Görsel yükleme hatası: {ex.Message}");
                return Json(new {
                    success = false,
                    message = $"Dosya yüklenirken bir hata oluştu: {ex.Message}",
                    error = ex.ToString() // Detaylı hata (production'da kaldırılabilir)
                });
            }
        }

        // POST: Admin/DeleteGalleryImage
        [HttpPost]
        public IActionResult DeleteGalleryImage(string imageUrl)
        {
            try
            {
                if (string.IsNullOrEmpty(imageUrl))
                {
                    return Json(new { success = false, message = "Görsel URL'si gerekli." });
                }

                // URL'den dosya yolunu ��kar
                if (imageUrl.StartsWith("/uploads/projeler/"))
                {
                    var fileName = Path.GetFileName(imageUrl);
                    var filePath = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "projeler", fileName);

                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                        _logger.LogInformation($"? Görsel silindi: {fileName} (Admin: {User.Identity.Name})");
                        return Json(new { success = true });
                    }
                }

                return Json(new { success = true }); // Dosya yoksa da success döndür
            }
            catch (Exception ex)
            {
                _logger.LogError($"? Görsel silme hatası: {ex.Message}");
                return Json(new { success = false, message = "Dosya silinirken bir hata oluştu." });
            }
        }

        // POST: Admin/UploadSunum
        [HttpPost]
        public async Task<IActionResult> UploadSunum(IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return Json(new { success = false, message = "Dosya seçilmedi." });
                }

                // Dosya boyutu kontrol� (10MB)
                if (file.Length > 10 * 1024 * 1024)
                {
                    return Json(new { success = false, message = "Dosya boyutu 10MB'dan küçük olmalıdır." });
                }

                // Dosya t�r� kontrol�
                var allowedExtensions = new[] { ".pdf", ".ppt", ".pptx" };
                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

                if (!allowedExtensions.Contains(extension))
                {
                    return Json(new { success = false, message = "Sadece PDF veya PowerPoint dosyaları yüklenebilir (.pdf, .ppt, .pptx)" });
                }

                // Uploads klas�r�n� olu�tur
                var uploadsPath = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "sunumlar");
                if (!Directory.Exists(uploadsPath))
                {
                    Directory.CreateDirectory(uploadsPath);
                }

                // Benzersiz dosya ad� olu�tur
                var fileName = $"{Guid.NewGuid()}{extension}";
                var filePath = Path.Combine(uploadsPath, fileName);

                // Dosyay� kaydet
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // URL'i d�nd�r
                var fileUrl = $"/uploads/sunumlar/{fileName}";

                _logger.LogInformation($"? Sunum yüklendi: {fileName} (Admin: {User.Identity.Name})");

                return Json(new { success = true, url = fileUrl });
            }
            catch (Exception ex)
            {
                _logger.LogError($"? Sunum yükleme hatası: {ex.Message}");
                return Json(new { success = false, message = "Dosya yüklenirken bir hata oluştu." });
            }
        }

        // POST: Admin/DeleteSunum
        [HttpPost]
        public IActionResult DeleteSunum(string fileUrl)
        {
            try
            {
                if (string.IsNullOrEmpty(fileUrl))
                {
                    return Json(new { success = false, message = "Dosya URL'si gerekli." });
                }

                // URL'den dosya yolunu ��kar
                if (fileUrl.StartsWith("/uploads/sunumlar/"))
                {
                    var fileName = Path.GetFileName(fileUrl);
                    var filePath = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "sunumlar", fileName);

                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                        _logger.LogInformation($"? Sunum silindi: {fileName} (Admin: {User.Identity.Name})");
                        return Json(new { success = true });
                    }
                }

                return Json(new { success = true }); // Dosya yoksa da success d�nd�r
            }
            catch (Exception ex)
            {
                _logger.LogError($"? Sunum silme hatası: {ex.Message}");
                return Json(new { success = false, message = "Dosya silinirken bir hata oluştu." });
            }
        }

        // ====================================
        // E��T�M V�DEO Y�NET�M�
        // ====================================

        // GET: Admin/EgitimVideolar
        public async Task<IActionResult> EgitimVideolar()
        {
            var videolar = await _context.EgitimVideolar
                .OrderByDescending(v => v.Sira)
                .ThenByDescending(v => v.EklenmeTarihi)
                .ToListAsync();

            return View(videolar);
        }

        // GET: Admin/VideoDetails/5
        public async Task<IActionResult> VideoDetails(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var video = await _context.EgitimVideolar
                .FirstOrDefaultAsync(m => m.VideoID == id);

            if (video == null)
            {
                return NotFound();
            }

            return View(video);
        }

        // GET: Admin/CreateVideo
        public IActionResult CreateVideo()
        {
            ViewBag.Kategoriler = EgitimKategorileri.GetAll();
            return View();
        }

        // POST: Admin/CreateVideo
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateVideo([Bind("Baslik,Aciklama,YoutubeVideoID,Kategori,Sure,OneEikan,Yeni,Populer,Sira,Aktif")] EgitimVideo video)
        {
            if (ModelState.IsValid)
            {
                video.EklenmeTarihi = DateTime.Now;
                video.IzlenmeSayisi = 0;
                video.BegeniSayisi = 0;

                _context.Add(video);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"? Eğitim videosu eklendi: {video.Baslik} (Admin: {User.Identity.Name})");
                TempData["SuccessMessage"] = "Video başarıyla eklendi!";
                return RedirectToAction(nameof(EgitimVideolar));
            }

            ViewBag.Kategoriler = EgitimKategorileri.GetAll();
            return View(video);
        }

        // GET: Admin/EditVideo/5
        public async Task<IActionResult> EditVideo(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var video = await _context.EgitimVideolar.FindAsync(id);
            if (video == null)
            {
                return NotFound();
            }

            ViewBag.Kategoriler = EgitimKategorileri.GetAll();
            return View(video);
        }

        // POST: Admin/EditVideo/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditVideo(int id, [Bind("VideoID,Baslik,Aciklama,YoutubeVideoID,Kategori,Sure,IzlenmeSayisi,BegeniSayisi,OneEikan,Yeni,Populer,Sira,Aktif,EklenmeTarihi")] EgitimVideo video)
        {
            if (id != video.VideoID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(video);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation($"? Eğitim videosu güncellendi: {video.Baslik} (Admin: {User.Identity.Name})");
                    TempData["SuccessMessage"] = "Video başarıyla güncellendi!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!VideoExists(video.VideoID))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(EgitimVideolar));
            }

            ViewBag.Kategoriler = EgitimKategorileri.GetAll();
            return View(video);
        }

        // GET: Admin/DeleteVideo/5
        public async Task<IActionResult> DeleteVideo(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var video = await _context.EgitimVideolar
                .FirstOrDefaultAsync(m => m.VideoID == id);

            if (video == null)
            {
                return NotFound();
            }

            return View(video);
        }

        // POST: Admin/DeleteVideo/5
        [HttpPost, ActionName("DeleteVideo")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteVideoConfirmed(int id)
        {
            try
            {
                var video = await _context.EgitimVideolar.FindAsync(id);
                if (video != null)
                {
                    var videoBaslik = video.Baslik;
                    _context.EgitimVideolar.Remove(video);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation($"? Eğitim videosu silindi: {videoBaslik} (Admin: {User.Identity.Name})");
                    TempData["SuccessMessage"] = "Video başarıyla silindi!";
                }

                return RedirectToAction(nameof(EgitimVideolar));
            }
            catch (Exception ex)
            {
                _logger.LogError($"? Video silme hatası: {ex.Message}");
                TempData["ErrorMessage"] = "Bir hata oluştu. Lütfen tekrar deneyin.";
                return RedirectToAction(nameof(EgitimVideolar));
            }
        }

        // POST: Admin/ToggleVideoAktif/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleVideoAktif(int id)
        {
            var video = await _context.EgitimVideolar.FindAsync(id);
            if (video == null)
            {
                return NotFound();
            }

            video.Aktif = !video.Aktif;
            await _context.SaveChangesAsync();

            _logger.LogInformation($"? Video durumu değiştirildi: {video.Baslik} -> {(video.Aktif ? "Aktif" : "Pasif")} (Admin: {User.Identity.Name})");
            TempData["SuccessMessage"] = $"Video {(video.Aktif ? "aktif" : "pasif")} hale getirildi!";
            return RedirectToAction(nameof(EgitimVideolar));
        }

        private bool VideoExists(int id)
        {
            return _context.EgitimVideolar.Any(e => e.VideoID == id);
        }

        // ============================================
        // M��TER� S�LME
        // ============================================

        // POST: Admin/DeleteMusteri/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteMusteri(int id)
        {
            try
            {
                var musteri = await _context.Musteriler
                    .Include(m => m.Randevular)
                    .Include(m => m.Satislar)
                    .FirstOrDefaultAsync(m => m.IdMusteri == id);

                if (musteri == null)
                {
                    TempData["ErrorMessage"] = "Müşteri bulunamadı.";
                    return RedirectToAction(nameof(AllMusteriler));
                }

                // �li�kili randevular� sil
                if (musteri.Randevular.Any())
                {
                    _context.Randevular.RemoveRange(musteri.Randevular);
                }

                // �li�kili sat��lar� sil
                if (musteri.Satislar.Any())
                {
                    _context.Satislar.RemoveRange(musteri.Satislar);
                }

                // M��teriyi sil
                _context.Musteriler.Remove(musteri);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Müşteri silindi: {musteri.Ad} {musteri.Soyad} (ID: {id})");
                TempData["SuccessMessage"] = $"'{musteri.Ad} {musteri.Soyad}' başarıyla silindi. {musteri.Randevular.Count} randevu ve {musteri.Satislar.Count} satış kaydı da silindi.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Müşteri silinirken hata oluştu: {id}");
                TempData["ErrorMessage"] = "Müşteri silinirken bir hata oluştu: " + ex.Message;
            }

            return RedirectToAction(nameof(AllMusteriler));
        }

        // ====================================
        // M��TER� OLU�TURMA
        // ====================================

        // POST: Admin/CreateMusteri
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateMusteri(string Ad, string Soyad, string Telefon,
            bool? Cinsiyet, int VekarerID)
        {
            try
            {
                // Validate Vekarer exists
                var Vekarer = await _userManager.FindByIdAsync(VekarerID.ToString());
                if (Vekarer == null)
                {
                    TempData["ErrorMessage"] = "Geçersiz Vekarer seçimi.";
                    return RedirectToAction(nameof(AllMusteriler));
                }

                // Create new Musteri
                var musteri = new Musteri
                {
                    Ad = Ad.Trim(),
                    Soyad = Soyad.Trim(),
                    Telefon = Telefon.Trim(),
                    Cinsiyet = Cinsiyet,
                    VekarerID = VekarerID,
                    EklenmeTarihi = DateTime.Now
                };

                _context.Musteriler.Add(musteri);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"? Yeni müşteri oluşturuldu: {Ad} {Soyad} (Admin: {User.Identity.Name})");
                TempData["SuccessMessage"] = $"Müşteri başarıyla oluşturuldu: {Ad} {Soyad}";
                return RedirectToAction(nameof(AllMusteriler));
            }
            catch (Exception ex)
            {
                _logger.LogError($"? Müşteri oluşturma hatası: {ex.Message}");
                TempData["ErrorMessage"] = "Bir hata oluştu. Lütfen tekrar deneyin.";
                return RedirectToAction(nameof(AllMusteriler));
            }
        }

        // ====================================
        // VİSİONER OLU�TURMA
        // ====================================

        // POST: Admin/CreateVekarer
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateVekarer(string Ad, string Soyad, string Email,
            string PhoneNumber, string Sehir, bool Cinsiyet, string Password, string ConfirmPassword)
        {
            try
            {
                // Validate passwords match
                if (Password != ConfirmPassword)
                {
                    TempData["ErrorMessage"] = "Şifreler eşleşmiyor.";
                    return RedirectToAction(nameof(AllVekarers));
                }

                // Check if email already exists
                var existingUser = await _userManager.FindByEmailAsync(Email);
                if (existingUser != null)
                {
                    TempData["ErrorMessage"] = "Bu email adresi zaten kullanılıyor.";
                    return RedirectToAction(nameof(AllVekarers));
                }

                // Create new AppUser
                var newUser = new AppUser
                {
                    UserName = Email,
                    Email = Email,
                    Ad = Ad.Trim(),
                    Soyad = Soyad.Trim(),
                    PhoneNumber = PhoneNumber,
                    Sehir = Sehir.Trim(),
                    Cinsiyet = Cinsiyet,
                    Kvkk = true,
                    Pazarlama = false,
                    EmailConfirmed = true
                };

                // Create user with password
                var result = await _userManager.CreateAsync(newUser, Password);

                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    TempData["ErrorMessage"] = $"Vekarer oluşturulamadı: {errors}";
                    return RedirectToAction(nameof(AllVekarers));
                }

                // Add to Vekarer role
                await _userManager.AddToRoleAsync(newUser, "Vekarer");

                _logger.LogInformation($"? Yeni Vekarer oluşturuldu: {newUser.Email} (Admin: {User.Identity.Name})");
                TempData["SuccessMessage"] = $"Vekarer başarıyla oluşturuldu: {Ad} {Soyad}";
                return RedirectToAction(nameof(AllVekarers));
            }
            catch (Exception ex)
            {
                _logger.LogError($"? Vekarer oluşturma hatası: {ex.Message}");
                TempData["ErrorMessage"] = "Bir hata oluştu. Lütfen tekrar deneyin.";
                return RedirectToAction(nameof(AllVekarers));
            }
        }

        // POST: Admin/DeactivateVekarer
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeactivateVekarer(int id)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(id.ToString());
                if (user == null)
                {
                    TempData["ErrorMessage"] = "Kullanıcı bulunamadı.";
                    return RedirectToAction(nameof(AllVekarers));
                }

                // Lock the user account indefinitely
                user.LockoutEnabled = true;
                user.LockoutEnd = DateTimeOffset.MaxValue;

                var result = await _userManager.UpdateAsync(user);

                if (result.Succeeded)
                {
                    _logger.LogWarning($"?? Vekarer hesabı deaktif edildi: {user.Email} (Admin: {User.Identity.Name})");
                    TempData["SuccessMessage"] = $"{user.Ad} {user.Soyad} hesabı başarıyla deaktif edildi.";
                }
                else
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    TempData["ErrorMessage"] = $"Deaktif etme başarısız: {errors}";
                }

                return RedirectToAction(nameof(AllVekarers));
            }
            catch (Exception ex)
            {
                _logger.LogError($"? Vekarer deaktif etme hatası: {ex.Message}");
                TempData["ErrorMessage"] = "Bir hata oluştu. Lütfen tekrar deneyin.";
                return RedirectToAction(nameof(AllVekarers));
            }
        }

        // POST: Admin/ReactivateVekarer
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReactivateVekarer(int id)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(id.ToString());
                if (user == null)
                {
                    TempData["ErrorMessage"] = "Kullanıcı bulunamadı.";
                    return RedirectToAction(nameof(AllVekarers));
                }

                // Unlock the user account
                user.LockoutEnd = null;

                var result = await _userManager.UpdateAsync(user);

                if (result.Succeeded)
                {
                    _logger.LogInformation($"? Vekarer hesabı aktifleştirildi: {user.Email} (Admin: {User.Identity.Name})");
                    TempData["SuccessMessage"] = $"{user.Ad} {user.Soyad} hesabı başarıyla aktifleştirildi.";
                }
                else
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    TempData["ErrorMessage"] = $"Aktifleştirme başarısız: {errors}";
                }

                return RedirectToAction(nameof(AllVekarers));
            }
            catch (Exception ex)
            {
                _logger.LogError($"? Vekarer aktifleştirme hatası: {ex.Message}");
                TempData["ErrorMessage"] = "Bir hata oluştu. Lütfen tekrar deneyin.";
                return RedirectToAction(nameof(AllVekarers));
            }
        }

        // ====================================
        // SEO SETTINGS MANAGEMENT
        // ====================================

        // GET: Admin/SeoSettings
        public async Task<IActionResult> SeoSettings()
        {
            var seoSettings = await _context.SeoSettings
                .OrderBy(s => s.PagePath)
                .ToListAsync();

            return View(seoSettings);
        }

        // GET: Admin/CreateSeoSetting
        public IActionResult CreateSeoSetting()
        {
            return View();
        }

        // POST: Admin/CreateSeoSetting
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateSeoSetting(SeoSetting seoSetting)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(seoSetting.PagePath))
                {
                    TempData["ErrorMessage"] = "Sayfa yolu zorunludur.";
                    return View(seoSetting);
                }

                // PagePath zaten var mı kontrol et
                if (await _context.SeoSettings.AnyAsync(s => s.PagePath == seoSetting.PagePath))
                {
                    TempData["ErrorMessage"] = "Bu sayfa yolu için zaten bir SEO ayarı mevcut.";
                    return View(seoSetting);
                }

                seoSetting.CreatedAt = DateTime.Now;
                _context.SeoSettings.Add(seoSetting);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"📝 Yeni SEO ayarı oluşturuldu: {seoSetting.PagePath} (Admin: {User.Identity.Name})");
                TempData["SuccessMessage"] = "SEO ayarı başarıyla oluşturuldu.";
                return RedirectToAction(nameof(SeoSettings));
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ SEO ayarı oluşturma hatası: {ex.Message}");
                TempData["ErrorMessage"] = "Bir hata oluştu. Lütfen tekrar deneyin.";
                return View(seoSetting);
            }
        }

        // GET: Admin/EditSeoSetting/5
        public async Task<IActionResult> EditSeoSetting(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var seoSetting = await _context.SeoSettings.FindAsync(id);
            if (seoSetting == null)
            {
                return NotFound();
            }

            return View(seoSetting);
        }

        // POST: Admin/EditSeoSetting/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditSeoSetting(int id, SeoSetting seoSetting)
        {
            if (id != seoSetting.Id)
            {
                return NotFound();
            }

            try
            {
                var existingSetting = await _context.SeoSettings.FindAsync(id);
                if (existingSetting == null)
                {
                    return NotFound();
                }

                // PagePath değiştirilmişse, başka bir kayıtta kullanılmadığını kontrol et
                if (existingSetting.PagePath != seoSetting.PagePath)
                {
                    if (await _context.SeoSettings.AnyAsync(s => s.PagePath == seoSetting.PagePath && s.Id != id))
                    {
                        TempData["ErrorMessage"] = "Bu sayfa yolu başka bir SEO ayarında kullanılıyor.";
                        return View(seoSetting);
                    }
                }

                // Update properties
                existingSetting.PagePath = seoSetting.PagePath;
                existingSetting.PageTitle = seoSetting.PageTitle;
                existingSetting.MetaDescription = seoSetting.MetaDescription;
                existingSetting.OgTitle = seoSetting.OgTitle;
                existingSetting.OgDescription = seoSetting.OgDescription;
                existingSetting.OgImage = seoSetting.OgImage;
                existingSetting.OgType = seoSetting.OgType;
                existingSetting.Author = seoSetting.Author;
                existingSetting.Robots = seoSetting.Robots;
                existingSetting.CanonicalUrl = seoSetting.CanonicalUrl;
                existingSetting.TwitterTitle = seoSetting.TwitterTitle;
                existingSetting.TwitterDescription = seoSetting.TwitterDescription;
                existingSetting.IsActive = seoSetting.IsActive;
                existingSetting.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                _logger.LogInformation($"📝 SEO ayarı güncellendi: {seoSetting.PagePath} (Admin: {User.Identity.Name})");
                TempData["SuccessMessage"] = "SEO ayarı başarıyla güncellendi.";
                return RedirectToAction(nameof(SeoSettings));
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ SEO ayarı güncelleme hatası: {ex.Message}");
                TempData["ErrorMessage"] = "Bir hata oluştu. Lütfen tekrar deneyin.";
                return View(seoSetting);
            }
        }

        // GET: Admin/DeleteSeoSetting/5
        public async Task<IActionResult> DeleteSeoSetting(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var seoSetting = await _context.SeoSettings.FindAsync(id);
            if (seoSetting == null)
            {
                return NotFound();
            }

            return View(seoSetting);
        }

        // POST: Admin/DeleteSeoSettingConfirmed/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSeoSettingConfirmed(int id)
        {
            try
            {
                var seoSetting = await _context.SeoSettings.FindAsync(id);
                if (seoSetting == null)
                {
                    return NotFound();
                }

                _context.SeoSettings.Remove(seoSetting);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"🗑️ SEO ayarı silindi: {seoSetting.PagePath} (Admin: {User.Identity.Name})");
                TempData["SuccessMessage"] = "SEO ayarı başarıyla silindi.";
                return RedirectToAction(nameof(SeoSettings));
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ SEO ayarı silme hatası: {ex.Message}");
                TempData["ErrorMessage"] = "Bir hata oluştu. Lütfen tekrar deneyin.";
                return RedirectToAction(nameof(SeoSettings));
            }
        }
    }
}
