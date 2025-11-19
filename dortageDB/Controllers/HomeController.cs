using System.Diagnostics;
using dortageDB.Models;
using Microsoft.AspNetCore.Mvc;

namespace dortageDB.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult GizlilikPolitikasi()
        {
            ViewBag.PdfPath = "/documents/gizlilik.pdf";
            return View();
        }

        public IActionResult KVKK()
        {
            ViewBag.PdfPath = "/documents/kvkk.pdf";
            return View();
        }

        public IActionResult AydınlatmaMetni()
        {
            ViewBag.PdfPath = "/documents/aydinlatma-metni.pdf";
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
