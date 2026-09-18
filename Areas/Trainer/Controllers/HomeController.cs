using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QUIZAPP;
using QUIZAPP.Models;
using System.Diagnostics;

namespace TSSC.Unified.Areas.Trainer.Controllers
{

    [Area("Trainer")]
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly AppdbContext _db;

        public HomeController(ILogger<HomeController> logger, AppdbContext db)
        {
            _logger = logger;
            _db = db;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
