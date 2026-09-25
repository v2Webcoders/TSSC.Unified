using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QUIZAPP;
using QUIZAPP.Models;
using TSSC.Unified.Models;

namespace TSSC.Unified.Areas.HRMS.Controllers.TP
{
    [Area("HRMS")]
    public class TPRegistrationController : Controller
    {
        private readonly AppdbContext _context;
        private readonly UserManager<AppUser> _userManager;
        public TPRegistrationController(AppdbContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }
        //public async Task<IActionResult> Index()
        //{
        //    var user = await _userManager.GetUserAsync(User);

        //    if (user == null)
        //        return Unauthorized();

        //    var roles = await _userManager.GetRolesAsync(user);

        //    IQueryable<TPRegistration> query = _context.TPRegistrations;

        //    if (roles.Contains("RegionalHead"))
        //    {
        //        // Regional Head sees applications waiting for
        //        // Regional Head approval
        //        query = query.Where(x =>
        //            string.IsNullOrEmpty(x.Regional_Head_Status));
        //    }
        //    else if (roles.Contains("VerticalHead"))
        //    {
        //        // Vertical Head sees applications approved by
        //        // Regional Head and waiting for Vertical Head
        //        query = query.Where(x =>
        //            x.Regional_Head_Status == "Approved" &&
        //            string.IsNullOrEmpty(x.Vertical_Head_Status));
        //    }
        //    else if (roles.Contains("Finance"))
        //    {
        //        // Finance sees applications approved by
        //        // Vertical Head and waiting for Finance
        //        query = query.Where(x =>
        //            x.Vertical_Head_Status == "Approved" &&
        //            string.IsNullOrEmpty(x.Finance_Status));
        //    }
        //    else if (roles.Contains("CEO"))
        //    {
        //        // CEO sees applications approved by
        //        // Finance and waiting for CEO
        //        query = query.Where(x =>
        //            x.Finance_Status == "Approved" &&
        //            string.IsNullOrEmpty(x.CEO_Status));
        //    }
        //    else
        //    {
        //        return Forbid();
        //    }

        //    var registrations = await query
        //        .OrderByDescending(x => x.CreatedOn)
        //        .ToListAsync();

        //    return View(registrations);
        //}

        public async Task<IActionResult> Index(string tab = "pending")
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return Unauthorized();

            var roles = await _userManager.GetRolesAsync(user);

            IQueryable<TPRegistration> query = _context.TPRegistrations;

            if (roles.Contains("RegionalHead"))
            {
                if (tab == "pending")
                {
                    query = query.Where(x =>
                        string.IsNullOrEmpty(x.Regional_Head_Status) ||
                        x.Regional_Head_Status == "Pending");
                }
                else if (tab == "approved")
                {
                    query = query.Where(x =>
                        x.Regional_Head_Status == "Approved");
                }
                else if (tab == "rejected")
                {
                    query = query.Where(x =>
                        x.Regional_Head_Status == "Rejected");
                }
            }
            else if (roles.Contains("VerticalHead"))
            {
                if (tab == "pending")
                {
                    query = query.Where(x =>
                        x.Regional_Head_Status == "Approved" &&
                        (string.IsNullOrEmpty(x.Vertical_Head_Status) ||
                         x.Vertical_Head_Status == "Pending"));
                }
                else if (tab == "approved")
                {
                    query = query.Where(x =>
                        x.Vertical_Head_Status == "Approved");
                }
                else if (tab == "rejected")
                {
                    query = query.Where(x =>
                        x.Vertical_Head_Status == "Rejected");
                }
            }
            else if (roles.Contains("Finance"))
            {
                if (tab == "pending")
                {
                    query = query.Where(x =>
                        x.Vertical_Head_Status == "Approved" &&
                        (string.IsNullOrEmpty(x.Finance_Status) ||
                         x.Finance_Status == "Pending"));
                }
                else if (tab == "approved")
                {
                    query = query.Where(x =>
                        x.Finance_Status == "Approved");
                }
                else if (tab == "rejected")
                {
                    query = query.Where(x =>
                        x.Finance_Status == "Rejected");
                }
            }
            else if (roles.Contains("CEO"))
            {
                if (tab == "pending")
                {
                    query = query.Where(x =>
                        x.Finance_Status == "Approved" &&
                        (string.IsNullOrEmpty(x.CEO_Status) ||
                         x.CEO_Status == "Pending"));
                }
                else if (tab == "approved")
                {
                    query = query.Where(x =>
                        x.CEO_Status == "Approved");
                }
                else if (tab == "rejected")
                {
                    query = query.Where(x =>
                        x.CEO_Status == "Rejected");
                }
            }
            else
            {
                return Forbid();
            }

            var registrations = await query
                .OrderByDescending(x => x.CreatedOn)
                .ToListAsync();

            ViewBag.CurrentTab = tab;

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

            var employee = await _context.Employee
                .FirstOrDefaultAsync(x => x.ApplicationUserId == user.Id);

            if (employee == null)
                return Forbid();

            ViewBag.UserRole = roles.FirstOrDefault();

            ViewBag.EmployeeName =
                $"{employee.FirstName} {employee.LastName}".Trim();

            ViewBag.EmployeeCode = employee.EmployeeCode;

            ViewBag.ApprovedBy =
                $"{employee.FirstName} {employee.LastName}".Trim()
                + $" ({employee.EmployeeCode})";

            return View(registration);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Decide(
    int id,
    string decision,
    string? remarks)
        {
            if (decision is not ("Approved" or "Rejected"))
                return BadRequest();

            var registration = await _context.TPRegistrations
                .FirstOrDefaultAsync(x => x.Id == id);

            if (registration == null)
                return NotFound();

            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return Unauthorized();

            var roles = await _userManager.GetRolesAsync(user);

            var employee = await _context.Employee
                .FirstOrDefaultAsync(x => x.ApplicationUserId == user.Id);

            if (employee == null)
                return Forbid();

            var employeeName =
                $"{employee.FirstName} {employee.LastName}".Trim();

            var approvedBy =
                $"{employeeName} ({employee.EmployeeCode})";


            // =====================================================
            // REGIONAL HEAD
            // =====================================================

            if (roles.Contains("RegionalHead"))
            {
                //if (!string.IsNullOrEmpty(registration.Regional_Head_Status))
                //    return Forbid();

                registration.Regional_Head_Status = decision;
                registration.Regional_Head_Remarks = remarks;
                registration.Regional_Head_ApprovedBy = approvedBy;
                registration.Regional_Head_Date = DateTime.UtcNow;
            }


            // =====================================================
            // VERTICAL HEAD
            // =====================================================

            else if (roles.Contains("VerticalHead"))
            {
                if (registration.Regional_Head_Status != "Approved")
                    return Forbid();

                registration.Vertical_Head_Status = decision;
                registration.Vertical_Head_Remarks = remarks;
                registration.Vertical_Head_ApprovedBy = approvedBy;
                registration.Vertical_Head_Date = DateTime.UtcNow;
            }


            // =====================================================
            // FINANCE
            // =====================================================

            else if (roles.Contains("Finance"))
            {
                if (registration.Vertical_Head_Status != "Approved")
                    return Forbid();

                registration.Finance_Status = decision;
                registration.Finance_Remarks = remarks;
                registration.Finance_ApprovedBy = approvedBy;
                registration.Finance_Date = DateTime.UtcNow;
            }


            // =====================================================
            // CEO
            // =====================================================

            else if (roles.Contains("CEO"))
            {
                if (registration.Finance_Status != "Approved")
                    return Forbid();

                registration.CEO_Status = decision;
                registration.CEO_Remarks = remarks;
                registration.CEO_ApprovedBy = approvedBy;
                registration.CEO_Date = DateTime.UtcNow;
            }

            else
            {
                return Forbid();
            }


            // =====================================================
            // OVERALL APPROVAL STATUS
            // =====================================================

            if (decision == "Rejected")
            {
                // Internal workflow status
                registration.OverallApprovalStatus = "Rejected";

                // TP-facing status
                registration.Status = "Rejected";
            }
            else if (
                registration.Regional_Head_Status == "Approved" &&
                registration.Vertical_Head_Status == "Approved" &&
                registration.Finance_Status == "Approved" &&
                registration.CEO_Status == "Approved")
            {
                // All approval stages completed
                registration.OverallApprovalStatus = "Approved";

                // TP-facing status
                registration.Status = "Approved";
            }
            else
            {
                // Approval process is still continuing
                registration.OverallApprovalStatus = "Pending";

                // TP-facing status
                registration.Status = "Under Review";
            }


            registration.UpdatedOn = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id });
        }
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> Decide(
        //int id,
        //string decision,
        //string? remarks)
        //{
        //    if (decision is not ("Approved" or "Rejected"))
        //        return BadRequest();

        //    var registration = await _context.TPRegistrations
        //        .FirstOrDefaultAsync(x => x.Id == id);

        //    if (registration == null)
        //        return NotFound();

        //    // =========================================================
        //    // LOGGED-IN USER
        //    // =========================================================

        //    var user = await _userManager.GetUserAsync(User);

        //    if (user == null)
        //        return Unauthorized();

        //    var roles = await _userManager.GetRolesAsync(user);

        //    // =========================================================
        //    // GET EMPLOYEE
        //    // =========================================================

        //    var employee = await _context.Employee
        //        .FirstOrDefaultAsync(x => x.ApplicationUserId == user.Id);

        //    if (employee == null)
        //        return Forbid();

        //    var employeeName =
        //        $"{employee.FirstName} {employee.LastName}".Trim();

        //    var approvedBy =
        //        $"{employeeName} ({employee.EmployeeCode})";


        //    // =========================================================
        //    // REGIONAL HEAD
        //    // =========================================================

        //    if (roles.Contains("RegionalHead"))
        //    {
        //        // First approval stage
        //        if (!string.IsNullOrEmpty(registration.Regional_Head_Status))
        //            return Forbid();

        //        registration.Regional_Head_Status = decision;
        //        registration.Regional_Head_Remarks = remarks;
        //        registration.Regional_Head_ApprovedBy = approvedBy;
        //        registration.Regional_Head_Date = DateTime.UtcNow;
        //    }


        //    // =========================================================
        //    // VERTICAL HEAD
        //    // =========================================================

        //    else if (roles.Contains("VerticalHead"))
        //    {
        //        // Regional Head must approve first
        //        if (registration.Regional_Head_Status != "Approved")
        //            return Forbid();

        //        // Already decided
        //        if (!string.IsNullOrEmpty(registration.Vertical_Head_Status))
        //            return Forbid();

        //        registration.Vertical_Head_Status = decision;
        //        registration.Vertical_Head_Remarks = remarks;
        //        registration.Vertical_Head_ApprovedBy = approvedBy;
        //        registration.Vertical_Head_Date = DateTime.UtcNow;
        //    }


        //    // =========================================================
        //    // FINANCE
        //    // =========================================================

        //    else if (roles.Contains("Finance"))
        //    {
        //        // Vertical Head must approve first
        //        if (registration.Vertical_Head_Status != "Approved")
        //            return Forbid();

        //        // Already decided
        //        if (!string.IsNullOrEmpty(registration.Finance_Status))
        //            return Forbid();

        //        registration.Finance_Status = decision;
        //        registration.Finance_Remarks = remarks;
        //        registration.Finance_ApprovedBy = approvedBy;
        //        registration.Finance_Date = DateTime.UtcNow;
        //    }


        //    // =========================================================
        //    // CEO
        //    // =========================================================

        //    else if (roles.Contains("CEO"))
        //    {
        //        // Finance must approve first
        //        if (registration.Finance_Status != "Approved")
        //            return Forbid();

        //        // Already decided
        //        if (!string.IsNullOrEmpty(registration.CEO_Status))
        //            return Forbid();

        //        registration.CEO_Status = decision;
        //        registration.CEO_Remarks = remarks;
        //        registration.CEO_ApprovedBy = approvedBy;
        //        registration.CEO_Date = DateTime.UtcNow;
        //    }


        //    // =========================================================
        //    // UNKNOWN / UNAUTHORIZED ROLE
        //    // =========================================================

        //    else
        //    {
        //        return Forbid();
        //    }


        //    // =========================================================
        //    // OVERALL STATUS
        //    // =========================================================

        //    if (decision == "Rejected")
        //    {
        //        registration.OverallApprovalStatus = "Rejected";
        //    }
        //    else if (
        //        registration.Regional_Head_Status == "Approved" &&
        //        registration.Vertical_Head_Status == "Approved" &&
        //        registration.Finance_Status == "Approved" &&
        //        registration.CEO_Status == "Approved")
        //    {
        //        registration.OverallApprovalStatus = "Approved";
        //    }
        //    else
        //    {
        //        registration.OverallApprovalStatus = "Pending";
        //    }


        //    // =========================================================
        //    // UPDATE TIMESTAMP
        //    // =========================================================

        //    registration.UpdatedOn = DateTime.UtcNow;

        //    await _context.SaveChangesAsync();

        //    return RedirectToAction(nameof(Details), new { id });
        //}
    }
}
