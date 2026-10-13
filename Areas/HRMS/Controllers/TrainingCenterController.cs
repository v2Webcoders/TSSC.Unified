using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QUIZAPP;
using QUIZAPP.Models;
using QUIZAPP.Services;
using TSSC.Unified.ViewModel;

namespace TSSC.Unified.Areas.HRMS.Controllers
{
    [Area("HRMS")]
    public class TrainingCenterController :Controller
    {
        private readonly AppdbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly EmailService _emailService;
        public TrainingCenterController(AppdbContext context,
        IWebHostEnvironment environment,
        UserManager<AppUser> userManager,
        RoleManager<IdentityRole> roleManager, EmailService emailService)
        {
            _context = context;
            _environment = environment;
            _userManager = userManager;
            _roleManager = roleManager;
            _emailService = emailService;
        }

        [HttpGet]
        public async Task<IActionResult> PaymentApproval(string tab = "Pending")
        {
            tab = tab?.Trim() ?? "Pending";

            if (tab != "Pending" && tab != "Approved" && tab != "Rejected")
                tab = "Pending";

            var query = _context.TrainingCenter
                .AsNoTracking()
                .Where(x => x.PaymentStatus == "Success");

            if (tab == "Pending")
            {
                query = query.Where(x => x.FinanceStatus == "Pending");
            }
            else if (tab == "Approved")
            {
                query = query.Where(x => x.FinanceStatus == "Approved");
            }
            else if (tab == "Rejected")
            {
                query = query.Where(x => x.FinanceStatus == "Rejected");
            }

            var centers = await query
                .OrderByDescending(x => x.PaymentDate)
                .Select(x => new TrainingCenterViewModel
                {
                    Id = x.Id,
                    TrainingCenterName = x.TrainingCenterName,

                    SchemeName = _context.Scheme
                        .Where(s => s.SchemeId == x.SchemeId)
                        .Select(s => s.SchemeName)
                        .FirstOrDefault(),

                    JobRoleTitle = _context.JobRoles
                        .Where(j => j.Id == x.JobRoleId)
                        .Select(j => j.JobRoleTitle)
                        .FirstOrDefault(),

                    PaymentStatus = x.PaymentStatus,
                    PaymentDate = x.PaymentDate,
                    PaymentBy = x.PaymentBy,
                    FinanceRemark = x.FinanceRemark,
                    FinanceApprovedBy = x.FinanceApprovedBy,
                    FinanceApprovedDate = x.FinanceApprovedDate,
                    FinanceStatus = x.FinanceStatus
                })
                .ToListAsync();

            ViewBag.ActiveTab = tab;

            return View(centers);
        }

        //[HttpGet]
        //public async Task<IActionResult> PaymentApproval(string tab = "Pending")
        //{
        //    var centers = await _context.TrainingCenter
        //        .AsNoTracking()
        //        .Where(x =>
        //            x.PaymentStatus == "Success" &&
        //            x.FinanceApprovedBy == null &&
        //            x.FinanceStatus == "Pending")
        //        .OrderByDescending(x => x.PaymentDate)
        //        .Select(x => new TrainingCenterViewModel
        //        {
        //            Id = x.Id,
        //            TrainingCenterName = x.TrainingCenterName,
        //            PaymentStatus = x.PaymentStatus,
        //            PaymentDate = x.PaymentDate,
        //            PaymentBy = x.PaymentBy,
        //            FinanceRemark = x.FinanceRemark,
        //            FinanceApprovedBy = x.FinanceApprovedBy,
        //            FinanceApprovedDate = x.FinanceApprovedDate,
        //            FinanceStatus = x.FinanceStatus,
        //            SchemeName = _context.Scheme
        //        .Where(s => s.SchemeId == x.SchemeId)
        //        .Select(s => s.SchemeCode)
        //        .FirstOrDefault(),
        //            JobRoleTitle = _context.JobRoles
        //        .Where(j => j.Id == x.JobRoleId)
        //        .Select(j => j.JobRoleTitle)
        //        .FirstOrDefault(),
        //        })
        //        .ToListAsync();

        //    ViewBag.Tab = tab;

        //    return View(centers);
        //}

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessPaymentApproval(
            int id,
            string decision,
            string? financeRemark)
        {
            if (decision != "Approved" && decision != "Rejected")
            {
                TempData["Error"] = "Invalid approval decision.";

                return RedirectToAction(
                    nameof(PaymentApproval),
                    new { tab = "Pending" });
            }

            var center = await _context.TrainingCenter
                .FirstOrDefaultAsync(x => x.Id == id);

            if (center == null)
                return NotFound();
            if (center.PaymentStatus != "Success" ||
                center.FinanceStatus != "Pending" ||
                center.FinanceApprovedBy != null)
            {
                TempData["Error"] =
                    "This payment is not available for approval.";

                return RedirectToAction(
                    nameof(PaymentApproval),
                    new { tab = "Pending" });
            }

            //if (string.IsNullOrWhiteSpace(financeRemark))
            //{
            //    TempData["Error"] = "Please enter a finance remark.";

            //    return RedirectToAction(
            //        nameof(PaymentApproval),
            //        new { tab = "Pending" });
            //}

            var currentUser = await _userManager.GetUserAsync(User);

            if (currentUser == null)
                return Challenge();

            var financeEmployee = await _context.Employee
                .FirstOrDefaultAsync(
                    x => x.ApplicationUserId == currentUser.Id);

            if (financeEmployee == null)
            {
                TempData["Error"] =
                    "Finance employee record not found.";

                return RedirectToAction(
                    nameof(PaymentApproval),
                    new { tab = "Pending" });
            }

            center.FinanceStatus = decision;
            center.FinanceRemark = financeRemark.Trim();
            center.FinanceApprovedBy = financeEmployee.EmployeeId;
            center.FinanceApprovedDate = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["Success"] = decision == "Approved"
                ? "Training Partner payment approved successfully."
                : "Training Partner payment rejected successfully.";

            return RedirectToAction(
                nameof(PaymentApproval),
                new { tab = "Pending" });
        }

    }
}
