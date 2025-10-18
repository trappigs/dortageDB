using dortageDB.Data;
using dortageDB.Entities;
using dortageDB.ViewModels;
using dortageDB.Services;
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
        private readonly IReferralService _referralService;
        private readonly IEmailService _emailService;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            UserManager<AppUser> userManager,
            SignInManager<AppUser> signInManager,
            RoleManager<AppRole> roleManager,
            AppDbContext context,
            IReferralService referralService,
            IEmailService emailService,
            ILogger<AccountController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _context = context;
            _referralService = referralService;
            _emailService = emailService;
            _logger = logger;
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
            try
            {
                if (!ModelState.IsValid)
                {
                    // Hataları logla ve ViewBag'e ekle
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                    ViewBag.Errors = errors;

                    foreach (var error in errors)
                    {
                        Console.WriteLine($"❌ Validation Error: {error}");
                    }
                    return View(model);
                }

                // KVKK kontrolü
                if (!model.Kvkk)
                {
                    ModelState.AddModelError("Kvkk", "KVKK metnini onaylamanız gerekmektedir.");
                    return View(model);
                }

                // Referral kodu kontrolü (varsa)
                if (!string.IsNullOrWhiteSpace(model.ReferralCode))
                {
                    Console.WriteLine($"🔍 Referans kodu kontrol ediliyor: {model.ReferralCode}");
                    var (isValid, error) = await _referralService.ValidateAndConsumeAsync(model.ReferralCode);

                    if (!isValid)
                    {
                        Console.WriteLine($"❌ Referans kodu geçersiz: {error}");
                        ModelState.AddModelError("ReferralCode", error ?? "Geçersiz referans kodu.");
                        return View(model);
                    }

                    Console.WriteLine("✅ Referans kodu doğrulandı ve tüketildi!");
                }

                Console.WriteLine("✅ Validation başarılı, kullanıcı oluşturuluyor...");

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
                    EmailConfirmed = true
                };

                Console.WriteLine($"📝 Kullanıcı oluşturuluyor: {user.Email}");

                var result = await _userManager.CreateAsync(user, model.Password);

                if (!result.Succeeded)
                {
                    Console.WriteLine("❌ Kullanıcı oluşturma başarısız!");
                    foreach (var error in result.Errors)
                    {
                        Console.WriteLine($"   - {error.Description}");
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                    return View(model);
                }

                Console.WriteLine("✅ Kullanıcı başarıyla oluşturuldu!");

                // Topraktar profili oluştur
                if (model.TopraktarMi)
                {
                    Console.WriteLine("📋 Topraktar profili oluşturuluyor...");
                    var topraktarProfile = new TopraktarProfile
                    {
                        UserId = user.Id
                    };
                    _context.TopraktarProfiles.Add(topraktarProfile);
                    await _context.SaveChangesAsync();
                    Console.WriteLine("✅ Topraktar profili oluşturuldu!");
                }

                // Roller ata
                if (model.Roller != null && model.Roller.Any())
                {
                    Console.WriteLine("👤 Özel roller atanıyor...");
                    foreach (var roleName in model.Roller)
                    {
                        if (!await _roleManager.RoleExistsAsync(roleName))
                        {
                            await _roleManager.CreateAsync(new AppRole { Name = roleName });
                            Console.WriteLine($"📝 Rol oluşturuldu: {roleName}");
                        }
                        await _userManager.AddToRoleAsync(user, roleName);
                        Console.WriteLine($"✅ Rol atandı: {roleName}");
                    }
                }
                else if (model.TopraktarMi)
                {
                    Console.WriteLine("👤 Topraktar rolü atanıyor...");
                    const string topraktarRole = "topraktar";
                    if (!await _roleManager.RoleExistsAsync(topraktarRole))
                    {
                        Console.WriteLine("📝 Topraktar rolü oluşturuluyor...");
                        await _roleManager.CreateAsync(new AppRole { Name = topraktarRole });
                    }
                    await _userManager.AddToRoleAsync(user, topraktarRole);
                    Console.WriteLine("✅ Topraktar rolü atandı!");
                }

                Console.WriteLine("🎉 Kayıt işlemi tamamlandı! Login sayfasına yönlendiriliyor...");

                // Referral kodu kullanıldıysa başarı mesajına ekle
                if (!string.IsNullOrWhiteSpace(model.ReferralCode))
                {
                    TempData["SuccessMessage"] = "Kayıt başarılı! Referans kodunuz kullanıldı. Giriş yapabilirsiniz.";
                }
                else
                {
                    TempData["SuccessMessage"] = "Kayıt başarılı! Giriş yapabilirsiniz.";
                }

                return RedirectToAction(nameof(Login));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"💥 HATA: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                ModelState.AddModelError(string.Empty, $"Bir hata oluştu: {ex.Message}");
                return View(model);
            }
        }

        // AccountController.cs - Login metodları (güncellenmiş hali)

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
        public async Task<IActionResult> Login(LoginVM model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            Console.WriteLine($"🔐 Login denemesi: {model.Email}");

            var result = await _signInManager.PasswordSignInAsync(
                model.Email,
                model.Password,
                model.RememberMe,
                lockoutOnFailure: false
            );

            if (result.Succeeded)
            {
                Console.WriteLine($"✅ Login başarılı: {model.Email}");

                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }
                return RedirectToAction("Index", "Dashboard");
            }

            if (result.IsLockedOut)
            {
                Console.WriteLine($"🔒 Hesap kilitli: {model.Email}");
                ModelState.AddModelError("", "Hesabınız kilitlenmiştir. Lütfen daha sonra tekrar deneyin.");
                return View(model);
            }

            Console.WriteLine($"❌ Login başarısız: {model.Email}");
            ModelState.AddModelError("", "E-posta veya şifre hatalı.");
            return View(model);
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


        // AccountController.cs - Şifremi Unuttum metodları ekleyin

        // GET: Account/ForgotPassword
        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        // POST: Account/ForgotPassword
        public async Task<IActionResult> ForgotPassword(ForgotPasswordVM model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // User tipini kullanıyoruz
            var user = await _userManager.FindByEmailAsync(model.Email);

            if (user == null)
            {
                _logger.LogWarning("Şifre sıfırlama talebi - Kullanıcı bulunamadı: {Email}", model.Email);
                return RedirectToAction(nameof(ForgotPasswordConfirmation));
            }
            _logger.LogInformation("Şifre sıfırlama talebi gönderildi: {Email}", model.Email);

            // Token oluştur - User tipiyle
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);


            var callbackUrl = Url.Action(
                action: "ResetPassword",
                controller: "Account",
                values: new { userId = user.Id, token = token },
                protocol: Request.Scheme);

            var emailBody = $@"
        <h2>Şifre Sıfırlama Talebi</h2>
        <p>Merhaba,</p>
        <p>Şifrenizi sıfırlamak için aşağıdaki linke tıklayınız:</p>
        <p><a href='{callbackUrl}'>Şifremi Sıfırla</a></p>
        <p>Eğer bu talebi siz yapmadıysanız, bu emaili görmezden gelebilirsiniz.</p>
        <p>Bu link 24 saat geçerlidir.</p>
    ";

            await _emailService.SendEmailAsync(
                to: model.Email,
                subject: "Şifre Sıfırlama Talebi",
                htmlBody: emailBody);

            return RedirectToAction(nameof(ForgotPasswordConfirmation));
        }

        // GET: Account/ForgotPasswordConfirmation
        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPasswordConfirmation()
        {
            return View();
        }

        // GET: Account/ResetPassword
        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResetPassword(string? token = null, string? email = null)
        {
            if (token == null || email == null)
            {
                TempData["ErrorMessage"] = "Geçersiz şifre sıfırlama linki.";
                return RedirectToAction(nameof(Login));
            }

            var model = new ResetPasswordVM
            {
                Token = token,
                Email = email
            };

            return View(model);
        }

        // POST: Account/ResetPassword
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordVM model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                Console.WriteLine($"❌ Şifre sıfırlama başarısız - Kullanıcı bulunamadı: {model.Email}");
                // Güvenlik için başarılı mesajı göster
                return RedirectToAction(nameof(ResetPasswordConfirmation));
            }

            Console.WriteLine($"🔄 Şifre sıfırlanıyor: {model.Email}");

            var result = await _userManager.ResetPasswordAsync(user, model.Token, model.NewPassword);

            if (result.Succeeded)
            {
                Console.WriteLine($"✅ Şifre başarıyla sıfırlandı: {model.Email}");
                return RedirectToAction(nameof(ResetPasswordConfirmation));
            }

            Console.WriteLine($"❌ Şifre sıfırlama başarısız: {model.Email}");
            foreach (var error in result.Errors)
            {
                Console.WriteLine($"   - {error.Description}");
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        // GET: Account/ResetPasswordConfirmation
        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResetPasswordConfirmation()
        {
            return View();
        }








        // GET: Account/AccessDenied
        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}