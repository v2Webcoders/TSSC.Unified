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

        [HttpGet]
        public async Task<IActionResult> InspectionList()
        {
            var centers = await _context.TrainingCenter
                .AsNoTracking()
                .OrderByDescending(x => x.CreatedDate)
                .Where(x=>x.FinanceStatus== "Approved" && x.PaymentStatus=="Success")
                .Select(x => new TrainingCenterViewModel
                {
                    Id = x.Id,
                    TrainingCenterName = x.TrainingCenterName,
                    TrainingCenterCode = x.TrainingCenterCode,
                    CompleteAddress = x.CompleteAddress,
                    PaymentStatus = x.PaymentStatus,
                    FinanceStatus = x.FinanceStatus,
                    InspectionDate = x.InspectionDate,
                    InspectionStatus = x.InspectionStatus,
                    InspectionCreatedDate = x.InspectionCreatedDate,
                    SchemeName = _context.Scheme
                        .Where(s => s.SchemeId == x.SchemeId)
                        .Select(s => s.SchemeCode)
                        .FirstOrDefault(),

                    JobRoleTitle = _context.JobRoles
                        .Where(j => j.Id == x.JobRoleId)
                        .Select(j => j.JobRoleTitle)
                        .FirstOrDefault()
                })
                .ToListAsync();

            return View(centers);
        }


        [HttpGet]
        public async Task<IActionResult> Information(int id)
        {
            var center = await _context.TrainingCenter
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);

            if (center == null)
                return NotFound();

            var model = new TrainingCenterViewModel
            {
                Id = center.Id,
                SchemeId = center.SchemeId,
                ProjectId = center.ProjectId,
                JobRoleId = center.JobRoleId,

                // Partner Information
                TrainingCenterName = center.TrainingCenterName,
                TrainingCenterCode = center.TrainingCenterCode,
                TrainingCenterSPOCName = center.TrainingCenterSPOCName,
                TrainingCenterSPOCEmail = center.TrainingCenterSPOCEmail,
                SPOCContactNumber = center.SPOCContactNumber,
                TrainingCenterPhoneNumber = center.TrainingCenterPhoneNumber,
                CompleteAddress = center.CompleteAddress,
                CenterFloor = center.CenterFloor,
                Landmark = center.Landmark,

                // Center Information
                CenterOwnershipType = center.CenterOwnershipType,
                FranchisePartnerName = center.FranchisePartnerName,
                CenterType = center.CenterType,
                SelfFunded = center.SelfFunded,
                NSDC = center.NSDC,
                STAR = center.STAR,
                MoRD = center.MoRD,
                StateGovernmentSchemes = center.StateGovernmentSchemes,
                OtherCentralGovernmentSchemes =
                    center.OtherCentralGovernmentSchemes,

                UsableAreaSqFt = center.UsableAreaSqFt,
                NumberOfClassrooms = center.NumberOfClassrooms,
                NumberOfFloors = center.NumberOfFloors,
                NumberOfLabs = center.NumberOfLabs,
                CenterDescription = center.CenterDescription,
                EquipmentDetails = center.EquipmentDetails,

                // Organization Information
                OrganizationName = center.OrganizationName,
                SigningAuthorityName = center.SigningAuthorityName,
                MobileNumber = center.MobileNumber,
                PANNumber = center.PANNumber,
                EmailId = center.EmailId,

                // Inspection Information
                InspectionDate = center.InspectionDate,
                InspectionStatus = center.InspectionStatus,
                InspectionCreatedDate = center.InspectionCreatedDate,
                //InspectionRemarks = center.InspectionRemarks,

                // Scheme and Job Role
                SchemeName = await _context.Scheme
                    .Where(s => s.SchemeId == center.SchemeId)
                    .Select(s => s.SchemeName)
                    .FirstOrDefaultAsync(),

                JobRoleTitle = await _context.JobRoles
                    .Where(j => j.Id == center.JobRoleId)
                    .Select(j => j.JobRoleTitle)
                    .FirstOrDefaultAsync(),

                QPHours = await _context.JobRoles
                    .Where(j => j.Id == center.JobRoleId)
                    .Select(j => (decimal?)j.QPHours)
                    .FirstOrDefaultAsync()
            };
            ViewBag.ApproachRoadPhotoPath = center.ApproachRoadPhotoPath;
            ViewBag.OutsideClassroomPhotoPath = center.OutsideClassroomPhotoPath;
            ViewBag.InsideClassroomPhotoPath = center.InsideClassroomPhotoPath;
            ViewBag.LabPhotoPath = center.LabPhotoPath;
            ViewBag.CCTVPhotoPath = center.CCTVPhotoPath;
            ViewBag.CompanySealPath = center.CompanySealPath;

            return View(model);
        }
        //[HttpGet]
        //public async Task<IActionResult> ScheduleInspection(int id)
        //{
        //    var center = await _context.TrainingCenter
        //        .AsNoTracking()
        //        .FirstOrDefaultAsync(x => x.Id == id);

        //    if (center == null)
        //    {
        //        return NotFound();
        //    }

        //    var model = new TrainingCenterViewModel
        //    {
        //        Id = center.Id,
        //        TrainingCenterName = center.TrainingCenterName,
        //        TrainingCenterCode = center.TrainingCenterCode,
        //        JobRoleId = center.JobRoleId,
        //        SchemeId = center.SchemeId,
        //        InspectionDate = center.InspectionDate,
        //        InspectionStatus = center.InspectionStatus,

        //        JobRoleTitle = await _context.JobRoles
        //            .AsNoTracking()
        //            .Where(x => x.Id == center.JobRoleId)
        //            .Select(x => x.JobRoleTitle)
        //            .FirstOrDefaultAsync(),

        //        SchemeName = await _context.Scheme
        //            .AsNoTracking()
        //            .Where(x => x.SchemeId == center.SchemeId)
        //            .Select(x => x.SchemeName)
        //            .FirstOrDefaultAsync()
        //    };

        //    return View(model);
        //}



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ScheduleInspection(
            int id,
            DateTime? inspectionDate,
            string? inspectionMode,
            string? inspectionLink)
        {
            var center = await _context.TrainingCenter
                .FirstOrDefaultAsync(x => x.Id == id);

            if (center == null)
            {
                return NotFound();
            }

            if (!inspectionDate.HasValue ||
                inspectionDate.Value <= DateTime.Now)
            {
                TempData["Error"] =
                    "Please select a future inspection date and time.";

                return RedirectToAction(nameof(ScheduleInspection), new { id });
            }

            inspectionMode = inspectionMode?.Trim();

            if (inspectionMode != "Online" && inspectionMode != "Offline")
            {
                TempData["Error"] =
                    "Please select Online or Offline inspection mode.";

                return RedirectToAction(nameof(ScheduleInspection), new { id });
            }

            inspectionLink = inspectionLink?.Trim();

            if (inspectionMode == "Online")
            {
                if (string.IsNullOrWhiteSpace(inspectionLink))
                {
                    TempData["Error"] =
                        "Inspection link is required for Online mode.";

                    return RedirectToAction(nameof(ScheduleInspection), new { id });
                }

                if (inspectionLink.Length > 1000 ||
                    !Uri.TryCreate(
                        inspectionLink,
                        UriKind.Absolute,
                        out var uri) ||
                    (uri.Scheme != Uri.UriSchemeHttp &&
                     uri.Scheme != Uri.UriSchemeHttps))
                {
                    TempData["Error"] =
                        "Please enter a valid HTTP or HTTPS inspection link.";

                    return RedirectToAction(nameof(ScheduleInspection), new { id });
                }
            }

            center.InspectionDate = inspectionDate.Value;
            center.InspectionStatus = "Scheduled";
            center.InspectionMode = inspectionMode;
            center.InspectionLink =
                inspectionMode == "Online" ? inspectionLink : null;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Inspection scheduled successfully.";

            return RedirectToAction(nameof(Information), new { id = center.Id });
        }

    }
}
