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
            var regionalHeads =
     await _userManager.GetUsersInRoleAsync("RegionalHead");

            var regionalHeadIds = regionalHeads
                .Select(u => u.Id)
                .ToList();

            var employees = await _context.Employee
                .Where(e => e.IsActive == true)
                .ToListAsync();

            var regionalHeadEmployees = employees
                .Where(e => regionalHeadIds.Contains(e.ApplicationUserId))
                .Select(e => new
                {
                    EmployeeId = e.EmployeeId,
                    Name = e.FirstName + " " + e.LastName,
                    Email = e.OfficialEmail
                })
                .ToList();

            ViewBag.RegionalHeads = regionalHeadEmployees;

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
                InspectionRemarks = center.InspectionRemarks,
                InspectionMode = center.InspectionMode,

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

                return RedirectToAction(nameof(Information), new { id });
            }

            inspectionMode = inspectionMode?.Trim();

            if (inspectionMode != "Online" && inspectionMode != "Offline")
            {
                TempData["Error"] =
                    "Please select Online or Offline inspection mode.";

                return RedirectToAction(nameof(Information), new { id });
            }

            inspectionLink = inspectionLink?.Trim();

            if (inspectionMode == "Online")
            {
                if (string.IsNullOrWhiteSpace(inspectionLink))
                {
                    TempData["Error"] = "Please enter the online inspection link.";
                    return RedirectToAction(nameof(Information), new { id });
                }

                if (!Uri.TryCreate(
                        inspectionLink,
                        UriKind.Absolute,
                        out var uri) ||
                    (uri.Scheme != Uri.UriSchemeHttp &&
                     uri.Scheme != Uri.UriSchemeHttps) ||
                    inspectionLink.Length > 1000)
                {
                    TempData["Error"] =
                        "Please enter a valid link starting with https:// or http://.";

                    return RedirectToAction(nameof(Information), new { id });
                }
            }
            else
            {
                inspectionLink = null;
            }

            center.InspectionDate = inspectionDate.Value;
            center.InspectionStatus = "Scheduled";
            center.InspectionMode = inspectionMode;
            center.InspectionLink =
                inspectionMode == "Online" ? inspectionLink : null;
            center.InspectionCreatedDate = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Inspection scheduled successfully.";

            return RedirectToAction(nameof(Information), new { id = center.Id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateInspectionStatus(
            int id,
            string actionType,
            string inspectionRemarks)
        {
            var center = await _context.TrainingCenter
                .FirstOrDefaultAsync(x => x.Id == id);

            if (center == null)
                return NotFound();

            if (string.IsNullOrWhiteSpace(inspectionRemarks))
            {
                TempData["Error"] = "Please enter inspection remarks.";
                return RedirectToAction(nameof(Information), new { id });
            }

            if (inspectionRemarks.Trim().Length > 1000)
            {
                TempData["Error"] = "Remarks cannot exceed 1000 characters.";
                return RedirectToAction(nameof(Information), new { id });
            }

            if (!string.Equals(
                center.InspectionStatus,
                "Scheduled",
                StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] = "Only scheduled inspections can be updated.";
                return RedirectToAction(nameof(Information), new { id });
            }

            switch (actionType)
            {
                case "OnHold":
                    center.InspectionStatus = "On Hold";
                    break;

                case "Reject":
                    center.InspectionStatus = "Rejected";
                    break;

                default:
                    TempData["Error"] = "Invalid inspection action.";
                    return RedirectToAction(nameof(Information), new { id });
            }

            center.InspectionRemarks = inspectionRemarks.Trim();

            await _context.SaveChangesAsync();

            TempData["Success"] = "Inspection status updated successfully.";

            return RedirectToAction(nameof(Information), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendToRegionalHead(
            int id,
            int regionalHeadEmployeeId,
            string inspectionRemarks)
        {
            var center = await _context.TrainingCenter
                .FirstOrDefaultAsync(x => x.Id == id);

            if (center == null)
                return NotFound();

            if (string.IsNullOrWhiteSpace(inspectionRemarks))
            {
                TempData["Error"] = "Please enter inspection remarks.";
                return RedirectToAction(nameof(Information), new { id });
            }

            if (inspectionRemarks.Trim().Length > 1000)
            {
                TempData["Error"] = "Remarks cannot exceed 1000 characters.";
                return RedirectToAction(nameof(Information), new { id });
            }

            if (!string.Equals(
                center.InspectionStatus,
                "Scheduled",
                StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] =
                    "Only scheduled inspections can be forwarded.";

                return RedirectToAction(nameof(Information), new { id });
            }

            // Verify the selected employee exists.
            var employee = await _context.Employee
    .FirstOrDefaultAsync(e =>e.EmployeeId == regionalHeadEmployeeId);

            if (employee == null)
            {
                TempData["Error"] = "Selected Regional Head was not found.";
                return RedirectToAction(nameof(Information), new { id });
            }

            // Verify that this employee actually has the Regional Head role
            // using your project's existing role-mapping logic here.

            center.InspectionRemarks = inspectionRemarks.Trim();
            center.RegionalHeadEmployeeId = regionalHeadEmployeeId;
            center.RegionalHeadStatus = "Pending";
            center.RegionalHeadRemarks = null;
            center.SentToRegionalHeadDate = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Training center sent to Regional Head successfully.";

            return RedirectToAction(nameof(Information), new { id });
        }

        [HttpGet]
        public async Task<IActionResult> RegionalHeadInspection(string tab = "Pending")
        {
            var userId = _userManager.GetUserId(User);

            if (string.IsNullOrEmpty(userId))
                return Challenge();

            var employee = await _context.Employee
                .AsNoTracking()
                .FirstOrDefaultAsync(e =>
                    e.ApplicationUserId == userId &&
                    e.IsActive == true);

            if (employee == null)
                return Forbid();

            var query = _context.TrainingCenter
                .AsNoTracking()
                .Where(x =>
                    x.RegionalHeadEmployeeId == employee.EmployeeId);

            switch (tab)
            {
                case "Pending":
                    query = query.Where(x =>
                        x.RegionalHeadStatus == "Pending");
                    break;

                case "OnHold":
                    query = query.Where(x =>
                        x.RegionalHeadStatus == "On Hold");
                    break;

                case "Rejected":
                    query = query.Where(x =>
                        x.RegionalHeadStatus == "Rejected");
                    break;

                case "Approved":
                    query = query.Where(x =>
                        x.RegionalHeadStatus == "Approved");
                    break;

                default:
                    tab = "Pending";
                    query = query.Where(x =>
                        x.RegionalHeadStatus == "Pending");
                    break;
            }

            var centers = await query
                .OrderByDescending(x => x.SentToRegionalHeadDate)
                .Select(x => new TrainingCenterViewModel
                {
                    Id = x.Id,
                    TrainingCenterName = x.TrainingCenterName,
                    TrainingCenterCode = x.TrainingCenterCode,
                    CompleteAddress = x.CompleteAddress,
                    InspectionDate = x.InspectionDate,
                    InspectionStatus = x.InspectionStatus,
                    PaymentStatus = x.PaymentStatus,
                    FinanceStatus = x.FinanceStatus,
                    InspectionCreatedDate = x.InspectionCreatedDate,
                    RegionalHeadStatus = x.RegionalHeadStatus,
                    SchemeName = _context.Scheme
                        .Where(s => s.SchemeId == x.SchemeId)
                        .Select(s => s.SchemeName)
                        .FirstOrDefault(),
                    JobRoleTitle = _context.JobRoles
                        .Where(j => j.Id == x.JobRoleId)
                        .Select(j => j.JobRoleTitle)
                        .FirstOrDefault()
                })
                .ToListAsync();

            ViewBag.ActiveTab = tab;

            return View(centers);
        }
        [HttpGet]
        public async Task<IActionResult> RegionalHeadDetails(int id, string tab = "Pending")
        {

            // 1. Get logged-in user
            var userId = _userManager.GetUserId(User);

            if (string.IsNullOrEmpty(userId))
                return Challenge();

            // 2. Find the logged-in Regional Head's Employee record
            var employee = await _context.Employee
                .AsNoTracking()
                .FirstOrDefaultAsync(e =>
                    e.ApplicationUserId == userId &&
                    e.IsActive == true);

            if (employee == null)
            {
                TempData["Error"] = "Employee record not found.";
                return RedirectToAction(nameof(RegionalHeadInspection));
            }

            // 3. Only allow access to the assigned Training Center
            var center = await _context.TrainingCenter
     .AsNoTracking()
     .FirstOrDefaultAsync(x =>
         x.Id == id &&
         (
             x.RegionalHeadEmployeeId == employee.EmployeeId ||
             x.RegionalHeadStatus == "Approved"
         ));

            if (center == null)
            {
                TempData["Error"] =
                    "This Training Center is not assigned to you.";

                return RedirectToAction(
                    nameof(RegionalHeadInspection),
                    new { tab = "Pending" });
            }

            // 4. Map all the details used by the existing Information page
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

            ViewBag.IsRegionalHeadView = true;
            ViewBag.RegionalHeadStatus = center.RegionalHeadStatus;
            ViewBag.RegionalHeadRemarks = center.RegionalHeadRemarks;
            ViewBag.RegionalInspectionRemarks = center.InspectionRemarks;
            ViewBag.IsVerticalHeadWorkflow =
    center.RegionalHeadStatus == "Approved";

            ViewBag.RegionalHeadStatus = center.RegionalHeadStatus;
            ViewBag.VerticalHeadStatus = center.VerticalHeadStatus;
            ViewBag.VerticalHeadRemarks = center.VerticalHeadRemarks;
            ViewBag.BusinessHeadStatus = center.BusinessHeadStatus;
            ViewBag.BusinessHeadRemarks = center.BusinessHeadRemarks;
            ViewBag.ActiveTab = tab;
            ViewBag.CurrentApprovalRole = User.IsInRole("RegionalHead")
    ? "RegionalHead"
    : User.IsInRole("VerticalHead")
        ? "VerticalHead"
        : User.IsInRole("BusinessHead")
            ? "BusinessHead"
            : ""; ;
              ViewBag.ActiveTab = tab;
            return View("RegionalHeadDetails", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegionalHeadUpdateStatus(
    int id,
    string status,
    string regionalHeadRemarks)
        {
            var userId = _userManager.GetUserId(User);

            if (string.IsNullOrEmpty(userId))
                return Challenge();
            var employee = await _context.Employee
                .FirstOrDefaultAsync(e =>
                    e.ApplicationUserId == userId &&
                    e.IsActive == true);

            if (employee == null)
                return Forbid();
            var center = await _context.TrainingCenter
                .FirstOrDefaultAsync(x =>
                    x.Id == id &&
                    x.RegionalHeadEmployeeId == employee.EmployeeId);

            if (center == null)
                return NotFound();

            // Allow only valid statuses
            var allowedStatuses = new[]
            {
        "On Hold",
        "Rejected",
        "Approved"
    };

            if (string.IsNullOrWhiteSpace(status) ||
                !allowedStatuses.Contains(status))
            {
                TempData["Error"] = "Invalid status selected.";

                return RedirectToAction(
                    nameof(RegionalHeadDetails), new { id });
            }
            if (string.IsNullOrWhiteSpace(regionalHeadRemarks))
            {
                TempData["Error"] = "Please enter Regional Head remarks.";

                return RedirectToAction(
                    nameof(RegionalHeadDetails), new { id });
            }

            if (regionalHeadRemarks.Trim().Length > 1000)
            {
                TempData["Error"] = "Remarks cannot exceed 1000 characters.";

                return RedirectToAction(
                    nameof(RegionalHeadDetails), new { id });
            }
            center.RegionalHeadApproveDate = DateTime.Now;
            center.RegionalHeadStatus = status;
            center.RegionalHeadRemarks = regionalHeadRemarks.Trim();
            center.VerticalHeadStatus = "Pending";

            await _context.SaveChangesAsync();

            TempData["Success"] = status switch
            {
                "On Hold" => "Training Center has been put On Hold.",
                "Rejected" => "Training Center has been rejected.",
                "Approved" => "Training Center has been approved by Regional Head.",
                _ => "Status updated successfully."
            };
            if (status == "On Hold")
            {
                return RedirectToAction(
                    nameof(RegionalHeadInspection),
                    new { tab = "OnHold" });
            }

            if (status == "Rejected")
            {
                return RedirectToAction(
                    nameof(RegionalHeadInspection),
                    new { tab = "Rejected" });
            }

            return RedirectToAction(
                nameof(RegionalHeadInspection),
                new { tab = "Approved" });
        }


        [HttpGet]
        public async Task<IActionResult> VerticalHeadInspection(
            string tab = "Pending")
        {
            var currentUser = await _userManager.GetUserAsync(User);

            if (currentUser == null)
                return Challenge();

            var employee = await _context.Employee
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.ApplicationUserId == currentUser.Id &&
                    x.IsActive == true);

            if (employee == null)
                return Forbid();
            var query = _context.TrainingCenter
                .AsNoTracking()
                .Where(x => x.RegionalHeadStatus == "Approved");

            switch (tab)
            {
                case "Pending":
                    query = query.Where(x =>
                        x.VerticalHeadStatus == "Pending");
                    break;

                case "OnHold":
                    query = query.Where(x =>
                        x.VerticalHeadStatus == "On Hold");
                    break;

                case "Rejected":
                    query = query.Where(x =>
                        x.VerticalHeadStatus == "Rejected");
                    break;

                case "Approved":
                    query = query.Where(x =>
                        x.VerticalHeadStatus == "Approved");
                    break;

                default:
                    tab = "Pending";
                    query = query.Where(x =>
                        x.VerticalHeadStatus == "Pending");
                    break;
            }

            var centers = await query
                .OrderByDescending(x => x.SentToRegionalHeadDate)
                .Select(x => new TrainingCenterViewModel
                {
                    Id = x.Id,
                    TrainingCenterName = x.TrainingCenterName,
                    TrainingCenterCode = x.TrainingCenterCode,
                    CompleteAddress = x.CompleteAddress,
                    InspectionDate = x.InspectionDate,
                    InspectionStatus = x.InspectionStatus,
                    PaymentStatus = x.PaymentStatus,
                    FinanceStatus = x.FinanceStatus,
                    InspectionCreatedDate = x.InspectionCreatedDate,
                    RegionalHeadStatus = x.RegionalHeadStatus,
                    VerticalHeadStatus = x.VerticalHeadStatus,

                    SchemeName = _context.Scheme
                        .Where(s => s.SchemeId == x.SchemeId)
                        .Select(s => s.SchemeName)
                        .FirstOrDefault(),

                    JobRoleTitle = _context.JobRoles
                        .Where(j => j.Id == x.JobRoleId)
                        .Select(j => j.JobRoleTitle)
                        .FirstOrDefault()
                })
                .ToListAsync();

            ViewBag.ActiveTab = tab;

            return View("RegionalHeadInspection", centers);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerticalHeadUpdateStatus(
            int id,
            string status,
            string verticalHeadRemarks)
        {
            // 1. Get logged-in user
            var currentUser = await _userManager.GetUserAsync(User);

            if (currentUser == null)
                return Challenge();

            // 2. Verify active employee
            var employee = await _context.Employee
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.ApplicationUserId == currentUser.Id &&
                    x.IsActive == true);

            if (employee == null)
                return Forbid();

            // 3. Validate status
            var allowedStatuses = new[] { "On Hold", "Rejected", "Approved" };

            if (string.IsNullOrWhiteSpace(status) ||
                !allowedStatuses.Contains(status))
            {
                TempData["Error"] = "Invalid status selected.";

                return RedirectToAction(
                    nameof(VerticalHeadInspection),
                    new { tab = "Pending" });
            }

            // 4. Validate remarks
            if (string.IsNullOrWhiteSpace(verticalHeadRemarks))
            {
                TempData["Error"] = "Please enter remarks.";

                return RedirectToAction(
                    nameof(RegionalHeadDetails),
                    new { id, tab = "Pending" });
            }

            verticalHeadRemarks = verticalHeadRemarks.Trim();

            if (verticalHeadRemarks.Length > 1000)
            {
                TempData["Error"] = "Remarks cannot exceed 1000 characters.";

                return RedirectToAction(
                    nameof(RegionalHeadDetails),
                    new { id, tab = "Pending" });
            }

            var center = await _context.TrainingCenter
                .FirstOrDefaultAsync(x =>
                    x.Id == id &&
                    x.RegionalHeadStatus == "Approved");

            if (center == null)
            {
                TempData["Error"] =
                    "Training Center not found or not approved by Regional Head.";

                return RedirectToAction(
                    nameof(VerticalHeadInspection),
                    new { tab = "Pending" });
            }
            if (center.VerticalHeadStatus != "Pending" &&
                center.VerticalHeadStatus != "On Hold")
            {
                TempData["Error"] =
                    "This Training Center has already been processed.";

                return RedirectToAction(
                    nameof(VerticalHeadInspection),
                    new { tab = "Pending" });
            }

            // 7. Update Vertical Head workflow
            center.VerticalHeadEmployeeId = employee.EmployeeId;
            center.VerticalHeadStatus = status;
            center.VerticalHeadRemarks = verticalHeadRemarks;
            center.VerticalHeadDateApproveDate = DateTime.Now;

            await _context.SaveChangesAsync();

            // 8. Success message and redirect to relevant tab
            TempData["Success"] =
                $"Training Center status updated to {status} successfully.";

            string redirectTab = status switch
            {
                "On Hold" => "OnHold",
                "Rejected" => "Rejected",
                "Approved" => "Approved",
                _ => "Pending"
            };

            return RedirectToAction(
                nameof(VerticalHeadInspection),
                new { tab = redirectTab });
        }
        [HttpGet]
        [Authorize(Roles = "BusinessHead")]
        public async Task<IActionResult> BusinessHeadInspection(
    string tab = "Pending")
        {
            var currentUser = await _userManager.GetUserAsync(User);

            if (currentUser == null)
                return Challenge();

            var employee = await _context.Employee
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.ApplicationUserId == currentUser.Id &&
                    x.IsActive == true);

            if (employee == null)
                return Forbid();

            var query = _context.TrainingCenter
                .AsNoTracking()
                .Where(x => x.VerticalHeadStatus == "Approved");

            switch (tab)
            {
                case "Pending":
                    query = query.Where(x =>
                        x.BusinessHeadStatus == "Pending");
                    break;

                case "OnHold":
                    query = query.Where(x =>
                        x.BusinessHeadStatus == "On Hold");
                    break;

                case "Rejected":
                    query = query.Where(x =>
                        x.BusinessHeadStatus == "Rejected");
                    break;

                case "Approved":
                    query = query.Where(x =>
                        x.BusinessHeadStatus == "Approved");
                    break;

                default:
                    tab = "Pending";
                    query = query.Where(x =>
                        x.BusinessHeadStatus == "Pending");
                    break;
            }

            var centers = await query
                .OrderByDescending(x => x.VerticalHeadDateApproveDate)
                .Select(x => new TrainingCenterViewModel
                {
                    Id = x.Id,
                    TrainingCenterName = x.TrainingCenterName,
                    TrainingCenterCode = x.TrainingCenterCode,
                    CompleteAddress = x.CompleteAddress,
                    InspectionDate = x.InspectionDate,
                    InspectionStatus = x.InspectionStatus,
                    PaymentStatus = x.PaymentStatus,
                    FinanceStatus = x.FinanceStatus,
                    InspectionCreatedDate = x.InspectionCreatedDate,

                    RegionalHeadStatus = x.RegionalHeadStatus,
                    VerticalHeadStatus = x.VerticalHeadStatus,
                    BusinessHeadStatus = x.BusinessHeadStatus,

                    SchemeName = _context.Scheme
                        .Where(s => s.SchemeId == x.SchemeId)
                        .Select(s => s.SchemeName)
                        .FirstOrDefault(),

                    JobRoleTitle = _context.JobRoles
                        .Where(j => j.Id == x.JobRoleId)
                        .Select(j => j.JobRoleTitle)
                        .FirstOrDefault()
                })
                .ToListAsync();

            ViewBag.ActiveTab = tab;

            return View("RegionalHeadInspection", centers);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "BusinessHead")]
        public async Task<IActionResult> BusinessHeadUpdateStatus(
            int id,
            string status,
            string businessHeadRemarks)
        {
            // 1. Get logged-in user
            var currentUser = await _userManager.GetUserAsync(User);

            if (currentUser == null)
                return Challenge();

            // 2. Find active employee
            var employee = await _context.Employee
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.ApplicationUserId == currentUser.Id &&
                    x.IsActive == true);

            if (employee == null)
                return Forbid();

            // 3. Validate status
            var allowedStatuses = new[]
            {
        "On Hold",
        "Rejected",
        "Approved"
    };

            if (string.IsNullOrWhiteSpace(status) ||
                !allowedStatuses.Contains(status))
            {
                TempData["Error"] = "Invalid status selected.";

                return RedirectToAction(
                    nameof(BusinessHeadInspection),
                    new { tab = "Pending" });
            }

            // 4. Validate remarks
            if (string.IsNullOrWhiteSpace(businessHeadRemarks))
            {
                TempData["Error"] = "Please enter remarks.";

                return RedirectToAction(
                    nameof(RegionalHeadDetails),
                    new { id, tab = "Pending" });
            }

            businessHeadRemarks = businessHeadRemarks.Trim();

            if (businessHeadRemarks.Length > 1000)
            {
                TempData["Error"] =
                    "Remarks cannot exceed 1000 characters.";

                return RedirectToAction(
                    nameof(RegionalHeadDetails),
                    new { id, tab = "Pending" });
            }

            // 5. Get center approved by Vertical Head
            var center = await _context.TrainingCenter
                .FirstOrDefaultAsync(x =>
                    x.Id == id &&
                    x.VerticalHeadStatus == "Approved");

            if (center == null)
            {
                TempData["Error"] =
                    "Training Center not found or not approved by Vertical Head.";

                return RedirectToAction(
                    nameof(BusinessHeadInspection),
                    new { tab = "Pending" });
            }

            // 6. Only Pending or On Hold records can be processed
            if (center.BusinessHeadStatus != "Pending" &&
                center.BusinessHeadStatus != "On Hold")
            {
                TempData["Error"] =
                    "This Training Center has already been processed.";

                return RedirectToAction(
                    nameof(BusinessHeadInspection),
                    new { tab = "Pending" });
            }

            // 7. Save Business Head approval details
            center.BusinessHeadEmployeeId = employee.EmployeeId;
            center.BusinessHeadStatus = status;
            center.BusinessHeadRemarks = businessHeadRemarks;
            center.BusinessHeadApproveDate = DateTime.Now;

            // 8. Save changes
            await _context.SaveChangesAsync();

            // 9. Redirect to the updated tab
            TempData["Success"] =
                $"Training Center status updated to {status} successfully.";

            string redirectTab = status switch
            {
                "On Hold" => "OnHold",
                "Rejected" => "Rejected",
                "Approved" => "Approved",
                _ => "Pending"
            };

            return RedirectToAction(
                nameof(BusinessHeadInspection),
                new { tab = redirectTab });
        }

    }
}
