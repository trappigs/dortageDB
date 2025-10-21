using dortageDB.Data;
using dortageDB.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace dortageDB.Controllers
{
    [Authorize(Roles = "topraktar,admin")]
    public class SatisController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly ILogger<SatisController> _logger;

        public SatisController(
            AppDbContext context,
            UserManager<AppUser> userManager,
            ILogger<SatisController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        // GET: Satis/Index - Topraktarın kendi satışlarını listele
        public async Task<IActionResult> Index()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Sadece bu topraktara ait satışları getir
            var satislar = await _context.Satislar
                .Include(s => s.Musteri)
                .Include(s => s.Topraktar)
                .Where(s => s.TopraktarID == currentUser.Id)
                .OrderByDescending(s => s.SatilmaTarihi)
                .ToListAsync();

            // İstatistikler
            var totalSales = satislar.Count;
            var totalRevenue = satislar.Sum(s => s.ToplamSatisFiyati);
            var totalCommission = satislar.Sum(s => s.OdenecekKomisyon);
            var avgSaleValue = totalSales > 0 ? totalRevenue / totalSales : 0;
            var taksitCount = satislar.Count(s => s.Taksit);
            var pesinCount = totalSales - taksitCount;

            // Bu ay satışları
            var thisMonthStart = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            var thisMonthSales = satislar.Count(s => s.SatilmaTarihi >= thisMonthStart);
            var thisMonthRevenue = satislar
                .Where(s => s.SatilmaTarihi >= thisMonthStart)
                .Sum(s => s.ToplamSatisFiyati);

            ViewBag.TotalSales = totalSales;
            ViewBag.TotalRevenue = totalRevenue;
            ViewBag.TotalCommission = totalCommission;
            ViewBag.AvgSaleValue = avgSaleValue;
            ViewBag.TaksitCount = taksitCount;
            ViewBag.PesinCount = pesinCount;
            ViewBag.ThisMonthSales = thisMonthSales;
            ViewBag.ThisMonthRevenue = thisMonthRevenue;

            return View(satislar);
        }

        // GET: Satis/Details/5 - Satış detaylarını görüntüle
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var satis = await _context.Satislar
                .Include(s => s.Musteri)
                .Include(s => s.Topraktar)
                .FirstOrDefaultAsync(s => s.SatisID == id);

            if (satis == null)
            {
                return NotFound();
            }

            // Sadece kendi satışını görebilir (admin hariç)
            if (!User.IsInRole("admin") && satis.TopraktarID != currentUser.Id)
            {
                _logger.LogWarning($"⚠️ Yetkisiz erişim denemesi: User {currentUser.Id} tried to access sale {id}");
                return Forbid();
            }

            return View(satis);
        }
    }
}