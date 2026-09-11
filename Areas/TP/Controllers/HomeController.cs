using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QUIZAPP;
using QUIZAPP.Models;
using TSSC.Unified.Models;

namespace TSSC.Unified.Areas.TP.Controllers
{
    [Area("TP")]
    public class HomeController : Controller
    {
        private readonly AppdbContext _context;
        private readonly UserManager<AppUser> _userManager;

        public HomeController(
            AppdbContext context,
            UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return Unauthorized();

            var registration = await _context.TPRegistrations
                .FirstOrDefaultAsync(x =>
                    x.ApplicationUserId == user.Id);

            if (registration == null)
                return NotFound("TP registration not found.");

            return View(registration);
        }
    }
}
