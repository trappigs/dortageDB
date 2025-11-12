using dortageDB.Data;
using dortageDB.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace dortageDB.Controllers
{
    [Authorize(Roles = "Vekarer,admin")]
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

        // GET: Satis/Index - Vekarerýn kendi satýþlarýný listele
        public async Task<IActionResult> Index()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Sadece bu Vekarera ait satýþlarý getir
            var satislar = await _context.Satislar
                .Include(s => s.Musteri)
                .Include(s => s.Vekarer)
                .Where(s => s.VekarerID == currentUser.Id)
                .OrderByDescending(s => s.SatilmaTarihi)
                .ToListAsync();

            // Ýstatistikler
            var totalSales = satislar.Count;
            var totalRevenue = satislar.Sum(s => s.ToplamSatisFiyati);
            var totalCommission = satislar.Sum(s => s.OdenecekKomisyon);
            var avgSaleValue = totalSales > 0 ? totalRevenue / totalSales : 0;
            var taksitCount = satislar.Count(s => s.Taksit);
            var pesinCount = totalSales - taksitCount;

            // Bu ay satýþlarý
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

        // GET: Satis/Details/5 - Satýþ detaylarýný görüntüle
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
                .Include(s => s.Vekarer)
                .FirstOrDefaultAsync(s => s.SatisID == id);

            if (satis == null)
            {
                return NotFound();
            }

            // Sadece kendi satýþýný görebilir (admin hariç)
            if (!User.IsInRole("admin") && satis.VekarerID != currentUser.Id)
            {
                _logger.LogWarning($"?? Yetkisiz eriþim denemesi: User {currentUser.Id} tried to access sale {id}");
                return Forbid();
            }

            return View(satis);
        }
    }
}
