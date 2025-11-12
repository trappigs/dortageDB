using Microsoft.AspNetCore.Mvc;

namespace dortageDB.Controllers;

public class VisionerNedirController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}
