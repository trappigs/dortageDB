using dortageDB.Data;
using dortageDB.Entities;
using dortageDB.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace dortageDB.Controllers
{
    [Authorize(Roles = "topraktar,admin")]
    public class RandevuController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly ILogger<RandevuController> _logger;

        public RandevuController(
            AppDbContext context,
            UserManager<AppUser> userManager,
            ILogger<RandevuController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        // GET: Randevu
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);

            var randevular = await _context.Randevular
                .Include(r => r.Musteri)
                .Include(r => r.Topraktar)
                .OrderBy(r => r.RandevuZaman)
                .ToListAsync();

            // Sadece kendi randevularını göster (admin hariç)
            if (!User.IsInRole("admin"))
            {
                randevular = randevular.Where(r => r.TopraktarID == user.Id).ToList();
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

            // Sadece kendi müşterilerini göster (admin hariç)
            var musteriQuery = User.IsInRole("admin")
                ? _context.Musteriler
                : _context.Musteriler.Where(m => m.TopraktarID == user.Id);

            var musteriler = await musteriQuery
                .OrderBy(m => m.Ad)
                .Select(m => new
                {
                    m.IdMusteri,
                    AdSoyad = m.Ad + " " + m.Soyad + " (" + m.Telefon + ")"
                })
                .ToListAsync();

            ViewBag.Musteriler = new SelectList(musteriler, "IdMusteri", "AdSoyad");

            // Bölge listesi
            ViewBag.Bolgeler = new SelectList(new[]
            {
                "Adana", "Adıyaman", "Afyonkarahisar", "Ağrı", "Aksaray", "Amasya", "Ankara", "Antalya",
                "Ardahan", "Artvin", "Aydın", "Balıkesir", "Bartın", "Batman", "Bayburt", "Bilecik",
                "Bingöl", "Bitlis", "Bolu", "Burdur", "Bursa", "Çanakkale", "Çankırı", "Çorum",
                "Denizli", "Diyarbakır", "Düzce", "Edirne", "Elazığ", "Erzincan", "Erzurum", "Eskişehir",
                "Gaziantep", "Giresun", "Gümüşhane", "Hakkari", "Hatay", "Iğdır", "Isparta", "İstanbul",
                "İzmir", "Kahramanmaraş", "Karabük", "Karaman", "Kars", "Kastamonu", "Kayseri", "Kilis",
                "Kırıkkale", "Kırklareli", "Kırşehir", "Kocaeli", "Konya", "Kütahya", "Malatya", "Manisa",
                "Mardin", "Mersin", "Muğla", "Muş", "Nevşehir", "Niğde", "Ordu", "Osmaniye",
                "Rize", "Sakarya", "Samsun", "Şanlıurfa", "Siirt", "Sinop", "Şırnak", "Sivas",
                "Tekirdağ", "Tokat", "Trabzon", "Tunceli", "Uşak", "Van", "Yalova", "Yozgat", "Zonguldak"
            });

            return View();
        }

        // POST: Randevu/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(RandevuCreateVM model)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);

                // Yeni müşteri mi, mevcut müşteri mi?
                int musteriId;

                if (model.YeniMusteri)
                {
                    // Yeni müşteri oluştur
                    if (string.IsNullOrWhiteSpace(model.YeniMusteriAd) ||
                        string.IsNullOrWhiteSpace(model.YeniMusteriSoyad) ||
                        string.IsNullOrWhiteSpace(model.YeniMusteriTelefon) ||
                        string.IsNullOrWhiteSpace(model.YeniMusteriSehir))
                    {
                        ModelState.AddModelError("", "Yeni müşteri için Ad, Soyad, Telefon ve Şehir zorunludur.");
                        await LoadMusterilerSelectList();
                        return View(model);
                    }

                    // Telefon numarasını temizle
                    var cleanPhone = model.YeniMusteriTelefon.Replace("(", "").Replace(")", "").Replace(" ", "").Trim();

                    // Telefon kontrolü
                    var existingPhone = await _context.Musteriler.AnyAsync(m => m.Telefon == cleanPhone);
                    if (existingPhone)
                    {
                        ModelState.AddModelError("YeniMusteriTelefon", "Bu telefon numarası zaten kayıtlı.");
                        await LoadMusterilerSelectList();
                        return View(model);
                    }

                    var yeniMusteri = new Musteri
                    {
                        Ad = model.YeniMusteriAd.Trim(),
                        Soyad = model.YeniMusteriSoyad.Trim(),
                        Telefon = cleanPhone,
                        Eposta = model.YeniMusteriEposta?.Trim(),
                        Sehir = model.YeniMusteriSehir.Trim(),
                        Cinsiyet = model.YeniMusteriCinsiyet ?? false,
                        TcNo = model.YeniMusteriTcNo?.Trim(),
                        TopraktarID = user!.Id
                    };

                    _context.Musteriler.Add(yeniMusteri);
                    await _context.SaveChangesAsync();

                    musteriId = yeniMusteri.IdMusteri;
                    _logger.LogInformation($"✅ Yeni müşteri oluşturuldu: {musteriId} - {yeniMusteri.Ad} {yeniMusteri.Soyad}");
                }
                else
                {
                    // Mevcut müşteri seçilmeli
                    if (!model.MusteriId.HasValue || model.MusteriId.Value == 0)
                    {
                        ModelState.AddModelError("MusteriId", "Lütfen bir müşteri seçin veya yeni müşteri ekleyin.");
                        await LoadMusterilerSelectList();
                        return View(model);
                    }

                    musteriId = model.MusteriId.Value;
                }

                // Randevu tarihini kontrol et
                if (model.RandevuZaman < DateTime.Now)
                {
                    ModelState.AddModelError("RandevuZaman", "Geçmiş tarihli randevu oluşturamazsınız.");
                    await LoadMusterilerSelectList();
                    return View(model);
                }

                // Randevu oluştur
                var randevu = new Randevu
                {
                    MusteriId = musteriId,
                    Bolge = model.Bolge,
                    Aciklama = model.Aciklama,
                    RandevuZaman = model.RandevuZaman,
                    RandevuTipi = model.RandevuTipi,
                    TopraktarID = user!.Id,
                    RandevuDurum = RandevuDurum.pending
                };

                _context.Randevular.Add(randevu);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"✅ Randevu oluşturuldu: {randevu.RandevuID}");
                TempData["SuccessMessage"] = model.YeniMusteri
                    ? "Yeni müşteri ve randevu başarıyla oluşturuldu."
                    : "Randevu başarıyla oluşturuldu.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ Randevu oluşturma hatası: {ex.Message}");
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
            if (!User.IsInRole("admin") && randevu.TopraktarID != user.Id)
            {
                TempData["ErrorMessage"] = "Bu randevuyu düzenleme yetkiniz yok.";
                return RedirectToAction(nameof(Index));
            }

            var model = new RandevuCreateVM
            {
                MusteriId = randevu.MusteriId,
                Bolge = randevu.Bolge,
                Aciklama = randevu.Aciklama,
                RandevuZaman = randevu.RandevuZaman,
                RandevuTipi = randevu.RandevuTipi,
                TopraktarID = randevu.TopraktarID
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
                if (!User.IsInRole("admin") && randevu.TopraktarID != user.Id)
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
                randevu.Bolge = model.Bolge;
                randevu.Aciklama = model.Aciklama;
                randevu.RandevuZaman = model.RandevuZaman;
                randevu.RandevuTipi = model.RandevuTipi;

                await _context.SaveChangesAsync();

                _logger.LogInformation($"✅ Randevu güncellendi: {id}");
                TempData["SuccessMessage"] = "Randevu başarıyla güncellendi.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ Randevu güncelleme hatası: {ex.Message}");
                ModelState.AddModelError("", "Bir hata oluştu. Lütfen tekrar deneyin.");
                await LoadMusterilerSelectList();
                ViewData["RandevuId"] = id;
                return View(model);
            }
        }

        // POST: Randevu/UpdateStatus/5
        // ⚠️ SADECE ADMIN YETKİSİ - Randevu durumunu değiştirme
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "admin")] // 🔒 SADECE ADMIN
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

                    _logger.LogInformation($"✅ Randevu durumu güncellendi: {id} -> {status} (Admin: {User.Identity.Name})");
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
                _logger.LogError($"❌ Randevu durum güncelleme hatası: {ex.Message}");
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
                .Include(r => r.Topraktar)
                .FirstOrDefaultAsync(r => r.RandevuID == id);

            if (randevu == null)
            {
                return NotFound();
            }

            // Yetki kontrolü
            var user = await _userManager.GetUserAsync(User);
            if (!User.IsInRole("admin") && randevu.TopraktarID != user.Id)
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
                if (!User.IsInRole("admin") && randevu.TopraktarID != user.Id)
                {
                    TempData["ErrorMessage"] = "Bu randevuyu silme yetkiniz yok.";
                    return RedirectToAction(nameof(Index));
                }

                _context.Randevular.Remove(randevu);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"✅ Randevu silindi: {id}");
                TempData["SuccessMessage"] = "Randevu başarıyla silindi.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ Randevu silme hatası: {ex.Message}");
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
                .Include(r => r.Topraktar)
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

            // Sadece kendi müşterilerini göster (admin hariç)
            var musteriQuery = User.IsInRole("admin")
                ? _context.Musteriler
                : _context.Musteriler.Where(m => m.TopraktarID == user!.Id);

            var musteriler = await musteriQuery
                .OrderBy(m => m.Ad)
                .Select(m => new
                {
                    m.IdMusteri,
                    AdSoyad = m.Ad + " " + m.Soyad + " (" + m.Telefon + ")"
                })
                .ToListAsync();

            ViewBag.Musteriler = new SelectList(musteriler, "IdMusteri", "AdSoyad");

            // Bölge listesi
            ViewBag.Bolgeler = new SelectList(new[]
            {
                "Adana", "Adıyaman", "Afyonkarahisar", "Ağrı", "Aksaray", "Amasya", "Ankara", "Antalya",
                "Ardahan", "Artvin", "Aydın", "Balıkesir", "Bartın", "Batman", "Bayburt", "Bilecik",
                "Bingöl", "Bitlis", "Bolu", "Burdur", "Bursa", "Çanakkale", "Çankırı", "Çorum",
                "Denizli", "Diyarbakır", "Düzce", "Edirne", "Elazığ", "Erzincan", "Erzurum", "Eskişehir",
                "Gaziantep", "Giresun", "Gümüşhane", "Hakkari", "Hatay", "Iğdır", "Isparta", "İstanbul",
                "İzmir", "Kahramanmaraş", "Karabük", "Karaman", "Kars", "Kastamonu", "Kayseri", "Kilis",
                "Kırıkkale", "Kırklareli", "Kırşehir", "Kocaeli", "Konya", "Kütahya", "Malatya", "Manisa",
                "Mardin", "Mersin", "Muğla", "Muş", "Nevşehir", "Niğde", "Ordu", "Osmaniye",
                "Rize", "Sakarya", "Samsun", "Şanlıurfa", "Siirt", "Sinop", "Şırnak", "Sivas",
                "Tekirdağ", "Tokat", "Trabzon", "Tunceli", "Uşak", "Van", "Yalova", "Yozgat", "Zonguldak"
            });
        }
    }
}