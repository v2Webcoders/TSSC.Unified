using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using QUIZAPP;
using QUIZAPP.Models;
using QUIZAPP.Services;

namespace TSSC.Unified.Areas.Agency.Controllers
{
    public class AgencyController :Controller
    {

        private readonly ILogger<AgencyController> _logger;
        private readonly AppdbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IWebHostEnvironment _environment;
        private readonly EmailService _emailService;
        public AgencyController(
            ILogger<AgencyController> logger,
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
    }
}
