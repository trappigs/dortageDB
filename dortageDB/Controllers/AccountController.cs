using dortageDB.Data;
using dortageDB.Entities;
using dortageDB.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace dortageDB.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly RoleManager<AppRole> _roleManager;
        private readonly AppDbContext _context;

        public AccountController(
            UserManager<AppUser> userManager,
            SignInManager<AppUser> signInManager,
            RoleManager<AppRole> roleManager,
            AppDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _context = context;
        }

        // GET: Account/Register
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register()
        {
            return View();
        }

        // POST: Account/Register
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterVM model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // KVKK kontrolü
            if (!model.Kvkk)
            {
                ModelState.AddModelError("Kvkk", "KVKK metnini onaylamanız gerekmektedir.");
                return View(model);
            }

            // Yeni kullanıcı oluştur
            var user = new AppUser
            {
                UserName = model.Email,
                Email = model.Email,
                PhoneNumber = model.PhoneNumber,
                Ad = model.Ad,
                Soyad = model.Soyad,
                Sehir = model.Sehir,
                Cinsiyet = model.Cinsiyet,
                TcNo = model.TcNo,
                Kvkk = model.Kvkk,
                Pazarlama = model.Pazarlama,
                EmailConfirmed = true // Geliştirme için otomatik onaylı
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return View(model);
            }

            // Topraktar profili oluştur
            if (model.TopraktarMi)
            {
                var topraktarProfile = new TopraktarProfile
                {
                    UserId = user.Id
                };
                _context.TopraktarProfiles.Add(topraktarProfile);
                await _context.SaveChangesAsync();
            }

            // Roller ata
            if (model.Roller != null && model.Roller.Any())
            {
                foreach (var roleName in model.Roller)
                {
                    // Rol yoksa oluştur
                    if (!await _roleManager.RoleExistsAsync(roleName))
                    {
                        await _roleManager.CreateAsync(new AppRole { Name = roleName });
                    }
                    await _userManager.AddToRoleAsync(user, roleName);
                }
            }
            else if (model.TopraktarMi)
            {
                // Varsayılan olarak topraktar rolü ata
                const string topraktarRole = "topraktar";
                if (!await _roleManager.RoleExistsAsync(topraktarRole))
                {
                    await _roleManager.CreateAsync(new AppRole { Name = topraktarRole });
                }
                await _userManager.AddToRoleAsync(user, topraktarRole);
            }

            TempData["SuccessMessage"] = "Kayıt başarılı! Giriş yapabilirsiniz.";
            return RedirectToAction(nameof(Login));
        }

        // GET: Account/Login
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        // POST: Account/Login
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string email, string password, bool rememberMe = false, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                ModelState.AddModelError("", "E-posta ve şifre gereklidir.");
                return View();
            }

            var result = await _signInManager.PasswordSignInAsync(email, password, rememberMe, lockoutOnFailure: false);

            if (result.Succeeded)
            {
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }
                return RedirectToAction("Index", "Dashboard");
            }

            if (result.IsLockedOut)
            {
                ModelState.AddModelError("", "Hesabınız kilitlenmiştir. Lütfen daha sonra tekrar deneyin.");
                return View();
            }

            ModelState.AddModelError("", "E-posta veya şifre hatalı.");
            return View();
        }

        // POST: Account/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        // GET: Account/AccessDenied
        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}