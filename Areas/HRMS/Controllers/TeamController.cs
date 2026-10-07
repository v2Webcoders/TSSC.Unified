using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QUIZAPP;
using QUIZAPP.Models;
using TSSC.Unified.ViewModel;

namespace TSSC.Unified.Areas.HRMS.Controllers
{
    [Area("HRMS")]
    [Authorize]
    public class TeamController : Controller
    {
        private readonly AppdbContext _context;
        private readonly UserManager<AppUser> _userManager;

        public TeamController(
            AppdbContext context,
            UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [Authorize]
        public async Task<IActionResult> TeamMembers()
        {
            var userId = _userManager.GetUserId(User);

            var currentEmployee = await _context.Employee
                .FirstOrDefaultAsync(x => x.ApplicationUserId == userId);

            if (currentEmployee == null)
                return NotFound();

            // Determine the manager of the current employee's team
            var teamManagerId =
                currentEmployee.ReportingManagerId
                ?? currentEmployee.EmployeeId;

            // Get manager
            var manager = await _context.Employee
                .Include(x => x.Department)
                .Include(x => x.Designation)
                .FirstOrDefaultAsync(x => x.EmployeeId == teamManagerId);

            // Get team members
            var teamMembers = await _context.Employee
                .Where(x =>
                    x.IsActive &&
                    x.ReportingManagerId == teamManagerId)
                .Include(x => x.Department)
                .Include(x => x.Designation)
                .OrderBy(x => x.FirstName)
                .ToListAsync();

            var model = new TeamMembersVM
            {
                Manager = manager,
                TeamMembers = teamMembers
            };

            return View(model);
        }
    }
}
