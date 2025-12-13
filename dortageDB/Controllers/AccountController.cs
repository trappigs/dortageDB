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
                Console.WriteLine("=== KAYIT ��LEM� BA�LADI ===");
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
                    Console.WriteLine("? KVKK onayı eksik");
                    ModelState.AddModelError("Kvkk", "KVKK metnini onaylamanız gerekmektedir.");
                    return View(model);
                }

                // Pazarlama kontrol� (Kullan�m Ko�ullar�)
                if (!model.Pazarlama)
                {
                    Console.WriteLine("? Kullan�m ko�ullar� onay� eksik");
                    ModelState.AddModelError("Pazarlama", "Kullanım koşullarını kabul etmelisiniz.");
                    return View(model);
                }

                // Referral kodu kontrol� - ZORUNLU
                if (string.IsNullOrWhiteSpace(model.Code))
                {
                    Console.WriteLine("? Referans kodu boş");
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

                Console.WriteLine("? Referans kodu do�ruland�!");

                // Telefon numaras�n� temizle (format� kald�r)
                var cleanPhone = model.PhoneNumber.Replace("(", "").Replace(")", "").Replace(" ", "").Trim();
                Console.WriteLine($"?? Temizlenmi� telefon: {cleanPhone}");

                // Yeni kullan�c� olu�tur
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

                Console.WriteLine($"?? Kullan�c� olu�turuluyor: {user.Email}");

                var result = await _userManager.CreateAsync(user, model.Password);

                if (!result.Succeeded)
                {
                    Console.WriteLine("? Kullan�c� olu�turma ba�ar�s�z!");
                    foreach (var err in result.Errors)
                    {
                        Console.WriteLine($"   - {err.Code}: {err.Description}");

                        // T�rk�e hata mesajlar�
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

                Console.WriteLine("? Kullan�c� ba�ar�yla olu�turuldu!");

                // Vekarer profili olu�tur
                if (model.VekarerMi)
                {
                    Console.WriteLine("?? Vekarer profili olu�turuluyor...");
                    var VekarerProfile = new VekarerProfile
                    {
                        UserId = user.Id,
                        UsedReferralCode = model.Code // Kay�t olurken kulland��� referans kodu
                    };
                    _context.VekarerProfiles.Add(VekarerProfile);
                    await _context.SaveChangesAsync();
                    Console.WriteLine("? Vekarer profili olu�turuldu!");
                }

                // Roller ata
                if (model.Roller != null && model.Roller.Any())
                {
                    Console.WriteLine("?? �zel roller atan�yor...");
                    foreach (var roleName in model.Roller)
                    {
                        if (!await _roleManager.RoleExistsAsync(roleName))
                        {
                            await _roleManager.CreateAsync(new AppRole { Name = roleName });
                            Console.WriteLine($"?? Rol olu�turuldu: {roleName}");
                        }
                        await _userManager.AddToRoleAsync(user, roleName);
                        Console.WriteLine($"? Rol atand�: {roleName}");
                    }
                }
                else if (model.VekarerMi)
                {
                    Console.WriteLine("?? Vekarer rol� atan�yor...");
                    const string VekarerRole = "Vekarer";
                    if (!await _roleManager.RoleExistsAsync(VekarerRole))
                    {
                        Console.WriteLine("?? Vekarer rol� olu�turuluyor...");
                        await _roleManager.CreateAsync(new AppRole { Name = VekarerRole });
                    }
                    await _userManager.AddToRoleAsync(user, VekarerRole);
                    Console.WriteLine("? Vekarer rol� atand�!");
                }

                Console.WriteLine("?? Kay�t i�lemi tamamland�! Login sayfas�na y�nlendiriliyor...");

                TempData["SuccessMessage"] = "Kayıt başarılı! Referans kodunuz kullanıldı. Giriş yapabilirsiniz.";
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

                ModelState.AddModelError(string.Empty, "Bir hata oluştu. Lütfen tekrar deneyin.");
                return View(model);
            }
        }

        // AccountController.cs - Login metodlar� (g�ncellenmi� hali)

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
                Console.WriteLine($"? Login ba�ar�l�: {model.Email}");

                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }
                return RedirectToAction("Index", "Dashboard");
            }

            if (result.IsLockedOut)
            {
                Console.WriteLine($"?? Hesap kilitli: {model.Email}");
                ModelState.AddModelError("", "Hesabınız kilitlenmiştir. Lütfen daha sonra tekrar deneyin.");
                return View(model);
            }

            Console.WriteLine($"? Login ba�ar�s�z: {model.Email}");
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

            // Token olu�tur
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);

            // ? D�ZELTME: email parametresini de ekle
            var callbackUrl = Url.Action(
                action: "ResetPassword",
                controller: "Account",
                values: new { token = token, email = user.Email }, // ? email eklendi
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
            // ? D�ZELTME: email parametresi de kontrol ediliyor
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
                Console.WriteLine($"Şifre sıfırlama başarısız - Kullanıcı bulunamadı: {model.Email}");
                // G�venlik i�in ba�ar�l� mesaj� g�ster
                return RedirectToAction(nameof(ResetPasswordConfirmation));
            }

            Console.WriteLine($"Şifre sıfırlanıyor: {model.Email}");

            var result = await _userManager.ResetPasswordAsync(user, model.Token, model.NewPassword);

            if (result.Succeeded)
            {
                Console.WriteLine($"Şifre başarıyla sıfırlandı: {model.Email}");
                return RedirectToAction(nameof(ResetPasswordConfirmation));
            }

            Console.WriteLine($"Şifre sıfırlama başarısız: {model.Email}");
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
        public async Task<IActionResult> AccessDenied()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user != null)
                {
                    var roles = await _userManager.GetRolesAsync(user);
                    ViewBag.UserEmail = user.Email;
                    ViewBag.UserRoles = string.Join(", ", roles);
                    _logger.LogWarning($"Access Denied for user: {user.Email}, Roles: {string.Join(", ", roles)}");
                }
            }
            return View();
        }

        // TEMP: Manuel rol oluşturma ve atama endpoint'i
        [HttpGet]
        public async Task<IActionResult> FixRoles()
        {
            var messages = new List<string>();

            try
            {
                // Vekarer rolü yoksa oluştur
                var vekarerRole = await _roleManager.FindByNameAsync("Vekarer");
                if (vekarerRole == null)
                {
                    vekarerRole = new AppRole { Name = "Vekarer" };
                    var result = await _roleManager.CreateAsync(vekarerRole);
                    if (result.Succeeded)
                    {
                        messages.Add("✅ Vekarer rolü oluşturuldu");
                    }
                    else
                    {
                        messages.Add("❌ Vekarer rolü oluşturulamadı: " + string.Join(", ", result.Errors.Select(e => e.Description)));
                        return Content(string.Join("\n", messages), "text/plain");
                    }
                }
                else
                {
                    messages.Add("ℹ️ Vekarer rolü zaten mevcut");
                }

                // Admin rolü yoksa oluştur
                var adminRole = await _roleManager.FindByNameAsync("admin");
                if (adminRole == null)
                {
                    adminRole = new AppRole { Name = "admin" };
                    var result = await _roleManager.CreateAsync(adminRole);
                    if (result.Succeeded)
                    {
                        messages.Add("✅ Admin rolü oluşturuldu");
                    }
                    else
                    {
                        messages.Add("❌ Admin rolü oluşturulamadı: " + string.Join(", ", result.Errors.Select(e => e.Description)));
                    }
                }
                else
                {
                    messages.Add("ℹ️ Admin rolü zaten mevcut");
                }

                // Mevcut kullanıcıya Vekarer rolü ata
                if (User.Identity?.IsAuthenticated == true)
                {
                    var user = await _userManager.GetUserAsync(User);
                    if (user != null)
                    {
                        // Önce rolün kesinlikle var olduğunu kontrol et
                        vekarerRole = await _roleManager.FindByNameAsync("Vekarer");
                        if (vekarerRole == null)
                        {
                            messages.Add("❌ Vekarer rolü bulunamadı - rol oluşturma başarısız olmuş olabilir");
                            return Content(string.Join("\n", messages), "text/plain");
                        }

                        var isInRole = await _userManager.IsInRoleAsync(user, "Vekarer");
                        if (!isInRole)
                        {
                            var result = await _userManager.AddToRoleAsync(user, "Vekarer");
                            if (result.Succeeded)
                            {
                                messages.Add($"✅ {user.Email} kullanıcısına Vekarer rolü atandı");
                                // Kullanıcıyı yeniden oturum açmaya zorla
                                await _signInManager.RefreshSignInAsync(user);
                                messages.Add("✅ Oturum yenilendi - artık Randevu ve Satış sayfalarına erişebilirsiniz!");
                            }
                            else
                            {
                                messages.Add($"❌ Rol atanamadı: " + string.Join(", ", result.Errors.Select(e => e.Description)));
                            }
                        }
                        else
                        {
                            messages.Add($"ℹ️ {user.Email} zaten Vekarer rolüne sahip");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                messages.Add($"❌ Hata oluştu: {ex.Message}");
                messages.Add($"Stack Trace: {ex.StackTrace}");
            }

            return Content(string.Join("\n", messages), "text/plain");
        }
    }
}
