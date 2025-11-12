using Microsoft.AspNetCore.Mvc;

namespace dortageDB.Controllers;

public class VekarerNedirController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}
