using dortageDB.Data;
using dortageDB.Entities;
using dortageDB.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

static async Task CreateAdminUser(IServiceProvider serviceProvider)
{
    var userManager = serviceProvider.GetRequiredService<UserManager<AppUser>>();
    var roleManager = serviceProvider.GetRequiredService<RoleManager<AppRole>>();

    Console.WriteLine("🔧 Admin kullanıcısı kontrol ediliyor...");

    // Admin rolü yoksa oluştur
    if (!await roleManager.RoleExistsAsync("admin"))
    {
        await roleManager.CreateAsync(new AppRole { Name = "admin" });
        Console.WriteLine("✅ Admin rolü oluşturuldu");
    }

    // Vekarer rolü yoksa oluştur
    if (!await roleManager.RoleExistsAsync("Vekarer"))
    {
        await roleManager.CreateAsync(new AppRole { Name = "Vekarer" });
        Console.WriteLine("✅ Vekarer rolü oluşturuldu");
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

// DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

// Identity
builder.Services.AddIdentity<AppUser, AppRole>(options =>
{
    // Şifre gereksinimleri
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;

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
builder.Services.AddScoped<IReferralService, ReferralService>();
builder.Services.AddScoped<IEmailService, EmailService>();

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

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

Console.WriteLine("✅ Uygulama başlatıldı!");

app.Run();
