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

            // Sadece kendi randevular�n� g�ster (admin hari�)
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

            // Sadece kendi m��terilerini g�ster (admin hari�)
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

            // Tarih ve saati birleştir ve parse et (ModelState kontrolünden ÖNCE)
            if (!string.IsNullOrWhiteSpace(model.RandevuTarih) && !string.IsNullOrWhiteSpace(model.RandevuSaat))
            {
                if (DateTime.TryParseExact(
                    $"{model.RandevuTarih} {model.RandevuSaat}",
                    "yyyy-MM-dd HH:mm",
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None,
                    out var randevuZaman))
                {
                    model.RandevuZaman = randevuZaman;
                    // ModelState'ten RandevuZaman hatalarını temizle
                    ModelState.Remove(nameof(model.RandevuZaman));
                }
            }

            // RandevuZaman parse edilemedi ise hata ekle
            if (!model.RandevuZaman.HasValue)
            {
                ModelState.AddModelError(nameof(model.RandevuZaman), "Geçerli bir tarih ve saat seçiniz.");
            }

            // ModelState hatalarını logla ve view'a geri dön
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

                // Yeni m��teri mi, mevcut m��teri mi?
                int musteriId;

                // Art�k sadece yeni m��teri ekliyoruz
                model.YeniMusteri = true;

                if (model.YeniMusteri)
                {
                    // Yeni m��teri olu�tur
                    if (string.IsNullOrWhiteSpace(model.YeniMusteriAd) ||
                        string.IsNullOrWhiteSpace(model.YeniMusteriSoyad) ||
                        string.IsNullOrWhiteSpace(model.YeniMusteriTelefon))
                    {
                        ModelState.AddModelError("", "Yeni müşteri için Ad, Soyad ve Telefon zorunludur.");
                        await LoadMusterilerSelectList();
                        return View(model);
                    }

                    // Telefon numaras�n� temizle
                    var cleanPhone = model.YeniMusteriTelefon.Replace("(", "").Replace(")", "").Replace(" ", "").Trim();

                    // Ayn� telefon numaras�na sahip m��teri var m� kontrol et
                    var mevcutMusteri = await _context.Musteriler.FirstOrDefaultAsync(m => m.Telefon == cleanPhone);

                    if (mevcutMusteri != null)
                    {
                        // Mevcut m��teriyi kullan
                        musteriId = mevcutMusteri.IdMusteri;
                        _logger.LogInformation($"? Mevcut müşteri kullanıldı: {musteriId} - {mevcutMusteri.Ad} {mevcutMusteri.Soyad}");
                    }
                    else
                    {
                        // Yeni m��teri olu�tur
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
                        _logger.LogInformation($"? Yeni müşteri oluşturuldu: {musteriId} - {yeniMusteri.Ad} {yeniMusteri.Soyad}");
                    }
                }
                else
                {
                    // Mevcut m��teri se�ilmeli
                    if (!model.MusteriId.HasValue || model.MusteriId.Value == 0)
                    {
                        ModelState.AddModelError("MusteriId", "Lütfen bir müşteri seçin veya yeni müşteri ekleyin.");
                        await LoadMusterilerSelectList();
                        return View(model);
                    }

                    musteriId = model.MusteriId.Value;
                }

                // Randevu tarihini kontrol et
                if (model.RandevuZaman.Value < DateTime.Now)
                {
                    ModelState.AddModelError("RandevuTarih", "Geçmiş tarihli randevu oluşturamazsınız.");
                    await LoadMusterilerSelectList();
                    return View(model);
                }

                // Randevu oluştur
                var randevu = new Randevu
                {
                    MusteriId = musteriId,
                    Aciklama = model.Aciklama,
                    RandevuZaman = model.RandevuZaman.Value,
                    RandevuTipi = model.RandevuTipi,
                    VekarerID = user!.Id,
                    RandevuDurum = RandevuDurum.OnayBekliyor
                };

                _context.Randevular.Add(randevu);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"? Randevu oluşturuldu: {randevu.RandevuID}");

                // M��teri bilgilerini al
                var musteri = await _context.Musteriler.FindAsync(musteriId);

                // E-posta g�nder
                try
                {
                    var emailSubject = "Yeni Randevu Oluşturuldu";
                    var emailBody = $@"
                        <h2>Yeni Randevu Bilgileri</h2>
                        <p><strong>Randevu ID:</strong> {randevu.RandevuID}</p>
                        <p><strong>Vekarer:</strong> {user.Ad} {user.Soyad} ({user.Email})</p>
                        <p><strong>Müşteri:</strong> {musteri?.Ad} {musteri?.Soyad}</p>
                        <p><strong>Telefon:</strong> {musteri?.Telefon}</p>
                        <p><strong>Tarih/Saat:</strong> {randevu.RandevuZaman:dd.MM.yyyy HH:mm}</p>
                        <p><strong>Tip:</strong> {randevu.RandevuTipi}</p>
                        <p><strong>Açıklama:</strong> {randevu.Aciklama ?? "Yok"}</p>
                        <p><strong>Durum:</strong> Bekliyor</p>
                    ";

                    await _emailService.SendEmailAsync("info@dortage.com", emailSubject, emailBody);
                    _logger.LogInformation($"? Randevu bildirimi e-postası gönderildi: {randevu.RandevuID}");
                }
                catch (Exception emailEx)
                {
                    _logger.LogError($"? E-posta gönderme hatası: {emailEx.Message}");
                    // E-posta hatas� randevu olu�turmay� engellemesin
                }

                TempData["SuccessMessage"] = model.YeniMusteri
                    ? "Yeni müşteri ve randevu başarıyla oluşturuldu."
                    : "Randevu başarıyla oluşturuldu.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError($"? Randevu oluşturma hatası: {ex.Message}");
                ModelState.AddModelError("", "Bir hata oluştu. Lütfen tekrar deneyin.");
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
                RandevuTarih = randevu.RandevuZaman.ToString("yyyy-MM-dd"),
                RandevuSaat = randevu.RandevuZaman.ToString("HH:mm"),
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
            // Tarih ve saati birleştir ve parse et (ModelState kontrolünden ÖNCE)
            if (!string.IsNullOrWhiteSpace(model.RandevuTarih) && !string.IsNullOrWhiteSpace(model.RandevuSaat))
            {
                if (DateTime.TryParseExact(
                    $"{model.RandevuTarih} {model.RandevuSaat}",
                    "yyyy-MM-dd HH:mm",
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None,
                    out var randevuZaman))
                {
                    model.RandevuZaman = randevuZaman;
                    // ModelState'ten RandevuZaman hatalarını temizle
                    ModelState.Remove(nameof(model.RandevuZaman));
                }
            }

            // RandevuZaman parse edilemedi ise hata ekle
            if (!model.RandevuZaman.HasValue)
            {
                ModelState.AddModelError(nameof(model.RandevuZaman), "Geçerli bir tarih ve saat seçiniz.");
            }

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

                // Yetki kontrol�
                var user = await _userManager.GetUserAsync(User);
                if (!User.IsInRole("admin") && randevu.VekarerID != user.Id)
                {
                    TempData["ErrorMessage"] = "Bu randevuyu düzenleme yetkiniz yok.";
                    return RedirectToAction(nameof(Index));
                }

                // MusteriId validation (Edit doesn't support creating new customers)
                if (!model.MusteriId.HasValue || model.MusteriId.Value == 0)
                {
                    ModelState.AddModelError("MusteriId", "Lütfen bir müşteri seçin.");
                    await LoadMusterilerSelectList();
                    return View(model);
                }

                randevu.MusteriId = model.MusteriId.Value;
                randevu.Aciklama = model.Aciklama;
                randevu.RandevuZaman = model.RandevuZaman.Value;
                randevu.RandevuTipi = model.RandevuTipi;

                await _context.SaveChangesAsync();

                _logger.LogInformation($"Randevu güncellendi: {id}");
                TempData["SuccessMessage"] = "Randevu başarıyla güncellendi.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError($"Randevu güncelleme hatası: {ex.Message}");
                ModelState.AddModelError("", "Bir hata oluştu. Lütfen tekrar deneyin.");
                await LoadMusterilerSelectList();
                ViewData["RandevuId"] = id;
                return View(model);
            }
        }

        // POST: Randevu/UpdateStatus/5
        // ?? SADECE ADMIN YETK�S� - Randevu durumunu de�i�tirme
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
                    TempData["ErrorMessage"] = "Geçersiz durum değeri.";
                }

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError($"? Randevu durum güncelleme hatası: {ex.Message}");
                TempData["ErrorMessage"] = "Bir hata oluştu. Lütfen tekrar deneyin.";
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

            // Yetki kontrol�
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

                // Yetki kontrol�
                var user = await _userManager.GetUserAsync(User);
                if (!User.IsInRole("admin") && randevu.VekarerID != user.Id)
                {
                    TempData["ErrorMessage"] = "Bu randevuyu silme yetkiniz yok.";
                    return RedirectToAction(nameof(Index));
                }

                _context.Randevular.Remove(randevu);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"? Randevu silindi: {id}");
                TempData["SuccessMessage"] = "Randevu başarıyla silindi.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError($"? Randevu silme hatası: {ex.Message}");
                TempData["ErrorMessage"] = "Bir hata oluştu. Lütfen tekrar deneyin.";
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

        // GET: Randevu/BulMusteriByPhone
        [HttpGet]
        [Authorize(Roles = "Vekarer,admin")]
        public async Task<IActionResult> BulMusteriByPhone(string telefon)
        {
            if (string.IsNullOrWhiteSpace(telefon))
            {
                return Json(new { bulundu = false });
            }

            // Telefon numaras�n� temizle
            var cleanPhone = telefon.Replace("(", "").Replace(")", "").Replace(" ", "").Replace("-", "").Trim();

            // Telefon numaras�yla m��teri ara
            var musteri = await _context.Musteriler.FirstOrDefaultAsync(m => m.Telefon == cleanPhone);

            if (musteri != null)
            {
                return Json(new
                {
                    bulundu = true,
                    ad = musteri.Ad,
                    soyad = musteri.Soyad,
                    cinsiyet = musteri.Cinsiyet,
                    idMusteri = musteri.IdMusteri
                });
            }

            return Json(new { bulundu = false });
        }

        private async Task LoadMusterilerSelectList()
        {
            var user = await _userManager.GetUserAsync(User);

            // Sadece kendi m��terilerini g�ster (admin hari�)
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
