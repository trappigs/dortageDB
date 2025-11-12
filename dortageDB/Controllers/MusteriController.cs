using dortageDB.Data;
using dortageDB.Entities;
using dortageDB.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace dortageDB.Controllers
{
    [Authorize(Roles = "Vekarer,admin")]
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
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Admin tüm müþterileri görebilir, diðerleri sadece kendi müþterilerini
            var musteriler = User.IsInRole("admin")
                ? await _context.Musteriler
                    .Include(m => m.Vekarer)
                    .OrderByDescending(m => m.IdMusteri)
                    .ToListAsync()
                : await _context.Musteriler
                    .Include(m => m.Vekarer)
                    .Where(m => m.VekarerID == user.Id)
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
                _logger.LogInformation("=== MÜÞTERÝ OLUÞTURMA BAÞLADI ===");
                _logger.LogInformation($"Ad: {model.Ad}, Soyad: {model.Soyad}");

                if (!ModelState.IsValid)
                {
                    return View(model);
                }

                // Telefon numarasýný temizle
                var cleanPhone = model.Telefon.Replace("(", "").Replace(")", "").Replace(" ", "").Trim();

                // Telefon kontrolü
                var existingPhone = await _context.Musteriler
                    .AnyAsync(m => m.Telefon == cleanPhone);

                if (existingPhone)
                {
                    ModelState.AddModelError("Telefon", "Bu telefon numarasý zaten kayýtlý.");
                    return View(model);
                }

                // TC No kontrolü (varsa)
                if (!string.IsNullOrWhiteSpace(model.TcNo))
                {
                    var existingTc = await _context.Musteriler
                        .AnyAsync(m => m.TcNo == model.TcNo);

                    if (existingTc)
                    {
                        ModelState.AddModelError("TcNo", "Bu TC Kimlik No zaten kayýtlý.");
                        return View(model);
                    }
                }

                var currentUser = await _userManager.GetUserAsync(User);

                var musteri = new Musteri
                {
                    Ad = model.Ad,
                    Soyad = model.Soyad,
                    Telefon = cleanPhone,
                    Cinsiyet = model.Cinsiyet,
                    TcNo = model.TcNo,
                    VekarerID = currentUser!.Id
                };

                _context.Musteriler.Add(musteri);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"? Müþteri baþarýyla oluþturuldu: {musteri.IdMusteri}");
                TempData["SuccessMessage"] = "Müþteri baþarýyla oluþturuldu.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError($"? Müþteri oluþturma hatasý: {ex.Message}");
                ModelState.AddModelError("", "Bir hata oluþtu. Lütfen tekrar deneyin.");
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

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var musteri = await _context.Musteriler.FindAsync(id);
            if (musteri == null)
            {
                return NotFound();
            }

            // Vekarer sadece kendi müþterisini düzenleyebilir (admin hariç)
            if (!User.IsInRole("admin") && musteri.VekarerID != user.Id)
            {
                _logger.LogWarning($"?? Yetkisiz eriþim denemesi: User {user.Id} tried to edit customer {id}");
                return Forbid();
            }

            var model = new MusteriCreateVM
            {
                Ad = musteri.Ad,
                Soyad = musteri.Soyad,
                Telefon = musteri.Telefon,
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

                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return RedirectToAction("Login", "Account");
                }

                var musteri = await _context.Musteriler.FindAsync(id);
                if (musteri == null)
                {
                    return NotFound();
                }

                // Vekarer sadece kendi müþterisini düzenleyebilir (admin hariç)
                if (!User.IsInRole("admin") && musteri.VekarerID != user.Id)
                {
                    _logger.LogWarning($"?? Yetkisiz eriþim denemesi: User {user.Id} tried to edit customer {id}");
                    return Forbid();
                }

                // Telefon numarasýný temizle
                var cleanPhone = model.Telefon.Replace("(", "").Replace(")", "").Replace(" ", "").Trim();

                // Telefon kontrolü (kendisi hariç)
                var existingPhone = await _context.Musteriler
                    .AnyAsync(m => m.Telefon == cleanPhone && m.IdMusteri != id);

                if (existingPhone)
                {
                    ModelState.AddModelError("Telefon", "Bu telefon numarasý zaten kayýtlý.");
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
                        ModelState.AddModelError("TcNo", "Bu TC Kimlik No zaten kayýtlý.");
                        ViewData["MusteriId"] = id;
                        return View(model);
                    }
                }

                musteri.Ad = model.Ad;
                musteri.Soyad = model.Soyad;
                musteri.Telefon = cleanPhone;
                musteri.Cinsiyet = model.Cinsiyet;
                musteri.TcNo = model.TcNo;

                await _context.SaveChangesAsync();

                _logger.LogInformation($"? Müþteri güncellendi: {id}");
                TempData["SuccessMessage"] = "Müþteri baþarýyla güncellendi.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError($"? Müþteri güncelleme hatasý: {ex.Message}");
                ModelState.AddModelError("", "Bir hata oluþtu. Lütfen tekrar deneyin.");
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

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var musteri = await _context.Musteriler
                .Include(m => m.Randevular)
                .Include(m => m.Satislar)
                .FirstOrDefaultAsync(m => m.IdMusteri == id);

            if (musteri == null)
            {
                return NotFound();
            }

            // Vekarer sadece kendi müþterisini silebilir (admin hariç)
            if (!User.IsInRole("admin") && musteri.VekarerID != user.Id)
            {
                _logger.LogWarning($"?? Yetkisiz eriþim denemesi: User {user.Id} tried to delete customer {id}");
                return Forbid();
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
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return RedirectToAction("Login", "Account");
                }

                var musteri = await _context.Musteriler
                    .Include(m => m.Randevular)
                    .Include(m => m.Satislar)
                    .FirstOrDefaultAsync(m => m.IdMusteri == id);

                if (musteri == null)
                {
                    return NotFound();
                }

                // Vekarer sadece kendi müþterisini silebilir (admin hariç)
                if (!User.IsInRole("admin") && musteri.VekarerID != user.Id)
                {
                    _logger.LogWarning($"?? Yetkisiz eriþim denemesi: User {user.Id} tried to delete customer {id}");
                    return Forbid();
                }

                // Ýliþkili kayýtlarý kontrol et
                if (musteri.Randevular.Any() || musteri.Satislar.Any())
                {
                    TempData["ErrorMessage"] = "Bu müþteriye ait randevu veya satýþ kaydý olduðu için silinemez.";
                    return RedirectToAction(nameof(Index));
                }

                _context.Musteriler.Remove(musteri);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"? Müþteri silindi: {id}");
                TempData["SuccessMessage"] = "Müþteri baþarýyla silindi.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError($"? Müþteri silme hatasý: {ex.Message}");
                TempData["ErrorMessage"] = "Bir hata oluþtu. Lütfen tekrar deneyin.";
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

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var musteri = await _context.Musteriler
                .Include(m => m.Randevular)
                    .ThenInclude(r => r.Vekarer)
                .Include(m => m.Satislar)
                    .ThenInclude(s => s.Vekarer)
                .FirstOrDefaultAsync(m => m.IdMusteri == id);

            if (musteri == null)
            {
                return NotFound();
            }

            // Vekarer sadece kendi müþterisinin detayýný görebilir (admin hariç)
            if (!User.IsInRole("admin") && musteri.VekarerID != user.Id)
            {
                _logger.LogWarning($"?? Yetkisiz eriþim denemesi: User {user.Id} tried to view customer {id}");
                return Forbid();
            }

            return View(musteri);
        }
    }
}
