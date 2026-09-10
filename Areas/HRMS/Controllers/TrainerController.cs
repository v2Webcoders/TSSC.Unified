using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QUIZAPP;
using QUIZAPP.Models;
using QUIZAPP.Services;

namespace TSSC.Unified.Areas.HRMS.Controllers
{
    [Area("HRMS")]
    [Authorize]
    public class TrainerController :Controller
    {

        private readonly ILogger<TrainerController> _logger;
        private readonly AppdbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IWebHostEnvironment _environment;
        private readonly EmailService _emailService;
        public TrainerController(
            ILogger<TrainerController> logger,
            AppdbContext context,
            UserManager<AppUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IWebHostEnvironment environment,
            EmailService emailService)
        {
            _logger = logger;
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
            _environment = environment;
            _emailService = emailService;
        }
        // =====================================================
        // for TOT?TOA Employee
        // =====================================================
        public async Task<IActionResult> RegistrationList(string? search)
        {
            var query = _context.TrainerRegistration
                .AsNoTracking()
                .AsQueryable();

            // Search by Candidate Name, Email, Mobile or Registration No.
            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();

                query = query.Where(x =>
                    x.CandidateName.Contains(search) ||
                    x.Email.Contains(search) ||
                    x.Mobile.Contains(search) ||
                    x.RegistrationNo.Contains(search));
            }

            var registrations = await query
                .OrderByDescending(x => x.Id)
                .ToListAsync();

            ViewBag.Search = search;

            return View(registrations);
        }
    }
}
