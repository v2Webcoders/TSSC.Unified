using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QUIZAPP;
using QUIZAPP.Models;
using TSSC.Unified.Models;

namespace TSSC.Unified.Areas.Admin.Controllers.TP
{
    [Area("Admin")]
    public class TPRegistrationController : Controller
    {
        private readonly AppdbContext _context;
        private readonly UserManager<AppUser> _userManager;
        public TPRegistrationController(AppdbContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index(bool approvedOnly = false)
        {
            IQueryable<TPRegistration> query = _context.TPRegistrations;

            // =====================================================
            // APPROVED & ONBOARDED
            // =====================================================
            if (approvedOnly)
            {
                query = query.Where(x =>
                    x.Status == "Approved" &&
                    x.CEO_Status == "Approved");
            }

            // =====================================================
            // ALL REGISTRATIONS
            // =====================================================
            // No filter when approvedOnly = false

            var registrations = await query
                .OrderByDescending(x => x.CreatedOn)
                .ToListAsync();

            ViewBag.ApprovedOnly = approvedOnly;

            return View(registrations);
        }


        public async Task<IActionResult> Details(int id)
        {
            var registration = await _context.TPRegistrations
                .Include(x => x.Documents)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (registration == null)
                return NotFound();

            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return Unauthorized();

            var roles = await _userManager.GetRolesAsync(user);


            ViewBag.UserRole = roles.FirstOrDefault();

            return View(registration);
        }

        
    }
}
