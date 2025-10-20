using dortageDB.Data;
using dortageDB.Entities;
using dortageDB.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

static async Task CreateAdminUser(IServiceProvider serviceProvider)
{
    var userManager = serviceProvider.GetRequiredService<UserManager<AppUser>>();
    var roleManager = serviceProvider.GetRequiredService<RoleManager<AppRole>>();
    
    // Admin rol� yoksa olu�tur
    if (!await roleManager.RoleExistsAsync("admin"))
    {
        await roleManager.CreateAsync(new AppRole { Name = "admin" });
    }
    
    // Admin kullan�c�s� yoksa olu�tur
    var adminUser = await userManager.FindByEmailAsync("admin@dortage.com");
    if (adminUser == null)
    {
        adminUser = new AppUser
        {
            UserName = "admin@dortage.com",
            Email = "admin@dortage.com",
            EmailConfirmed = true,
            Ad = "Admin",
            Soyad = "User",
            Sehir = "�stanbul",
            Cinsiyet = true,
            Kvkk = true,
            Pazarlama = false
        };
        
        await userManager.CreateAsync(adminUser, "Admin123!");
        await userManager.AddToRoleAsync(adminUser, "Admin");
    }
}
var builder = WebApplication.CreateBuilder(args);

Console.WriteLine("=== UYGULAMA BA�LATILIYOR ===");

// Add services to the container.
builder.Services.AddControllersWithViews();

// DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

// Identity
builder.Services.AddIdentity<AppUser, AppRole>(options =>
{
    // �ifre gereksinimleri
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;

    // Kullan�c� ayarlar�
    options.User.RequireUniqueEmail = true;

    // Lockout ayarlar�
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;

    // SignIn ayarlar�
    options.SignIn.RequireConfirmedEmail = false;
    options.SignIn.RequireConfirmedPhoneNumber = false;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

// Cookie ayarlar�
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

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
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

app.Run();