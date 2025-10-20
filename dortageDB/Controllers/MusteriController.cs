using dortageDB.Data;
using dortageDB.Entities;
using dortageDB.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace dortageDB.Controllers
{
    [Authorize(Roles = "topraktar,admin")]
    public class MusteriController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly ILogger<MusteriController> _logger;

        public MusteriController(
            AppDbContext context,
            UserManager<AppUser> userManager,
            ILogger<MusteriController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        // GET: Musteri
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            var musteriler = await _context.Musteriler
                .OrderByDescending(m => m.IdMusteri)
                .ToListAsync();

            return View(musteriler);
        }

        // GET: Musteri/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Musteri/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(MusteriCreateVM model)
        {
            try
            {
                _logger.LogInformation("=== MÜŞTERİ OLUŞTURMA BAŞLADI ===");
                _logger.LogInformation($"Ad: {model.Ad}, Soyad: {model.Soyad}");

                if (!ModelState.IsValid)
                {
                    return View(model);
                }

                // Telefon numarasını temizle
                var cleanPhone = model.Telefon.Replace("(", "").Replace(")", "").Replace(" ", "").Trim();

                // Telefon kontrolü
                var existingPhone = await _context.Musteriler
                    .AnyAsync(m => m.Telefon == cleanPhone);

                if (existingPhone)
                {
                    ModelState.AddModelError("Telefon", "Bu telefon numarası zaten kayıtlı.");
                    return View(model);
                }

                // TC No kontrolü (varsa)
                if (!string.IsNullOrWhiteSpace(model.TcNo))
                {
                    var existingTc = await _context.Musteriler
                        .AnyAsync(m => m.TcNo == model.TcNo);

                    if (existingTc)
                    {
                        ModelState.AddModelError("TcNo", "Bu TC Kimlik No zaten kayıtlı.");
                        return View(model);
                    }
                }

                var musteri = new Musteri
                {
                    Ad = model.Ad,
                    Soyad = model.Soyad,
                    Telefon = cleanPhone,
                    Eposta = model.Eposta,
                    Sehir = model.Sehir,
                    Cinsiyet = model.Cinsiyet,
                    TcNo = model.TcNo
                };

                _context.Musteriler.Add(musteri);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"✅ Müşteri başarıyla oluşturuldu: {musteri.IdMusteri}");
                TempData["SuccessMessage"] = "Müşteri başarıyla oluşturuldu.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ Müşteri oluşturma hatası: {ex.Message}");
                ModelState.AddModelError("", "Bir hata oluştu. Lütfen tekrar deneyin.");
                return View(model);
            }
        }

        // GET: Musteri/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var musteri = await _context.Musteriler.FindAsync(id);
            if (musteri == null)
            {
                return NotFound();
            }

            var model = new MusteriCreateVM
            {
                Ad = musteri.Ad,
                Soyad = musteri.Soyad,
                Telefon = musteri.Telefon,
                Eposta = musteri.Eposta,
                Sehir = musteri.Sehir,
                Cinsiyet = musteri.Cinsiyet,
                TcNo = musteri.TcNo
            };

            ViewData["MusteriId"] = id;
            return View(model);
        }

        // POST: Musteri/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, MusteriCreateVM model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    ViewData["MusteriId"] = id;
                    return View(model);
                }

                var musteri = await _context.Musteriler.FindAsync(id);
                if (musteri == null)
                {
                    return NotFound();
                }

                // Telefon numarasını temizle
                var cleanPhone = model.Telefon.Replace("(", "").Replace(")", "").Replace(" ", "").Trim();

                // Telefon kontrolü (kendisi hariç)
                var existingPhone = await _context.Musteriler
                    .AnyAsync(m => m.Telefon == cleanPhone && m.IdMusteri != id);

                if (existingPhone)
                {
                    ModelState.AddModelError("Telefon", "Bu telefon numarası zaten kayıtlı.");
                    ViewData["MusteriId"] = id;
                    return View(model);
                }

                // TC No kontrolü (varsa ve kendisi hariç)
                if (!string.IsNullOrWhiteSpace(model.TcNo))
                {
                    var existingTc = await _context.Musteriler
                        .AnyAsync(m => m.TcNo == model.TcNo && m.IdMusteri != id);

                    if (existingTc)
                    {
                        ModelState.AddModelError("TcNo", "Bu TC Kimlik No zaten kayıtlı.");
                        ViewData["MusteriId"] = id;
                        return View(model);
                    }
                }

                musteri.Ad = model.Ad;
                musteri.Soyad = model.Soyad;
                musteri.Telefon = cleanPhone;
                musteri.Eposta = model.Eposta;
                musteri.Sehir = model.Sehir;
                musteri.Cinsiyet = model.Cinsiyet;
                musteri.TcNo = model.TcNo;

                await _context.SaveChangesAsync();

                _logger.LogInformation($"✅ Müşteri güncellendi: {id}");
                TempData["SuccessMessage"] = "Müşteri başarıyla güncellendi.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ Müşteri güncelleme hatası: {ex.Message}");
                ModelState.AddModelError("", "Bir hata oluştu. Lütfen tekrar deneyin.");
                ViewData["MusteriId"] = id;
                return View(model);
            }
        }

        // GET: Musteri/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var musteri = await _context.Musteriler
                .Include(m => m.Randevular)
                .Include(m => m.Satislar)
                .FirstOrDefaultAsync(m => m.IdMusteri == id);

            if (musteri == null)
            {
                return NotFound();
            }

            return View(musteri);
        }

        // POST: Musteri/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var musteri = await _context.Musteriler
                    .Include(m => m.Randevular)
                    .Include(m => m.Satislar)
                    .FirstOrDefaultAsync(m => m.IdMusteri == id);

                if (musteri == null)
                {
                    return NotFound();
                }

                // İlişkili kayıtları kontrol et
                if (musteri.Randevular.Any() || musteri.Satislar.Any())
                {
                    TempData["ErrorMessage"] = "Bu müşteriye ait randevu veya satış kaydı olduğu için silinemez.";
                    return RedirectToAction(nameof(Index));
                }

                _context.Musteriler.Remove(musteri);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"✅ Müşteri silindi: {id}");
                TempData["SuccessMessage"] = "Müşteri başarıyla silindi.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ Müşteri silme hatası: {ex.Message}");
                TempData["ErrorMessage"] = "Bir hata oluştu. Lütfen tekrar deneyin.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Musteri/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var musteri = await _context.Musteriler
                .Include(m => m.Randevular)
                    .ThenInclude(r => r.Topraktar)
                .Include(m => m.Satislar)
                    .ThenInclude(s => s.Topraktar)
                .FirstOrDefaultAsync(m => m.IdMusteri == id);

            if (musteri == null)
            {
                return NotFound();
            }

            return View(musteri);
        }
    }
}