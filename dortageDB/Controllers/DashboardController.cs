using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using dortageDB.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using dortageDB.Entities;

namespace dortageDB.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly AppDbContext _context;

        public DashboardController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userId = int.Parse(userIdString!);

            // Toplam Müşteri Sayısı (bu topraktarın eklediği müşteriler)
            var totalCustomers = _context.Musteriler
                .Where(m => m.TopraktarID == userId)
                .Count();

            // Toplam Satış Sayısı
            var totalSales = _context.Satislar
                .Where(s => s.TopraktarID == userId)
                .Count();

            // Toplam Komisyon
            var totalCommission = _context.Satislar
                .Where(s => s.TopraktarID == userId)
                .Sum(s => (decimal?)s.OdenecekKomisyon) ?? 0;

            // Bekleyen Randevular
            var pendingAppointments = _context.Randevular
                .Where(r => r.TopraktarID == userId && r.RandevuDurum == RandevuDurum.pending)
                .Count();

            // Bu Ay Eklenen Müşteriler
            var thisMonthStart = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);

            var customersThisMonth = _context.Musteriler
                .Where(m => m.TopraktarID == userId && m.EklenmeTarihi >= thisMonthStart)
                .Count();

            // Bu Ay Yapılan Satışlar
            var salesThisMonth = _context.Satislar
                .Where(s => s.TopraktarID == userId && s.SatilmaTarihi >= thisMonthStart)
                .Count();

            // Bu Ay Komisyon
            var commissionThisMonth = _context.Satislar
                .Where(s => s.TopraktarID == userId && s.SatilmaTarihi >= thisMonthStart)
                .Sum(s => (decimal?)s.OdenecekKomisyon) ?? 0;

            // Bugünkü Randevular
            var today = DateTime.Today;
            var todayAppointments = _context.Randevular
                .Where(r => r.TopraktarID == userId &&
                           r.RandevuDurum == RandevuDurum.pending &&
                           r.RandevuZaman.Date == today)
                .Count();

            // Yüzde hesaplamaları (geçen aya göre)
            var lastMonthStart = thisMonthStart.AddMonths(-1);
            var lastMonthEnd = thisMonthStart.AddDays(-1);

            var customersLastMonth = _context.Musteriler
                .Where(m => m.TopraktarID == userId &&
                           m.EklenmeTarihi >= lastMonthStart &&
                           m.EklenmeTarihi <= lastMonthEnd)
                .Count();

            var salesLastMonth = _context.Satislar
                .Where(s => s.TopraktarID == userId &&
                           s.SatilmaTarihi >= lastMonthStart &&
                           s.SatilmaTarihi <= lastMonthEnd)
                .Count();

            var commissionLastMonth = _context.Satislar
                .Where(s => s.TopraktarID == userId &&
                           s.SatilmaTarihi >= lastMonthStart &&
                           s.SatilmaTarihi <= lastMonthEnd)
                .Sum(s => (decimal?)s.OdenecekKomisyon) ?? 0;

            // Yüzde değişim hesaplama
            int customerPercentChange = customersLastMonth > 0
                ? (int)Math.Round(((double)(customersThisMonth - customersLastMonth) / customersLastMonth) * 100)
                : 0;

            int salesPercentChange = salesLastMonth > 0
                ? (int)Math.Round(((double)(salesThisMonth - salesLastMonth) / salesLastMonth) * 100)
                : 0;

            int commissionPercentChange = commissionLastMonth > 0
                ? (int)Math.Round(((double)(commissionThisMonth - commissionLastMonth) / (double)commissionLastMonth) * 100)
                : 0;

            // Son Aktiviteler için veri topla
            var recentActivities = new List<object>();

            // Son 5 satış
            var recentSales = _context.Satislar
                .Where(s => s.TopraktarID == userId)
                .OrderByDescending(s => s.SatilmaTarihi)
                .Take(3)
                .Include(s => s.Musteri)
                .Select(s => new
                {
                    Type = "sale",
                    Title = $"{s.Musteri.Ad} {s.Musteri.Soyad} - Satış tamamlandı",
                    Time = s.SatilmaTarihi,
                    Icon = "success"
                })
                .ToList();

            // Son 5 randevu
            var recentAppointments = _context.Randevular
                .Where(r => r.TopraktarID == userId)
                .OrderByDescending(r => r.OlusturulmaTarihi)
                .Take(3)
                .Include(r => r.Musteri)
                .Select(r => new
                {
                    Type = "appointment",
                    Title = $"{r.Musteri.Ad} {r.Musteri.Soyad} - Randevu oluşturuldu",
                    Time = r.OlusturulmaTarihi,
                    Icon = "warning"
                })
                .ToList();

            // Son 5 müşteri
            var recentCustomers = _context.Musteriler
                .Where(m => m.TopraktarID == userId)
                .OrderByDescending(m => m.EklenmeTarihi)
                .Take(3)
                .Select(m => new
                {
                    Type = "customer",
                    Title = $"{m.Ad} {m.Soyad} - Yeni müşteri eklendi",
                    Time = m.EklenmeTarihi,
                    Icon = "info"
                })
                .ToList();

            // Tüm aktiviteleri birleştir ve tarihe göre sırala
            recentActivities.AddRange(recentSales);
            recentActivities.AddRange(recentAppointments);
            recentActivities.AddRange(recentCustomers);

            var sortedActivities = recentActivities
                .OrderByDescending(a => ((dynamic)a).Time)
                .Take(5)
                .ToList();

            // ViewBag'e verileri gönder
            ViewBag.TotalCustomers = totalCustomers;
            ViewBag.TotalSales = totalSales;
            ViewBag.TotalCommission = totalCommission;
            ViewBag.PendingAppointments = pendingAppointments;
            ViewBag.CustomersThisMonth = customersThisMonth;
            ViewBag.SalesThisMonth = salesThisMonth;
            ViewBag.CommissionThisMonth = commissionThisMonth;
            ViewBag.TodayAppointments = todayAppointments;
            ViewBag.CustomerPercentChange = customerPercentChange;
            ViewBag.SalesPercentChange = salesPercentChange;
            ViewBag.CommissionPercentChange = commissionPercentChange;
            ViewBag.RecentActivities = sortedActivities;

            return View();
        }

        public IActionResult TopraktarDashboard()
        {
            return View();
        }
    }
}