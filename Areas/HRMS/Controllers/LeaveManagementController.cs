using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using QUIZAPP.Areas.HRMS.Controllers;
using QUIZAPP.Models;
using QUIZAPP;
using TSSC.Unified.ViewModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using TSSC.Unified.Models;

namespace TSSC.Unified.Areas.HRMS.Controllers
{
    [Area("HRMS")]
    [Authorize]
    public class LeaveManagementController : Controller
    {
        private readonly ILogger<EmployeeController> _logger;
        private readonly AppdbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IWebHostEnvironment _environment;
        public LeaveManagementController(
            ILogger<EmployeeController> logger,
            AppdbContext context,
            UserManager<AppUser> userManager,
            RoleManager<IdentityRole> roleManager, IWebHostEnvironment environment)
        {
            _logger = logger;
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
            _environment = environment;
        }

        //HR ROLE LEAVE MANAGEMENT
        public async Task<IActionResult> MyLeaves()
        {
            var applicationUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var employee = await _context.Employee
                .FirstOrDefaultAsync(x => x.ApplicationUserId == applicationUserId);

            if (employee == null)
            {
                return NotFound("Employee not found.");
            }

            ViewBag.EmployeeId = employee.EmployeeId;
            ViewBag.IsHR = User.IsInRole("HR");

            IQueryable<LeaveRequest> query = _context.LeaveRequest
                .Include(x => x.LeaveType);

            // HR can see all leave requests
            if (!User.IsInRole("HR"))
            {
                query = query.Where(x =>
                    x.EmployeeId == employee.EmployeeId ||
                    x.ApproverId == employee.EmployeeId);
            }

            var leaveRequests = await query
                .OrderByDescending(x => x.CreatedDate)
                .ToListAsync();

            return View(leaveRequests);
        }

        public async Task<IActionResult> Approve(int id)
        {
            var applicationUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var employee = await _context.Employee
                .FirstOrDefaultAsync(x => x.ApplicationUserId == applicationUserId);

            if (employee == null)
                return NotFound();

            var leave = await _context.LeaveRequest
                .FirstOrDefaultAsync(x => x.LeaveRequestId == id);

            if (leave == null)
                return NotFound();

            leave.Status = "Approved";
            leave.ApprovedBy = employee.EmployeeId;
            leave.ApprovedDate = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Leave request approved successfully.";

            return RedirectToAction(nameof(MyLeaves));
        }

        public async Task<IActionResult> Reject(int id)
        {
            var applicationUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var employee = await _context.Employee
                .FirstOrDefaultAsync(x => x.ApplicationUserId == applicationUserId);

            if (employee == null)
                return NotFound();

            var leave = await _context.LeaveRequest
                .FirstOrDefaultAsync(x => x.LeaveRequestId == id);

            if (leave == null)
                return NotFound();

            leave.Status = "Rejected";
            leave.ApprovedBy = employee.EmployeeId;
            leave.ApprovedDate = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Leave request rejected successfully.";

            return RedirectToAction(nameof(MyLeaves));
        }
        public async Task<IActionResult> Balance(int? employeeId)
        {
            var employees = await _context.Employee
                .Where(x => x.IsActive)
                .OrderBy(x => x.FirstName)
                .ThenBy(x => x.LastName)
                .ToListAsync();

            ViewBag.Employees = employees;

            var query = _context.EmployeeLeaveBalance
                .Include(x => x.Employee)
                .Include(x => x.LeaveType)
                .Where(x => x.Employee.IsActive)
                .AsQueryable();

            // Filter by employee
            if (employeeId.HasValue)
            {
                query = query.Where(x =>
                    x.EmployeeId == employeeId.Value);

                var selectedEmployee = employees
                    .FirstOrDefault(x =>
                        x.EmployeeId == employeeId.Value);

                if (selectedEmployee != null)
                {
                    ViewBag.SelectedEmployeeId = employeeId.Value;

                    ViewBag.SelectedEmployeeName =
                        selectedEmployee.FirstName + " " +
                        selectedEmployee.LastName;
                }
            }

            var balances = await query
                .OrderBy(x => x.Employee.FirstName)
                .ThenBy(x => x.LeaveType.DisplayOrder)
                .ToListAsync();

            return View(balances);
        }
        [HttpGet]
        public async Task<IActionResult> MyBalance()
        {
            var applicationUserId =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            var employee = await _context.Employee
                .FirstOrDefaultAsync(x =>
                    x.ApplicationUserId == applicationUserId);

            if (employee == null)
                return NotFound("Employee record not found.");

            var balances = await _context.EmployeeLeaveBalance
                .Include(x => x.LeaveType)
                .Where(x => x.EmployeeId == employee.EmployeeId)
                .OrderBy(x => x.LeaveType.DisplayOrder)
                .ToListAsync();

            return View(balances);
        }
    }
}
