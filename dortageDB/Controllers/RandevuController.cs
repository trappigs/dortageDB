using dortageDB.Data;
using dortageDB.Entities;
using dortageDB.ViewModels;
using dortageDB.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace dortageDB.Controllers
{
    [Authorize(Roles = "Vekarer,admin")]
    public class RandevuController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly ILogger<RandevuController> _logger;
        private readonly IEmailService _emailService;

        public RandevuController(
            AppDbContext context,
            UserManager<AppUser> userManager,
            ILogger<RandevuController> logger,
            IEmailService emailService)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
            _emailService = emailService;
        }

        // GET: Randevu
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);

            var randevular = await _context.Randevular
                .Include(r => r.Musteri)
                .Include(r => r.Vekarer)
                .OrderBy(r => r.RandevuZaman)
                .ToListAsync();

            // Sadece kendi randevularýný göster (admin hariç)
            if (!User.IsInRole("admin"))
            {
                randevular = randevular.Where(r => r.VekarerID == user.Id).ToList();
            }

            return View(randevular);
        }

        // GET: Randevu/Create
        public async Task<IActionResult> Create()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Sadece kendi müþterilerini göster (admin hariç)
            var musteriQuery = User.IsInRole("admin")
                ? _context.Musteriler
                : _context.Musteriler.Where(m => m.VekarerID == user.Id);

            var musteriler = await musteriQuery
                .OrderBy(m => m.Ad)
                .Select(m => new
                {
                    m.IdMusteri,
                    AdSoyad = m.Ad + " " + m.Soyad + " (" + m.Telefon + ")"
                })
                .ToListAsync();

            ViewBag.Musteriler = new SelectList(musteriler, "IdMusteri", "AdSoyad");

            return View();
        }

        // POST: Randevu/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(RandevuCreateVM model)
        {
            _logger.LogInformation("Create POST action called");

            // ModelState hatalarýný logla ve view'a geri dön
            if (!ModelState.IsValid)
            {
                foreach (var modelState in ModelState.Values)
                {
                    foreach (var error in modelState.Errors)
                    {
                        _logger.LogError($"ModelState Error: {error.ErrorMessage}");
                    }
                }
                await LoadMusterilerSelectList();
                return View(model);
            }

            try
            {
                var user = await _userManager.GetUserAsync(User);

                // Yeni müþteri mi, mevcut müþteri mi?
                int musteriId;

                // Artýk sadece yeni müþteri ekliyoruz
                model.YeniMusteri = true;

                if (model.YeniMusteri)
                {
                    // Yeni müþteri oluþtur
                    if (string.IsNullOrWhiteSpace(model.YeniMusteriAd) ||
                        string.IsNullOrWhiteSpace(model.YeniMusteriSoyad) ||
                        string.IsNullOrWhiteSpace(model.YeniMusteriTelefon))
                    {
                        ModelState.AddModelError("", "Yeni müþteri için Ad, Soyad ve Telefon zorunludur.");
                        await LoadMusterilerSelectList();
                        return View(model);
                    }

                    // Telefon numarasýný temizle
                    var cleanPhone = model.YeniMusteriTelefon.Replace("(", "").Replace(")", "").Replace(" ", "").Trim();

                    // Telefon kontrolü
                    var existingPhone = await _context.Musteriler.AnyAsync(m => m.Telefon == cleanPhone);
                    if (existingPhone)
                    {
                        ModelState.AddModelError("YeniMusteriTelefon", "Bu telefon numarasý zaten kayýtlý.");
                        await LoadMusterilerSelectList();
                        return View(model);
                    }

                    var yeniMusteri = new Musteri
                    {
                        Ad = model.YeniMusteriAd.Trim(),
                        Soyad = model.YeniMusteriSoyad.Trim(),
                        Telefon = cleanPhone,
                        Cinsiyet = model.YeniMusteriCinsiyet,
                        TcNo = model.YeniMusteriTcNo?.Trim(),
                        VekarerID = user!.Id
                    };

                    _context.Musteriler.Add(yeniMusteri);
                    await _context.SaveChangesAsync();

                    musteriId = yeniMusteri.IdMusteri;
                    _logger.LogInformation($"? Yeni müþteri oluþturuldu: {musteriId} - {yeniMusteri.Ad} {yeniMusteri.Soyad}");
                }
                else
                {
                    // Mevcut müþteri seçilmeli
                    if (!model.MusteriId.HasValue || model.MusteriId.Value == 0)
                    {
                        ModelState.AddModelError("MusteriId", "Lütfen bir müþteri seçin veya yeni müþteri ekleyin.");
                        await LoadMusterilerSelectList();
                        return View(model);
                    }

                    musteriId = model.MusteriId.Value;
                }

                // Randevu tarihini kontrol et
                if (model.RandevuZaman < DateTime.Now)
                {
                    ModelState.AddModelError("RandevuZaman", "Geçmiþ tarihli randevu oluþturamazsýnýz.");
                    await LoadMusterilerSelectList();
                    return View(model);
                }

                // Randevu oluþtur
                var randevu = new Randevu
                {
                    MusteriId = musteriId,
                    Aciklama = model.Aciklama,
                    RandevuZaman = model.RandevuZaman,
                    RandevuTipi = model.RandevuTipi,
                    VekarerID = user!.Id,
                    RandevuDurum = RandevuDurum.OnayBekliyor
                };

                _context.Randevular.Add(randevu);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"? Randevu oluþturuldu: {randevu.RandevuID}");

                // Müþteri bilgilerini al
                var musteri = await _context.Musteriler.FindAsync(musteriId);

                // E-posta gönder
                try
                {
                    var emailSubject = "Yeni Randevu Oluþturuldu";
                    var emailBody = $@"
                        <h2>Yeni Randevu Bilgileri</h2>
                        <p><strong>Randevu ID:</strong> {randevu.RandevuID}</p>
                        <p><strong>Vekarer:</strong> {user.Ad} {user.Soyad} ({user.Email})</p>
                        <p><strong>Müþteri:</strong> {musteri?.Ad} {musteri?.Soyad}</p>
                        <p><strong>Telefon:</strong> {musteri?.Telefon}</p>
                        <p><strong>Tarih/Saat:</strong> {randevu.RandevuZaman:dd.MM.yyyy HH:mm}</p>
                        <p><strong>Tip:</strong> {randevu.RandevuTipi}</p>
                        <p><strong>Açýklama:</strong> {randevu.Aciklama ?? "Yok"}</p>
                        <p><strong>Durum:</strong> Bekliyor</p>
                    ";

                    await _emailService.SendEmailAsync("info@dortage.com", emailSubject, emailBody);
                    _logger.LogInformation($"? Randevu bildirimi e-postasý gönderildi: {randevu.RandevuID}");
                }
                catch (Exception emailEx)
                {
                    _logger.LogError($"? E-posta gönderme hatasý: {emailEx.Message}");
                    // E-posta hatasý randevu oluþturmayý engellemesin
                }

                TempData["SuccessMessage"] = model.YeniMusteri
                    ? "Yeni müþteri ve randevu baþarýyla oluþturuldu."
                    : "Randevu baþarýyla oluþturuldu.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError($"? Randevu oluþturma hatasý: {ex.Message}");
                ModelState.AddModelError("", "Bir hata oluþtu. Lütfen tekrar deneyin.");
                await LoadMusterilerSelectList();
                return View(model);
            }
        }

        // GET: Randevu/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var randevu = await _context.Randevular.FindAsync(id);
            if (randevu == null)
            {
                return NotFound();
            }

            // Sadece kendi randevusunu düzenleyebilir (admin hariç)
            var user = await _userManager.GetUserAsync(User);
            if (!User.IsInRole("admin") && randevu.VekarerID != user.Id)
            {
                TempData["ErrorMessage"] = "Bu randevuyu düzenleme yetkiniz yok.";
                return RedirectToAction(nameof(Index));
            }

            var model = new RandevuCreateVM
            {
                MusteriId = randevu.MusteriId,
                Aciklama = randevu.Aciklama,
                RandevuZaman = randevu.RandevuZaman,
                RandevuTipi = randevu.RandevuTipi,
                VekarerID = randevu.VekarerID
            };

            await LoadMusterilerSelectList();
            ViewData["RandevuId"] = id;
            ViewData["RandevuDurum"] = randevu.RandevuDurum;
            return View(model);
        }

        // POST: Randevu/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, RandevuCreateVM model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    await LoadMusterilerSelectList();
                    ViewData["RandevuId"] = id;
                    return View(model);
                }

                var randevu = await _context.Randevular.FindAsync(id);
                if (randevu == null)
                {
                    return NotFound();
                }

                // Yetki kontrolü
                var user = await _userManager.GetUserAsync(User);
                if (!User.IsInRole("admin") && randevu.VekarerID != user.Id)
                {
                    TempData["ErrorMessage"] = "Bu randevuyu düzenleme yetkiniz yok.";
                    return RedirectToAction(nameof(Index));
                }

                // MusteriId validation (Edit doesn't support creating new customers)
                if (!model.MusteriId.HasValue || model.MusteriId.Value == 0)
                {
                    ModelState.AddModelError("MusteriId", "Lütfen bir müþteri seçin.");
                    await LoadMusterilerSelectList();
                    return View(model);
                }

                randevu.MusteriId = model.MusteriId.Value;
                randevu.Aciklama = model.Aciklama;
                randevu.RandevuZaman = model.RandevuZaman;
                randevu.RandevuTipi = model.RandevuTipi;

                await _context.SaveChangesAsync();

                _logger.LogInformation($"? Randevu güncellendi: {id}");
                TempData["SuccessMessage"] = "Randevu baþarýyla güncellendi.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError($"? Randevu güncelleme hatasý: {ex.Message}");
                ModelState.AddModelError("", "Bir hata oluþtu. Lütfen tekrar deneyin.");
                await LoadMusterilerSelectList();
                ViewData["RandevuId"] = id;
                return View(model);
            }
        }

        // POST: Randevu/UpdateStatus/5
        // ?? SADECE ADMIN YETKÝSÝ - Randevu durumunu deðiþtirme
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "admin")] // ?? SADECE ADMIN
        public async Task<IActionResult> UpdateStatus(int id, string status)
        {
            try
            {
                var randevu = await _context.Randevular.FindAsync(id);
                if (randevu == null)
                {
                    return NotFound();
                }

                if (Enum.TryParse<RandevuDurum>(status, out var durum))
                {
                    randevu.RandevuDurum = durum;
                    await _context.SaveChangesAsync();

                    _logger.LogInformation($"? Randevu durumu güncellendi: {id} -> {status} (Admin: {User.Identity.Name})");
                    TempData["SuccessMessage"] = "Randevu durumu güncellendi.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Geçersiz durum deðeri.";
                }

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError($"? Randevu durum güncelleme hatasý: {ex.Message}");
                TempData["ErrorMessage"] = "Bir hata oluþtu. Lütfen tekrar deneyin.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Randevu/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var randevu = await _context.Randevular
                .Include(r => r.Musteri)
                .Include(r => r.Vekarer)
                .FirstOrDefaultAsync(r => r.RandevuID == id);

            if (randevu == null)
            {
                return NotFound();
            }

            // Yetki kontrolü
            var user = await _userManager.GetUserAsync(User);
            if (!User.IsInRole("admin") && randevu.VekarerID != user.Id)
            {
                TempData["ErrorMessage"] = "Bu randevuyu silme yetkiniz yok.";
                return RedirectToAction(nameof(Index));
            }

            return View(randevu);
        }

        // POST: Randevu/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var randevu = await _context.Randevular.FindAsync(id);
                if (randevu == null)
                {
                    return NotFound();
                }

                // Yetki kontrolü
                var user = await _userManager.GetUserAsync(User);
                if (!User.IsInRole("admin") && randevu.VekarerID != user.Id)
                {
                    TempData["ErrorMessage"] = "Bu randevuyu silme yetkiniz yok.";
                    return RedirectToAction(nameof(Index));
                }

                _context.Randevular.Remove(randevu);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"? Randevu silindi: {id}");
                TempData["SuccessMessage"] = "Randevu baþarýyla silindi.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError($"? Randevu silme hatasý: {ex.Message}");
                TempData["ErrorMessage"] = "Bir hata oluþtu. Lütfen tekrar deneyin.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Randevu/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var randevu = await _context.Randevular
                .Include(r => r.Musteri)
                .Include(r => r.Vekarer)
                .FirstOrDefaultAsync(r => r.RandevuID == id);

            if (randevu == null)
            {
                return NotFound();
            }

            return View(randevu);
        }

        private async Task LoadMusterilerSelectList()
        {
            var user = await _userManager.GetUserAsync(User);

            // Sadece kendi müþterilerini göster (admin hariç)
            var musteriQuery = User.IsInRole("admin")
                ? _context.Musteriler
                : _context.Musteriler.Where(m => m.VekarerID == user!.Id);

            var musteriler = await musteriQuery
                .OrderBy(m => m.Ad)
                .Select(m => new
                {
                    m.IdMusteri,
                    AdSoyad = m.Ad + " " + m.Soyad + " (" + m.Telefon + ")"
                })
                .ToListAsync();

            ViewBag.Musteriler = new SelectList(musteriler, "IdMusteri", "AdSoyad");
        }
    }
}
