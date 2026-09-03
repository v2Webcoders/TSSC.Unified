using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QUIZAPP;
using TSSC.Unified.Models;
using TSSC.Unified.ViewModel;

namespace TSSC.Unified.Areas.HRMS.Controllers
{
    [Area("HRMS")]
    [Authorize(Roles = "HR")]
    public class LeaveAdjustmentController : Controller
    {
        private readonly AppdbContext _context;

        public LeaveAdjustmentController(AppdbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var adjustments = await _context.LeaveAdjustment
                .Include(x => x.Employee)
                .Include(x => x.LeaveType)
                .Include(x => x.AdjustedByEmployee)
                .OrderByDescending(x => x.AdjustedOn)
                .ToListAsync();

            return View(adjustments);
        }
        public async Task<IActionResult> Create()
        {
            ViewBag.Employees = await _context.Employee
                .OrderBy(x => x.FirstName)
                .ToListAsync();

            ViewBag.LeaveTypes = await _context.LeaveType
                .Where(x => x.LeaveTypeName== "Comp Off")
                .ToListAsync();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(LeaveAdjustmentVM model)
        {
            // Reload dropdowns if validation fails
            ViewBag.Employees = await _context.Employee
                .Where(x => x.IsActive)
                .Select(x => new
                {
                    x.EmployeeId,
                    EmployeeDisplayName =
                        x.FirstName + " " +
                        (x.LastName ?? "") +
                        " (" + x.EmployeeCode + ")"
                })
                .ToListAsync();

            ViewBag.LeaveTypes = await _context.LeaveType
                .Where(x => x.IsActive)
                .ToListAsync();

            if (!ModelState.IsValid)
                return View(model);

            // Get existing employee leave balance
            var balance = await _context.EmployeeLeaveBalance
                .FirstOrDefaultAsync(x =>
                    x.EmployeeId == model.EmployeeId &&
                    x.LeaveTypeId == model.LeaveTypeId);

            if (balance == null)
            {
                ModelState.AddModelError(
                    "",
                    "Leave balance was not found for the selected employee and leave type.");

                return View(model);
            }

            // Current balance before adjustment
            var currentBalance =
                balance.OpeningBalance +
                balance.Adjustment -
                balance.UsedLeaves;

            // Credit
            if (model.AdjustmentType == "Credit")
            {
                balance.Adjustment += model.Days;
            }
            // Deduct
            else if (model.AdjustmentType == "Deduct")
            {
                if (currentBalance < model.Days)
                {
                    ModelState.AddModelError(
                        "",
                        $"Insufficient leave balance. Current balance is {currentBalance} days.");

                    return View(model);
                }

                balance.Adjustment -= model.Days;
            }
            else
            {
                ModelState.AddModelError(
                    nameof(model.AdjustmentType),
                    "Please select an adjustment type.");

                return View(model);
            }

            balance.LastUpdated = DateTime.Now;

            // Get HR employee who is making the adjustment
            var currentUserId =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            var hr = await _context.Employee
                .FirstOrDefaultAsync(x =>
                    x.ApplicationUserId == currentUserId);

            if (hr == null)
            {
                ModelState.AddModelError(
                    "",
                    "HR employee record could not be found.");

                return View(model);
            }

            // Create adjustment history
            var adjustment = new LeaveAdjustment
            {
                EmployeeId = model.EmployeeId,
                LeaveTypeId = model.LeaveTypeId,
                AdjustmentType = model.AdjustmentType,
                Days = model.Days,
                Reason = model.Reason,
                AdjustedBy = hr.EmployeeId,
                AdjustedOn = DateTime.Now
            };

            _context.LeaveAdjustment.Add(adjustment);

            await _context.SaveChangesAsync();

            TempData["Success"] =
                $"Leave {model.AdjustmentType.ToLower()}ed successfully.";

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> GetCurrentBalance(
    int employeeId,
    int leaveTypeId)
        {
            var balance = await _context.EmployeeLeaveBalance
                .FirstOrDefaultAsync(x =>
                    x.EmployeeId == employeeId &&
                    x.LeaveTypeId == leaveTypeId);

            if (balance == null)
            {
                return Json(new
                {
                    success = false,
                    currentBalance = 0
                });
            }

            return Json(new
            {
                success = true,
                currentBalance = balance.CurrentBalance
            });
        }
    }
}
