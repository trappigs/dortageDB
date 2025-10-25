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
                TotalTopraktars = await _userManager.GetUsersInRoleAsync("topraktar"),
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
                    TempData["ErrorMessage"] = "Referans kodu bo� olamaz.";
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

                _logger.LogInformation($"? Referans kodu olu�turuldu: {code} (Admin: {User.Identity.Name})");
                TempData["SuccessMessage"] = "Referans kodu ba�ar�yla olu�turuldu.";
                return RedirectToAction(nameof(Referrals));
            }
            catch (Exception ex)
            {
                _logger.LogError($"? Referans kodu olu�turma hatas�: {ex.Message}");
                TempData["ErrorMessage"] = "Bir hata olu�tu. L�tfen tekrar deneyin.";
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
                    TempData["ErrorMessage"] = "Referans kodu bulunamad�.";
                    return RedirectToAction(nameof(Referrals));
                }

                referral.IsActive = !referral.IsActive;
                await _context.SaveChangesAsync();

                _logger.LogInformation($"? Referans kodu durumu de�i�tirildi: {referral.Code} -> {referral.IsActive}");
                TempData["SuccessMessage"] = $"Referans kodu {(referral.IsActive ? "aktif" : "pasif")} edildi.";
                return RedirectToAction(nameof(Referrals));
            }
            catch (Exception ex)
            {
                _logger.LogError($"? Referans kodu durum de�i�tirme hatas�: {ex.Message}");
                TempData["ErrorMessage"] = "Bir hata olu�tu. L�tfen tekrar deneyin.";
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
                    TempData["ErrorMessage"] = "Referans kodu bulunamad�.";
                    return RedirectToAction(nameof(Referrals));
                }

                _context.Referrals.Remove(referral);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"? Referans kodu silindi: {referral.Code}");
                TempData["SuccessMessage"] = "Referans kodu ba�ar�yla silindi.";
                return RedirectToAction(nameof(Referrals));
            }
            catch (Exception ex)
            {
                _logger.LogError($"? Referans kodu silme hatas�: {ex.Message}");
                TempData["ErrorMessage"] = "Bir hata olu�tu. L�tfen tekrar deneyin.";
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
                .Include(m => m.Topraktar)
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

        // GET: Admin/AllSatislar - G�NCELLEME
        public async Task<IActionResult> AllSatislar()
        {
            var satislar = await _context.Satislar
                .Include(s => s.Musteri)
                .Include(s => s.Topraktar)
                .OrderByDescending(s => s.SatilmaTarihi)
                .ToListAsync();

            // Dropdown'lar i�in veri haz�rla
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
                    TempData["ErrorMessage"] = "Ge�ersiz veri giri�i.";
                    return RedirectToAction(nameof(AllSatislar));
                }

                // M��teri ve Topraktar kontrol�
                var musteri = await _context.Musteriler.FindAsync(SatilanMusteriID);
                var topraktar = await _userManager.FindByIdAsync(TopraktarID.ToString());

                if (musteri == null || topraktar == null)
                {
                    TempData["ErrorMessage"] = "Ge�ersiz m��teri veya topraktar.";
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

                _logger.LogInformation($"? Yeni sat�� olu�turuldu: #{satis.SatisID} - ?{satis.ToplamSatisFiyati} (Admin: {User.Identity.Name})");
                TempData["SuccessMessage"] = "Sat�� ba�ar�yla eklendi.";
                return RedirectToAction(nameof(AllSatislar));
            }
            catch (Exception ex)
            {
                _logger.LogError($"? Sat�� olu�turma hatas�: {ex.Message}");
                TempData["ErrorMessage"] = "Bir hata olu�tu. L�tfen tekrar deneyin.";
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
                    TempData["ErrorMessage"] = "Sat�� bulunamad�.";
                    return RedirectToAction(nameof(AllSatislar));
                }

                // Validate inputs
                if (string.IsNullOrWhiteSpace(Bolge) || ToplamSatisFiyati <= 0 || OdenecekKomisyon < 0)
                {
                    TempData["ErrorMessage"] = "Ge�ersiz veri giri�i.";
                    return RedirectToAction(nameof(AllSatislar));
                }

                // M��teri ve Topraktar kontrol�
                var musteri = await _context.Musteriler.FindAsync(SatilanMusteriID);
                var topraktar = await _userManager.FindByIdAsync(TopraktarID.ToString());

                if (musteri == null || topraktar == null)
                {
                    TempData["ErrorMessage"] = "Ge�ersiz m��teri veya topraktar.";
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

                _logger.LogInformation($"? Sat�� #{SatisID} g�ncellendi - ?{ToplamSatisFiyati} (Admin: {User.Identity.Name})");
                TempData["SuccessMessage"] = "Sat�� ba�ar�yla g�ncellendi.";
                return RedirectToAction(nameof(AllSatislar));
            }
            catch (Exception ex)
            {
                _logger.LogError($"? Sat�� g�ncelleme hatas�: {ex.Message}");
                TempData["ErrorMessage"] = "Bir hata olu�tu. L�tfen tekrar deneyin.";
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
                    TempData["ErrorMessage"] = "Sat�� bulunamad�.";
                    return RedirectToAction(nameof(AllSatislar));
                }

                var satisInfo = $"#{satis.SatisID} - ?{satis.ToplamSatisFiyati}";

                _context.Satislar.Remove(satis);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"? Sat�� {satisInfo} silindi (Admin: {User.Identity.Name})");
                TempData["SuccessMessage"] = "Sat�� ba�ar�yla silindi.";
                return RedirectToAction(nameof(AllSatislar));
            }
            catch (Exception ex)
            {
                _logger.LogError($"? Sat�� silme hatas�: {ex.Message}");
                TempData["ErrorMessage"] = "Bir hata olu�tu. L�tfen tekrar deneyin.";
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

                // Calculate total sales (ciro)
                var totalCiro = await _context.Satislar
                    .Where(s => s.TopraktarID == topraktar.Id)
                    .SumAsync(s => (decimal?)s.ToplamSatisFiyati) ?? 0;

                // Get TopraktarProfile for ReferralCode and UsedReferralCode
                var profile = await _context.TopraktarProfiles
                    .FirstOrDefaultAsync(p => p.UserId == topraktar.Id);

                topraktarData.Add(new
                {
                    User = topraktar,
                    RandevuCount = randevuCount,
                    SatisCount = satisCount,
                    TotalKomisyon = totalKomisyon,
                    TotalCiro = totalCiro,
                    ReferralCode = profile?.ReferralCode,
                    UsedReferralCode = profile?.UsedReferralCode // Kay�t olurken kulland��� kod
                });
            }

            ViewBag.TopraktarData = topraktarData;
            return View();
        }

        // POST: Admin/CreateRandevu
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateRandevu(int MusteriId, int TopraktarID,
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

                // M��teri ve Topraktar kontrol�
                var musteri = await _context.Musteriler.FindAsync(MusteriId);
                var topraktar = await _userManager.FindByIdAsync(TopraktarID.ToString());

                if (musteri == null || topraktar == null)
                {
                    TempData["ErrorMessage"] = "Ge�ersiz m��teri veya topraktar.";
                    return RedirectToAction(nameof(AllRandevular));
                }

                var randevu = new Randevu
                {
                    MusteriId = MusteriId,
                    TopraktarID = TopraktarID,
                    RandevuZaman = RandevuZaman,
                    RandevuTipi = RandevuTipi.Trim(),
                    Aciklama = string.IsNullOrWhiteSpace(Aciklama) ? null : Aciklama.Trim(),
                    RandevuDurum = RandevuDurum.OnayBekliyor
                };

                _context.Randevular.Add(randevu);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"? Yeni randevu olu�turuldu: #{randevu.RandevuID} (Admin: {User.Identity.Name})");
                TempData["SuccessMessage"] = "Randevu ba�ar�yla eklendi.";
                return RedirectToAction(nameof(AllRandevular));
            }
            catch (Exception ex)
            {
                _logger.LogError($"? Randevu olu�turma hatas�: {ex.Message}");
                TempData["ErrorMessage"] = "Bir hata olu�tu. L�tfen tekrar deneyin.";
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
                    TempData["ErrorMessage"] = "Randevu bulunamad�.";
                    return RedirectToAction(nameof(AllRandevular));
                }

                // Parse status to enum
                if (Enum.TryParse<RandevuDurum>(status, true, out var randevuDurum))
                {
                    randevu.RandevuDurum = randevuDurum;
                    await _context.SaveChangesAsync();

                    _logger.LogInformation($"? Randevu #{id} durumu g�ncellendi: {randevuDurum} (Admin: {User.Identity.Name})");
                    TempData["SuccessMessage"] = "Randevu durumu ba�ar�yla g�ncellendi.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Ge�ersiz durum de�eri.";
                }

                return RedirectToAction(nameof(AllRandevular));
            }
            catch (Exception ex)
            {
                _logger.LogError($"? Randevu durum g�ncelleme hatas�: {ex.Message}");
                TempData["ErrorMessage"] = "Bir hata olu�tu. L�tfen tekrar deneyin.";
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
                    TempData["ErrorMessage"] = "Randevu bulunamad�.";
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
                    TempData["ErrorMessage"] = "Ge�ersiz randevu durumu.";
                    return RedirectToAction(nameof(AllRandevular));
                }

                // Update randevu
                randevu.RandevuZaman = randevuZaman;
                randevu.RandevuTipi = randevuTipi.Trim();
                randevu.Aciklama = string.IsNullOrWhiteSpace(aciklama) ? null : aciklama.Trim();
                randevu.RandevuDurum = durum;

                await _context.SaveChangesAsync();

                _logger.LogInformation($"? Randevu #{id} d�zenlendi - Durum: {durum} (Admin: {User.Identity.Name})");
                TempData["SuccessMessage"] = "Randevu ba�ar�yla g�ncellendi.";
                return RedirectToAction(nameof(AllRandevular));
            }
            catch (Exception ex)
            {
                _logger.LogError($"? Randevu d�zenleme hatas�: {ex.Message}");
                TempData["ErrorMessage"] = "Bir hata olu�tu. L�tfen tekrar deneyin.";
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
                    TempData["ErrorMessage"] = "Randevu bulunamad�.";
                    return RedirectToAction(nameof(AllRandevular));
                }

                _context.Randevular.Remove(randevu);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"? Randevu #{id} silindi (Admin: {User.Identity.Name})");
                TempData["SuccessMessage"] = "Randevu ba�ar�yla silindi.";
                return RedirectToAction(nameof(AllRandevular));
            }
            catch (Exception ex)
            {
                _logger.LogError($"? Randevu silme hatas�: {ex.Message}");
                TempData["ErrorMessage"] = "Bir hata olu�tu. L�tfen tekrar deneyin.";
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
                    TempData["ErrorMessage"] = "Proje ad� ve konum zorunludur.";
                    return View(proje);
                }

                proje.KayitTarihi = DateTime.Now;
                proje.AktifMi = true;

                _context.Projeler.Add(proje);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"? Yeni proje olu�turuldu: {proje.ProjeAdi} (Admin: {User.Identity.Name})");
                TempData["SuccessMessage"] = "Proje ba�ar�yla olu�turuldu.";
                return RedirectToAction(nameof(Projeler));
            }
            catch (Exception ex)
            {
                _logger.LogError($"? Proje olu�turma hatas�: {ex.Message}");
                TempData["ErrorMessage"] = "Bir hata olu�tu. L�tfen tekrar deneyin.";
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
                    TempData["ErrorMessage"] = "Proje ad� ve konum zorunludur.";
                    return View(proje);
                }

                var existingProje = await _context.Projeler.FindAsync(id);
                if (existingProje == null)
                {
                    return NotFound();
                }

                // Update properties
                existingProje.ProjeAdi = proje.ProjeAdi;
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
                existingProje.GaleriGorselleri = proje.GaleriGorselleri;
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

                _logger.LogInformation($"? Proje #{id} g�ncellendi: {proje.ProjeAdi} (Admin: {User.Identity.Name})");
                TempData["SuccessMessage"] = "Proje ba�ar�yla g�ncellendi.";
                return RedirectToAction(nameof(Projeler));
            }
            catch (Exception ex)
            {
                _logger.LogError($"? Proje g�ncelleme hatas�: {ex.Message}");
                TempData["ErrorMessage"] = "Bir hata olu�tu. L�tfen tekrar deneyin.";
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
                    TempData["ErrorMessage"] = "Proje bulunamad�.";
                    return RedirectToAction(nameof(Projeler));
                }

                var projeAdi = proje.ProjeAdi;
                _context.Projeler.Remove(proje);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"? Proje silindi: {projeAdi} (Admin: {User.Identity.Name})");
                TempData["SuccessMessage"] = "Proje ba�ar�yla silindi.";
                return RedirectToAction(nameof(Projeler));
            }
            catch (Exception ex)
            {
                _logger.LogError($"? Proje silme hatas�: {ex.Message}");
                TempData["ErrorMessage"] = "Bir hata olu�tu. L�tfen tekrar deneyin.";
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
                    return Json(new { success = false, message = "Dosya se�ilmedi." });
                }

                // Dosya boyutu kontrol� (5MB)
                if (file.Length > 5 * 1024 * 1024)
                {
                    return Json(new { success = false, message = "Dosya boyutu 5MB'dan k���k olmal�d�r." });
                }

                // Dosya t�r� kontrol�
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

                if (!allowedExtensions.Contains(extension))
                {
                    return Json(new { success = false, message = "Sadece resim dosyalar� y�klenebilir (.jpg, .jpeg, .png, .webp, .gif)" });
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

                _logger.LogInformation($"? G�rsel y�klendi: {fileName} (Admin: {User.Identity.Name})");

                return Json(new { success = true, url = fileUrl });
            }
            catch (Exception ex)
            {
                _logger.LogError($"? G�rsel y�kleme hatas�: {ex.Message}");
                return Json(new { success = false, message = "Dosya y�klenirken bir hata olu�tu." });
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
                    return Json(new { success = false, message = "G�rsel URL'si gerekli." });
                }

                // URL'den dosya yolunu ��kar
                if (imageUrl.StartsWith("/uploads/projeler/"))
                {
                    var fileName = Path.GetFileName(imageUrl);
                    var filePath = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "projeler", fileName);

                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                        _logger.LogInformation($"? G�rsel silindi: {fileName} (Admin: {User.Identity.Name})");
                        return Json(new { success = true });
                    }
                }

                return Json(new { success = true }); // Dosya yoksa da success d�nd�r
            }
            catch (Exception ex)
            {
                _logger.LogError($"? G�rsel silme hatas�: {ex.Message}");
                return Json(new { success = false, message = "Dosya silinirken bir hata olu�tu." });
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
                    return Json(new { success = false, message = "Dosya se�ilmedi." });
                }

                // Dosya boyutu kontrol� (10MB)
                if (file.Length > 10 * 1024 * 1024)
                {
                    return Json(new { success = false, message = "Dosya boyutu 10MB'dan k���k olmal�d�r." });
                }

                // Dosya t�r� kontrol�
                var allowedExtensions = new[] { ".pdf", ".ppt", ".pptx" };
                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

                if (!allowedExtensions.Contains(extension))
                {
                    return Json(new { success = false, message = "Sadece PDF veya PowerPoint dosyalar� y�klenebilir (.pdf, .ppt, .pptx)" });
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

                _logger.LogInformation($"? Sunum y�klendi: {fileName} (Admin: {User.Identity.Name})");

                return Json(new { success = true, url = fileUrl });
            }
            catch (Exception ex)
            {
                _logger.LogError($"? Sunum y�kleme hatas�: {ex.Message}");
                return Json(new { success = false, message = "Dosya y�klenirken bir hata olu�tu." });
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
                _logger.LogError($"? Sunum silme hatas�: {ex.Message}");
                return Json(new { success = false, message = "Dosya silinirken bir hata olu�tu." });
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

                _logger.LogInformation($"? E�itim videosu eklendi: {video.Baslik} (Admin: {User.Identity.Name})");
                TempData["SuccessMessage"] = "Video ba�ar�yla eklendi!";
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

                    _logger.LogInformation($"? E�itim videosu g�ncellendi: {video.Baslik} (Admin: {User.Identity.Name})");
                    TempData["SuccessMessage"] = "Video ba�ar�yla g�ncellendi!";
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

                    _logger.LogInformation($"? E�itim videosu silindi: {videoBaslik} (Admin: {User.Identity.Name})");
                    TempData["SuccessMessage"] = "Video ba�ar�yla silindi!";
                }

                return RedirectToAction(nameof(EgitimVideolar));
            }
            catch (Exception ex)
            {
                _logger.LogError($"? Video silme hatas�: {ex.Message}");
                TempData["ErrorMessage"] = "Bir hata olu�tu. L�tfen tekrar deneyin.";
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

            _logger.LogInformation($"? Video durumu de�i�tirildi: {video.Baslik} -> {(video.Aktif ? "Aktif" : "Pasif")} (Admin: {User.Identity.Name})");
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
                    TempData["ErrorMessage"] = "M��teri bulunamad�.";
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

                _logger.LogInformation($"M��teri silindi: {musteri.Ad} {musteri.Soyad} (ID: {id})");
                TempData["SuccessMessage"] = $"'{musteri.Ad} {musteri.Soyad}' ba�ar�yla silindi. {musteri.Randevular.Count} randevu ve {musteri.Satislar.Count} sat�� kayd� da silindi.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"M��teri silinirken hata olu�tu: {id}");
                TempData["ErrorMessage"] = "M��teri silinirken bir hata olu�tu: " + ex.Message;
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
            bool? Cinsiyet, int TopraktarID)
        {
            try
            {
                // Validate topraktar exists
                var topraktar = await _userManager.FindByIdAsync(TopraktarID.ToString());
                if (topraktar == null)
                {
                    TempData["ErrorMessage"] = "Ge�ersiz topraktar se�imi.";
                    return RedirectToAction(nameof(AllMusteriler));
                }

                // Create new Musteri
                var musteri = new Musteri
                {
                    Ad = Ad.Trim(),
                    Soyad = Soyad.Trim(),
                    Telefon = Telefon.Trim(),
                    Cinsiyet = Cinsiyet,
                    TopraktarID = TopraktarID,
                    EklenmeTarihi = DateTime.Now
                };

                _context.Musteriler.Add(musteri);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"? Yeni m��teri olu�turuldu: {Ad} {Soyad} (Admin: {User.Identity.Name})");
                TempData["SuccessMessage"] = $"M��teri ba�ar�yla olu�turuldu: {Ad} {Soyad}";
                return RedirectToAction(nameof(AllMusteriler));
            }
            catch (Exception ex)
            {
                _logger.LogError($"? M��teri olu�turma hatas�: {ex.Message}");
                TempData["ErrorMessage"] = "Bir hata olu�tu. L�tfen tekrar deneyin.";
                return RedirectToAction(nameof(AllMusteriler));
            }
        }

        // ====================================
        // TOPRAKTAR OLU�TURMA
        // ====================================

        // POST: Admin/CreateTopraktar
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateTopraktar(string Ad, string Soyad, string Email,
            string PhoneNumber, string Sehir, bool Cinsiyet, string Password, string ConfirmPassword)
        {
            try
            {
                // Validate passwords match
                if (Password != ConfirmPassword)
                {
                    TempData["ErrorMessage"] = "�ifreler e�le�miyor.";
                    return RedirectToAction(nameof(AllTopraktars));
                }

                // Check if email already exists
                var existingUser = await _userManager.FindByEmailAsync(Email);
                if (existingUser != null)
                {
                    TempData["ErrorMessage"] = "Bu email adresi zaten kullan�l�yor.";
                    return RedirectToAction(nameof(AllTopraktars));
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
                    TempData["ErrorMessage"] = $"Topraktar olu�turulamad�: {errors}";
                    return RedirectToAction(nameof(AllTopraktars));
                }

                // Add to topraktar role
                await _userManager.AddToRoleAsync(newUser, "topraktar");

                _logger.LogInformation($"? Yeni topraktar olu�turuldu: {newUser.Email} (Admin: {User.Identity.Name})");
                TempData["SuccessMessage"] = $"Topraktar ba�ar�yla olu�turuldu: {Ad} {Soyad}";
                return RedirectToAction(nameof(AllTopraktars));
            }
            catch (Exception ex)
            {
                _logger.LogError($"? Topraktar olu�turma hatas�: {ex.Message}");
                TempData["ErrorMessage"] = "Bir hata olu�tu. L�tfen tekrar deneyin.";
                return RedirectToAction(nameof(AllTopraktars));
            }
        }

        // POST: Admin/DeactivateTopraktar
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeactivateTopraktar(int id)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(id.ToString());
                if (user == null)
                {
                    TempData["ErrorMessage"] = "Kullan�c� bulunamad�.";
                    return RedirectToAction(nameof(AllTopraktars));
                }

                // Lock the user account indefinitely
                user.LockoutEnabled = true;
                user.LockoutEnd = DateTimeOffset.MaxValue;

                var result = await _userManager.UpdateAsync(user);

                if (result.Succeeded)
                {
                    _logger.LogWarning($"?? Topraktar hesab� deaktif edildi: {user.Email} (Admin: {User.Identity.Name})");
                    TempData["SuccessMessage"] = $"{user.Ad} {user.Soyad} hesab� ba�ar�yla deaktif edildi.";
                }
                else
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    TempData["ErrorMessage"] = $"Deaktif etme ba�ar�s�z: {errors}";
                }

                return RedirectToAction(nameof(AllTopraktars));
            }
            catch (Exception ex)
            {
                _logger.LogError($"? Topraktar deaktif etme hatas�: {ex.Message}");
                TempData["ErrorMessage"] = "Bir hata olu�tu. L�tfen tekrar deneyin.";
                return RedirectToAction(nameof(AllTopraktars));
            }
        }

        // POST: Admin/ReactivateTopraktar
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReactivateTopraktar(int id)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(id.ToString());
                if (user == null)
                {
                    TempData["ErrorMessage"] = "Kullan�c� bulunamad�.";
                    return RedirectToAction(nameof(AllTopraktars));
                }

                // Unlock the user account
                user.LockoutEnd = null;

                var result = await _userManager.UpdateAsync(user);

                if (result.Succeeded)
                {
                    _logger.LogInformation($"? Topraktar hesab� aktifle�tirildi: {user.Email} (Admin: {User.Identity.Name})");
                    TempData["SuccessMessage"] = $"{user.Ad} {user.Soyad} hesab� ba�ar�yla aktifle�tirildi.";
                }
                else
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    TempData["ErrorMessage"] = $"Aktifle�tirme ba�ar�s�z: {errors}";
                }

                return RedirectToAction(nameof(AllTopraktars));
            }
            catch (Exception ex)
            {
                _logger.LogError($"? Topraktar aktifle�tirme hatas�: {ex.Message}");
                TempData["ErrorMessage"] = "Bir hata olu�tu. L�tfen tekrar deneyin.";
                return RedirectToAction(nameof(AllTopraktars));
            }
        }
    }
}
