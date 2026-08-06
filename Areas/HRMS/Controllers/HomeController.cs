using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QUIZAPP.Models;
using QUIZAPP.ViewModel;
using System.Diagnostics;

namespace QUIZAPP.Areas.HRMS.Controllers
{
    [Area("HRMS")]
    [Authorize(Roles = "HR,Employee")]
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
            if (User.IsInRole("HR"))
            {
                return View("Index");
            }
            else if (User.IsInRole("Employee"))
            {
                return View("EmployeeDashboard");
            }

            return View();
        }
        public IActionResult EmployeeDashboard()
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

