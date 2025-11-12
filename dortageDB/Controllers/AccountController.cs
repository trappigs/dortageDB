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
                Console.WriteLine("=== KAYIT ÝÞLEMÝ BAÞLADI ===");
                Console.WriteLine($"?? Email: {model.Email}");
                Console.WriteLine($"?? Telefon: {model.PhoneNumber}");
                Console.WriteLine($"?? TC No: {model.TcNo}");
                Console.WriteLine($"?? Referans Kodu: {model.Code}");

                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                    Console.WriteLine("? ModelState geçersiz:");
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
                    Console.WriteLine("? KVKK onayý eksik");
                    ModelState.AddModelError("Kvkk", "KVKK metnini onaylamanýz gerekmektedir.");
                    return View(model);
                }

                // Pazarlama kontrolü (Kullaným Koþullarý)
                if (!model.Pazarlama)
                {
                    Console.WriteLine("? Kullaným koþullarý onayý eksik");
                    ModelState.AddModelError("Pazarlama", "Kullaným koþullarýný kabul etmelisiniz.");
                    return View(model);
                }

                // Referral kodu kontrolü - ZORUNLU
                if (string.IsNullOrWhiteSpace(model.Code))
                {
                    Console.WriteLine("? Referans kodu boþ");
                    ModelState.AddModelError("Code", "Referans kodu zorunludur.");
                    return View(model);
                }

                Console.WriteLine($"?? Referans kodu kontrol ediliyor: {model.Code}");
                var (isValid, error) = await _referralService.ValidateAndConsumeAsync(model.Code);

                if (!isValid)
                {
                    Console.WriteLine($"? Referans kodu geçersiz: {error}");
                    ModelState.AddModelError("Code", error ?? "Geçersiz referans kodu.");
                    return View(model);
                }

                Console.WriteLine("? Referans kodu doðrulandý!");

                // Telefon numarasýný temizle (formatý kaldýr)
                var cleanPhone = model.PhoneNumber.Replace("(", "").Replace(")", "").Replace(" ", "").Trim();
                Console.WriteLine($"?? Temizlenmiþ telefon: {cleanPhone}");

                // Yeni kullanýcý oluþtur
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

                Console.WriteLine($"?? Kullanýcý oluþturuluyor: {user.Email}");

                var result = await _userManager.CreateAsync(user, model.Password);

                if (!result.Succeeded)
                {
                    Console.WriteLine("? Kullanýcý oluþturma baþarýsýz!");
                    foreach (var err in result.Errors)
                    {
                        Console.WriteLine($"   - {err.Code}: {err.Description}");

                        // Türkçe hata mesajlarý
                        if (err.Code == "DuplicateUserName")
                            ModelState.AddModelError("Email", "Bu e-posta adresi zaten kayýtlý.");
                        else if (err.Code == "DuplicateEmail")
                            ModelState.AddModelError("Email", "Bu e-posta adresi zaten kayýtlý.");
                        else if (err.Code == "PasswordTooShort")
                            ModelState.AddModelError("Password", "Þifre en az 6 karakter olmalýdýr.");
                        else if (err.Code == "PasswordRequiresNonAlphanumeric")
                            ModelState.AddModelError("Password", "Þifre en az bir özel karakter içermelidir.");
                        else if (err.Code == "PasswordRequiresDigit")
                            ModelState.AddModelError("Password", "Þifre en az bir rakam içermelidir.");
                        else if (err.Code == "PasswordRequiresUpper")
                            ModelState.AddModelError("Password", "Þifre en az bir büyük harf içermelidir.");
                        else
                            ModelState.AddModelError(string.Empty, err.Description);
                    }
                    return View(model);
                }

                Console.WriteLine("? Kullanýcý baþarýyla oluþturuldu!");

                // Vekarer profili oluþtur
                if (model.VekarerMi)
                {
                    Console.WriteLine("?? Vekarer profili oluþturuluyor...");
                    var VekarerProfile = new VekarerProfile
                    {
                        UserId = user.Id,
                        UsedReferralCode = model.Code // Kayýt olurken kullandýðý referans kodu
                    };
                    _context.VekarerProfiles.Add(VekarerProfile);
                    await _context.SaveChangesAsync();
                    Console.WriteLine("? Vekarer profili oluþturuldu!");
                }

                // Roller ata
                if (model.Roller != null && model.Roller.Any())
                {
                    Console.WriteLine("?? Özel roller atanýyor...");
                    foreach (var roleName in model.Roller)
                    {
                        if (!await _roleManager.RoleExistsAsync(roleName))
                        {
                            await _roleManager.CreateAsync(new AppRole { Name = roleName });
                            Console.WriteLine($"?? Rol oluþturuldu: {roleName}");
                        }
                        await _userManager.AddToRoleAsync(user, roleName);
                        Console.WriteLine($"? Rol atandý: {roleName}");
                    }
                }
                else if (model.VekarerMi)
                {
                    Console.WriteLine("?? Vekarer rolü atanýyor...");
                    const string VekarerRole = "Vekarer";
                    if (!await _roleManager.RoleExistsAsync(VekarerRole))
                    {
                        Console.WriteLine("?? Vekarer rolü oluþturuluyor...");
                        await _roleManager.CreateAsync(new AppRole { Name = VekarerRole });
                    }
                    await _userManager.AddToRoleAsync(user, VekarerRole);
                    Console.WriteLine("? Vekarer rolü atandý!");
                }

                Console.WriteLine("?? Kayýt iþlemi tamamlandý! Login sayfasýna yönlendiriliyor...");

                TempData["SuccessMessage"] = "Kayýt baþarýlý! Referans kodunuz kullanýldý. Giriþ yapabilirsiniz.";
                return RedirectToAction(nameof(Login));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"?? HATA: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");

                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                }

                ModelState.AddModelError(string.Empty, "Bir hata oluþtu. Lütfen tekrar deneyin.");
                return View(model);
            }
        }

        // AccountController.cs - Login metodlarý (güncellenmiþ hali)

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

            Console.WriteLine($"?? Login denemesi: {model.Email}");

            var result = await _signInManager.PasswordSignInAsync(
                model.Email,
                model.Password,
                model.RememberMe,
                lockoutOnFailure: false
            );

            if (result.Succeeded)
            {
                Console.WriteLine($"? Login baþarýlý: {model.Email}");

                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }
                return RedirectToAction("Index", "Dashboard");
            }

            if (result.IsLockedOut)
            {
                Console.WriteLine($"?? Hesap kilitli: {model.Email}");
                ModelState.AddModelError("", "Hesabýnýz kilitlenmiþtir. Lütfen daha sonra tekrar deneyin.");
                return View(model);
            }

            Console.WriteLine($"? Login baþarýsýz: {model.Email}");
            ModelState.AddModelError("", "E-posta veya þifre hatalý.");
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
                _logger.LogWarning("Þifre sýfýrlama talebi - Kullanýcý bulunamadý: {Email}", model.Email);
                return RedirectToAction(nameof(ForgotPasswordConfirmation));
            }

            _logger.LogInformation("Þifre sýfýrlama talebi gönderildi: {Email}", model.Email);

            // Token oluþtur
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);

            // ? DÜZELTME: email parametresini de ekle
            var callbackUrl = Url.Action(
                action: "ResetPassword",
                controller: "Account",
                values: new { token = token, email = user.Email }, // ? email eklendi
                protocol: Request.Scheme);

            var emailBody = $@"
                <h2>Þifre Sýfýrlama Talebi</h2>
                <p>Merhaba,</p>
                <p>Þifrenizi sýfýrlamak için aþaðýdaki linke týklayýnýz:</p>
                <p><a href='{callbackUrl}'>Þifremi Sýfýrla</a></p>
                <p>Eðer bu talebi siz yapmadýysanýz, bu emaili görmezden gelebilirsiniz.</p>
                <p>Bu link 24 saat geçerlidir.</p>
            ";

            await _emailService.SendEmailAsync(
                to: model.Email,
                subject: "Þifre Sýfýrlama Talebi",
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
            // ? DÜZELTME: email parametresi de kontrol ediliyor
            if (token == null || email == null)
            {
                TempData["ErrorMessage"] = "Geçersiz þifre sýfýrlama linki.";
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
                Console.WriteLine($"? Þifre sýfýrlama baþarýsýz - Kullanýcý bulunamadý: {model.Email}");
                // Güvenlik için baþarýlý mesajý göster
                return RedirectToAction(nameof(ResetPasswordConfirmation));
            }

            Console.WriteLine($"?? Þifre sýfýrlanýyor: {model.Email}");

            var result = await _userManager.ResetPasswordAsync(user, model.Token, model.NewPassword);

            if (result.Succeeded)
            {
                Console.WriteLine($"? Þifre baþarýyla sýfýrlandý: {model.Email}");
                return RedirectToAction(nameof(ResetPasswordConfirmation));
            }

            Console.WriteLine($"? Þifre sýfýrlama baþarýsýz: {model.Email}");
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
