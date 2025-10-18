using dortageDB.Data;
using dortageDB.Entities;
using dortageDB.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

public class AccountController : Controller
{
    private readonly AppDbContext _context;

    public AccountController(AppDbContext context)
    {
        _context = context;
    }

    // GET: Account/Register
    [HttpGet]
    public IActionResult Register()
    {
        return View();
    }

    // POST: Account/Register
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(UserCreateVM model)
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

        // E-posta kontrolü
        if (await _context.Users.AnyAsync(u => u.Eposta == model.Eposta))
        {
            ModelState.AddModelError("Eposta", "Bu e-posta adresi zaten kullanılıyor.");
            return View(model);
        }

        // Telefon kontrolü
        if (await _context.Users.AnyAsync(u => u.Telefon == model.Telefon))
        {
            ModelState.AddModelError("Telefon", "Bu telefon numarası zaten kullanılıyor.");
            return View(model);
        }

        // Yeni kullanıcı oluştur
        var user = new User
        {
            Ad = model.Ad,
            Soyad = model.Soyad,
            Telefon = model.Telefon,
            Eposta = model.Eposta,
            Sehir = model.Sehir,
            Cinsiyet = model.Cinsiyet,
            Kvkk = model.Kvkk,
            Pazarlama = model.Pazarlama,
            Sifre = HashPassword(model.Sifre)
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Topraktar profili oluştur
        if (model.TopraktarMi)
        {
            var topraktarProfile = new TopraktarProfile
            {
                UserId = user.Id
            };
            _context.TopraktarProfiles.Add(topraktarProfile);
        }

        // Roller ata
        if (model.Roller != null && model.Roller.Any())
        {
            foreach (var roleName in model.Roller)
            {
                var role = await _context.Roles.FirstOrDefaultAsync(r => r.Name == roleName);
                if (role != null)
                {
                    var userRole = new UserRole
                    {
                        UserId = user.Id,
                        RoleId = role.Id
                    };
                    _context.UserRoles.Add(userRole);
                }
            }
        }
        else if (model.TopraktarMi)
        {
            // Varsayılan olarak topraktar rolü ata
            var topraktarRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "topraktar");
            if (topraktarRole != null)
            {
                var userRole = new UserRole
                {
                    UserId = user.Id,
                    RoleId = topraktarRole.Id
                };
                _context.UserRoles.Add(userRole);
            }
        }

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Kayıt başarılı! Giriş yapabilirsiniz.";
        return RedirectToAction(nameof(Login));
    }

    // GET: Account/Login
    [HttpGet]
    public IActionResult Login()
    {
        return View();
    }

    // POST: Account/Login
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(string eposta, string sifre)
    {
        if (string.IsNullOrEmpty(eposta) || string.IsNullOrEmpty(sifre))
        {
            ModelState.AddModelError("", "E-posta ve şifre gereklidir.");
            return View();
        }

        var user = await _context.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Eposta == eposta);

        if (user == null || !VerifyPassword(sifre, user.Sifre))
        {
            ModelState.AddModelError("", "E-posta veya şifre hatalı.");
            return View();
        }

        // Claims oluştur
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, $"{user.Ad} {user.Soyad}"),
            new Claim(ClaimTypes.Email, user.Eposta)
        };

        // Rolleri ekle
        foreach (var userRole in user.UserRoles)
        {
            claims.Add(new Claim(ClaimTypes.Role, userRole.Role.Name));
        }

        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var authProperties = new AuthenticationProperties
        {
            IsPersistent = true,
            ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
        };

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(claimsIdentity),
            authProperties);

        return RedirectToAction("Index", "Dashboard");
    }

    // POST: Account/Logout
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }

    // Şifre hashleme
    private string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(hashedBytes);
    }

    // Şifre doğrulama
    private bool VerifyPassword(string password, string hashedPassword)
    {
        var hashOfInput = HashPassword(password);
        return hashOfInput == hashedPassword;
    }
}