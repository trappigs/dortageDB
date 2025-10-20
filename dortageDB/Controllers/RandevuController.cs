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
            var musteriler = await _context.Musteriler
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
            try
            {
                if (!ModelState.IsValid)
                {
                    await LoadMusterilerSelectList();
                    return View(model);
                }

                var user = await _userManager.GetUserAsync(User);

                // Randevu tarihini kontrol et
                if (model.RandevuZaman < DateTime.Now)
                {
                    ModelState.AddModelError("RandevuZaman", "Geçmiş tarihli randevu oluşturamazsınız.");
                    await LoadMusterilerSelectList();
                    return View(model);
                }

                var randevu = new Randevu
                {
                    MusteriId = model.MusteriId,
                    Bolge = model.Bolge,
                    RandevuZaman = model.RandevuZaman,
                    RandevuTipi = model.RandevuTipi,
                    TopraktarID = user.Id,
                    RandevuDurum = RandevuDurum.pending
                };

                _context.Randevular.Add(randevu);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"✅ Randevu oluşturuldu: {randevu.RandevuID}");
                TempData["SuccessMessage"] = "Randevu başarıyla oluşturuldu.";
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

                randevu.MusteriId = model.MusteriId;
                randevu.Bolge = model.Bolge;
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
            var musteriler = await _context.Musteriler
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