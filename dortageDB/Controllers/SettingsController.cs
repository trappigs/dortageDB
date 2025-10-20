// dortageDB/Controllers/SettingsController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using dortageDB.Entities;
using dortageDB.ViewModels;
using dortageDB.Data;

namespace dortageDB.Controllers
{
    [Authorize]
    public class SettingsController : Controller
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly AppDbContext _context;
        private readonly ILogger<SettingsController> _logger;

        public SettingsController(
            UserManager<AppUser> userManager,
            SignInManager<AppUser> signInManager,
            AppDbContext context,
            ILogger<SettingsController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
            _logger = logger;
        }

        // GET: Settings/Profile
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var model = new UserSettingsVM
            {
                Ad = user.Ad,
                Soyad = user.Soyad,
                Email = user.Email ?? "",
                PhoneNumber = user.PhoneNumber ?? "",
                Sehir = user.Sehir,
                TcNo = user.TcNo,
                Cinsiyet = user.Cinsiyet
            };

            return View(model);
        }

        // POST: Settings/UpdateProfile
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(UserSettingsVM model)
        {
            _logger.LogInformation("=== Profil güncelleme başladı ===");
            _logger.LogInformation($"Ad: {model.Ad}, Soyad: {model.Soyad}");

            // Sadece kişisel bilgileri güncelliyorsak şifre alanlarını ModelState'den çıkar
            ModelState.Remove("CurrentPassword");
            ModelState.Remove("NewPassword");
            ModelState.Remove("ConfirmNewPassword");

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("ModelState geçersiz");
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    _logger.LogWarning($"Hata: {error.ErrorMessage}");
                }
                TempData["ErrorMessage"] = "Lütfen tüm alanları doğru doldurun.";
                return View("Profile", model);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                _logger.LogError("Kullanıcı bulunamadı");
                return RedirectToAction("Login", "Account");
            }

            try
            {
                // Kişisel bilgileri güncelle
                user.Ad = model.Ad;
                user.Soyad = model.Soyad;
                user.Sehir = model.Sehir;
                user.Cinsiyet = model.Cinsiyet;
                user.TcNo = model.TcNo;

                // Telefon numarasını temizle
                if (!string.IsNullOrWhiteSpace(model.PhoneNumber))
                {
                    var cleanPhone = model.PhoneNumber.Replace("(", "").Replace(")", "").Replace(" ", "").Trim();
                    user.PhoneNumber = cleanPhone;
                }

                var result = await _userManager.UpdateAsync(user);

                if (result.Succeeded)
                {
                    _logger.LogInformation("✅ Profil başarıyla güncellendi");
                    TempData["SuccessMessage"] = "Profiliniz başarıyla güncellendi.";
                    return RedirectToAction(nameof(Profile));
                }

                _logger.LogError("Profil güncelleme başarısız");
                foreach (var error in result.Errors)
                {
                    _logger.LogError($"Hata: {error.Description}");
                    ModelState.AddModelError(string.Empty, error.Description);
                }

                TempData["ErrorMessage"] = "Profil güncellenirken bir hata oluştu.";
                return View("Profile", model);
            }
            catch (Exception ex)
            {
                _logger.LogError($"💥 HATA: {ex.Message}");
                _logger.LogError($"Stack Trace: {ex.StackTrace}");
                TempData["ErrorMessage"] = "Bir hata oluştu. Lütfen tekrar deneyin.";
                return View("Profile", model);
            }
        }

        // POST: Settings/ChangePassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(UserSettingsVM model)
        {
            _logger.LogInformation("=== Şifre değiştirme başladı ===");

            if (string.IsNullOrWhiteSpace(model.CurrentPassword) ||
                string.IsNullOrWhiteSpace(model.NewPassword))
            {
                TempData["ErrorMessage"] = "Lütfen tüm şifre alanlarını doldurun.";
                return RedirectToAction(nameof(Profile));
            }

            if (model.NewPassword != model.ConfirmNewPassword)
            {
                TempData["ErrorMessage"] = "Yeni şifreler eşleşmiyor.";
                return RedirectToAction(nameof(Profile));
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            try
            {
                var result = await _userManager.ChangePasswordAsync(
                    user,
                    model.CurrentPassword,
                    model.NewPassword
                );

                if (result.Succeeded)
                {
                    _logger.LogInformation("✅ Şifre başarıyla değiştirildi");
                    await _signInManager.RefreshSignInAsync(user);
                    TempData["SuccessMessage"] = "Şifreniz başarıyla değiştirildi.";
                    return RedirectToAction(nameof(Profile));
                }

                _logger.LogError("Şifre değiştirme başarısız");
                foreach (var error in result.Errors)
                {
                    _logger.LogError($"Hata: {error.Description}");
                }

                TempData["ErrorMessage"] = "Mevcut şifreniz hatalı veya yeni şifre gereksinimleri karşılamıyor.";
                return RedirectToAction(nameof(Profile));
            }
            catch (Exception ex)
            {
                _logger.LogError($"💥 HATA: {ex.Message}");
                TempData["ErrorMessage"] = "Bir hata oluştu. Lütfen tekrar deneyin.";
                return RedirectToAction(nameof(Profile));
            }
        }
    }
}