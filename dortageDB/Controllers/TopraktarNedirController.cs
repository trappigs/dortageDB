using Microsoft.AspNetCore.Mvc;

namespace dortageDB.Controllers;

public class TopraktarNedirController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}
