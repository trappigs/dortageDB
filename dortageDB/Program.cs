using dortageDB.Data;
using dortageDB.Entities;
using dortageDB.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

// Set Console Encoding to UTF-8 to display Turkish characters correctly
Console.OutputEncoding = System.Text.Encoding.UTF8;

static async Task CreateAdminUser(IServiceProvider serviceProvider)
{
    var userManager = serviceProvider.GetRequiredService<UserManager<AppUser>>();
    var roleManager = serviceProvider.GetRequiredService<RoleManager<AppRole>>();

    Console.WriteLine("🔧 Admin kullanıcısı kontrol ediliyor...");

    // Admin rolü yoksa oluştur
    if (!await roleManager.RoleExistsAsync("admin"))
    {
        var adminRole = new AppRole { Name = "admin" };
        await roleManager.CreateAsync(adminRole);
        Console.WriteLine("✅ Admin rolü oluşturuldu");
    }

    // Vekarer rolü yoksa oluştur
    if (!await roleManager.RoleExistsAsync("Vekarer"))
    {
        var vekarerRole = new AppRole { Name = "Vekarer" };
        await roleManager.CreateAsync(vekarerRole);
        Console.WriteLine("✅ Vekarer rolü oluşturuldu");
    }
    else
    {
        // Vekarer rolü varsa, NormalizedName'in doğru olduğundan emin ol
        var vekarerRole = await roleManager.FindByNameAsync("Vekarer");
        if (vekarerRole != null && vekarerRole.NormalizedName != "VEKARER")
        {
            vekarerRole.NormalizedName = "VEKARER";
            await roleManager.UpdateAsync(vekarerRole);
            Console.WriteLine("✅ Vekarer rolü NormalizedName düzeltildi");
        }
    }

    // Belirli kullanıcılara Vekarer rolü ata (eğer rolü yoksa)
    var vekarerUsers = new[] { "milyonlarcaharf@gmail.com" };
    foreach (var email in vekarerUsers)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user != null)
        {
            var userRoles = await userManager.GetRolesAsync(user);
            if (!userRoles.Contains("Vekarer") && !userRoles.Contains("admin"))
            {
                await userManager.AddToRoleAsync(user, "Vekarer");
                Console.WriteLine($"✅ {email} kullanıcısına Vekarer rolü atandı");
            }
        }
    }

    // Admin kullanıcısı yoksa oluştur
    var adminUser = await userManager.FindByEmailAsync("admin@dortage.com");
    if (adminUser == null)
    {
        adminUser = new AppUser
        {
            UserName = "admin@dortage.com",
            Email = "admin@dortage.com",
            EmailConfirmed = true,
            PhoneNumber = "05001234567",
            Ad = "Admin",
            Soyad = "DORTAGE",
            Sehir = "İstanbul",
            TcNo = "12345678901",
            Cinsiyet = true,
            Kvkk = true,
            Pazarlama = false
        };

        var result = await userManager.CreateAsync(adminUser, "Admin123!");
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(adminUser, "admin");
            Console.WriteLine("✅ Admin kullanıcısı oluşturuldu:");
            Console.WriteLine($"   Email: admin@dortage.com");
            Console.WriteLine($"   Şifre: Admin123!");
        }
        else
        {
            Console.WriteLine("❌ Admin kullanıcısı oluşturulamadı:");
            foreach (var error in result.Errors)
            {
                Console.WriteLine($"   - {error.Description}");
            }
        }
    }
    else
    {
        Console.WriteLine("ℹ️  Admin kullanıcısı zaten mevcut");
    }
}

var builder = WebApplication.CreateBuilder(args);

Console.WriteLine("=== UYGULAMA BAŞLATILIYOR ===");

// Add services to the container.
builder.Services.AddControllersWithViews();

// Dosya yükleme limitleri
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 10 * 1024 * 1024; // 10MB
});

builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.Limits.MaxRequestBodySize = 10 * 1024 * 1024; // 10MB
});

// DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default"),
        sqlOptions => sqlOptions.EnableRetryOnFailure()));

// Identity
builder.Services.AddIdentity<AppUser, AppRole>(options =>
{
    // Şifre gereksinimleri
    options.Password.RequireDigit = false;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;

    // Kullanıcı ayarları
    options.User.RequireUniqueEmail = true;

    // Lockout ayarları
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;

    // SignIn ayarları
    options.SignIn.RequireConfirmedEmail = false;
    options.SignIn.RequireConfirmedPhoneNumber = false;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

// Cookie ayarları
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromDays(30);
    options.SlidingExpiration = true;
});

// Services
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<ISeoService, SeoService>();

var app = builder.Build();

// Veritabanı migration'larını uygula ve admin kullanıcısı oluştur
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    try
    {
        // Migration'ları uygula
        var context = services.GetRequiredService<AppDbContext>();
        await context.Database.MigrateAsync();
        Console.WriteLine("✅ Migration'lar uygulandı");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Migration hatası: {ex.Message}");
    }

    // Admin kullanıcısı oluştur
    await CreateAdminUser(services);
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Varsayılan route önce
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Özel route'lar: Yasal sayfalar için
app.MapControllerRoute(
    name: "gizlilik-politikasi",
    pattern: "gizlilik-politikasi",
    defaults: new { controller = "Home", action = "GizlilikPolitikasi" });

app.MapControllerRoute(
    name: "kvkk",
    pattern: "kvkk",
    defaults: new { controller = "Home", action = "KVKK" });

app.MapControllerRoute(
    name: "aydinlatma-metni",
    pattern: "aydinlatma-metni",
    defaults: new { controller = "Home", action = "AydınlatmaMetni" });

app.MapControllerRoute(
    name: "giris",
    pattern: "giris",
    defaults: new { controller = "Account", action = "Login" });

app.MapControllerRoute(
    name: "dashboard",
    pattern: "dashboard",
    defaults: new { controller = "Dashboard", action = "Index" });

app.MapControllerRoute(
    name: "randevu",
    pattern: "randevu",
    defaults: new { controller = "Randevu", action = "Index" });

app.MapControllerRoute(
    name: "randevu-detaylar",
    pattern: "randevu/detaylar/{id}",
    defaults: new { controller = "Randevu", action = "Details" });

app.MapControllerRoute(
    name: "randevu-duzenle",
    pattern: "randevu/duzenle/{id}",
    defaults: new { controller = "Randevu", action = "Edit" });

app.MapControllerRoute(
    name: "kayit",
    pattern: "kayit",
    defaults: new { controller = "Account", action = "Register" });

app.MapControllerRoute(
    name: "projeler",
    pattern: "projeler",
    defaults: new { controller = "Proje", action = "Index" });

app.MapControllerRoute(
    name: "vekarernedir",
    pattern: "vekarernedir",
    defaults: new { controller = "VekarerNedir", action = "Index" });

app.MapControllerRoute(
    name: "vekarer-akademi",
    pattern: "vekarer-akademi",
    defaults: new { controller = "VekarerAkademi", action = "Index" });

app.MapControllerRoute(
    name: "hakkimizda",
    pattern: "hakkimizda",
    defaults: new { controller = "Hakkimizda", action = "Index" });

app.MapControllerRoute(
    name: "iletisim",
    pattern: "iletisim",
    defaults: new { controller = "Iletisim", action = "Index" });

// Özel route: Proje slug'ları için (örn: /dikili)
// Bu route en sonda olmalı, böylece diğer route'lar önce denenir
app.MapControllerRoute(
    name: "proje-slug",
    pattern: "{slug}",
    defaults: new { controller = "Proje", action = "Details" });

Console.WriteLine("✅ Uygulama başlatıldı!");

app.Run();
