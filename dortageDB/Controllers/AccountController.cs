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
        // POST: Account/Register
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterVM model)
        {
            try
            {
                Console.WriteLine("=== KAYIT İŞLEMİ BAŞLADI ===");
                Console.WriteLine($"📧 Email: {model.Email}");
                Console.WriteLine($"📱 Telefon: {model.PhoneNumber}");
                Console.WriteLine($"🆔 TC No: {model.TcNo}");
                Console.WriteLine($"🔑 Referans Kodu: {model.Code}");

                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                    Console.WriteLine("❌ ModelState geçersiz:");
                    foreach (var error1 in errors)
                    {
                        Console.WriteLine($"   - {error1}");
                    }
                    ViewBag.Errors = errors;
                    return View(model);
                }

                // KVKK kontrolü
                if (!model.Kvkk)
                {
                    Console.WriteLine("❌ KVKK onayı eksik");
                    ModelState.AddModelError("Kvkk", "KVKK metnini onaylamanız gerekmektedir.");
                    return View(model);
                }

                // Pazarlama kontrolü (Kullanım Koşulları)
                if (!model.Pazarlama)
                {
                    Console.WriteLine("❌ Kullanım koşulları onayı eksik");
                    ModelState.AddModelError("Pazarlama", "Kullanım koşullarını kabul etmelisiniz.");
                    return View(model);
                }

                // Referral kodu kontrolü - ZORUNLU
                if (string.IsNullOrWhiteSpace(model.Code))
                {
                    Console.WriteLine("❌ Referans kodu boş");
                    ModelState.AddModelError("Code", "Referans kodu zorunludur.");
                    return View(model);
                }

                Console.WriteLine($"🔍 Referans kodu kontrol ediliyor: {model.Code}");
                var (isValid, error) = await _referralService.ValidateAndConsumeAsync(model.Code);

                if (!isValid)
                {
                    Console.WriteLine($"❌ Referans kodu geçersiz: {error}");
                    ModelState.AddModelError("Code", error ?? "Geçersiz referans kodu.");
                    return View(model);
                }

                Console.WriteLine("✅ Referans kodu doğrulandı!");

                // Telefon numarasını temizle (formatı kaldır)
                var cleanPhone = model.PhoneNumber.Replace("(", "").Replace(")", "").Replace(" ", "").Trim();
                Console.WriteLine($"📱 Temizlenmiş telefon: {cleanPhone}");

                // Yeni kullanıcı oluştur
                var user = new AppUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    PhoneNumber = cleanPhone,
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
                    foreach (var err in result.Errors)
                    {
                        Console.WriteLine($"   - {err.Code}: {err.Description}");

                        // Türkçe hata mesajları
                        if (err.Code == "DuplicateUserName")
                            ModelState.AddModelError("Email", "Bu e-posta adresi zaten kayıtlı.");
                        else if (err.Code == "DuplicateEmail")
                            ModelState.AddModelError("Email", "Bu e-posta adresi zaten kayıtlı.");
                        else if (err.Code == "PasswordTooShort")
                            ModelState.AddModelError("Password", "Şifre en az 6 karakter olmalıdır.");
                        else if (err.Code == "PasswordRequiresNonAlphanumeric")
                            ModelState.AddModelError("Password", "Şifre en az bir özel karakter içermelidir.");
                        else if (err.Code == "PasswordRequiresDigit")
                            ModelState.AddModelError("Password", "Şifre en az bir rakam içermelidir.");
                        else if (err.Code == "PasswordRequiresUpper")
                            ModelState.AddModelError("Password", "Şifre en az bir büyük harf içermelidir.");
                        else
                            ModelState.AddModelError(string.Empty, err.Description);
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

                TempData["SuccessMessage"] = "Kayıt başarılı! Referans kodunuz kullanıldı. Giriş yapabilirsiniz.";
                return RedirectToAction(nameof(Login));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"💥 HATA: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");

                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                }

                ModelState.AddModelError(string.Empty, "Bir hata oluştu. Lütfen tekrar deneyin.");
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


        // GET: Account/ForgotPassword
        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        // POST: Account/ForgotPassword
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordVM model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.FindByEmailAsync(model.Email);

            if (user == null)
            {
                _logger.LogWarning("Şifre sıfırlama talebi - Kullanıcı bulunamadı: {Email}", model.Email);
                return RedirectToAction(nameof(ForgotPasswordConfirmation));
            }

            _logger.LogInformation("Şifre sıfırlama talebi gönderildi: {Email}", model.Email);

            // Token oluştur
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);

            // ✅ DÜZELTME: email parametresini de ekle
            var callbackUrl = Url.Action(
                action: "ResetPassword",
                controller: "Account",
                values: new { token = token, email = user.Email }, // ✅ email eklendi
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
            // ✅ DÜZELTME: email parametresi de kontrol ediliyor
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