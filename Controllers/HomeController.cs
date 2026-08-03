using Microsoft.AspNetCore.Mvc;

namespace TSSC.Unified.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return RedirectToAction("Index", "Home", new { area = "Admin" });
        }
    }
}
