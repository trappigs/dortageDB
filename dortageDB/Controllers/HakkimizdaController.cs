using Microsoft.AspNetCore.Mvc;

namespace dortageDB.Controllers;

public class HakkimizdaController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}
