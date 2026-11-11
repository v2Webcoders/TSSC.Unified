using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QUIZAPP;
using QUIZAPP.Models;
using QUIZAPP.Services;
using TSSC.Unified.Models;

namespace TSSC.Unified.Areas.HRMS.Controllers
{
    [Area("HRMS")]
    [Authorize]
    public class GrievanceController :Controller
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly AppdbContext _context;
        private readonly UtilityService _us;
        private readonly EmailService _emailService;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<GrievanceController> _logger;
        public GrievanceController(
            UtilityService us,
            UserManager<AppUser> userManager,
            SignInManager<AppUser> signInManager,
            AppdbContext context,
            EmailService emailService,
            IWebHostEnvironment environment,
            ILogger<GrievanceController> logger)
        {
            _us = us;
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
            _emailService = emailService;
            _environment = environment;
            _logger = logger;
        }
        [HttpGet]
        public async Task<IActionResult> GrievanceList()
        {
            var list = await _context.Grievance.AsNoTracking()
                .OrderByDescending(x=>x.CreatedDate).ToListAsync();
            return View(list);
        }


        //   [HttpGet]
        //   public async Task<IActionResult> GrievanceDetails(int id, string? returnUrl)
        //   {
        //       var grievance = await _context.Grievance
        //           .AsNoTracking()
        //           .FirstOrDefaultAsync(x => x.Id == id);

        //       if (grievance == null)
        //       {
        //           return NotFound();
        //       }

        //       var employees = await _context.Employee
        //.AsNoTracking()
        //.ToListAsync();

        //       ViewBag.EmployeeList = new SelectList(
        //           employees.Select(e => new
        //           {
        //               EmployeeId = e.EmployeeId,
        //               DisplayName = $"{e.FirstName} {e.LastName} ({e.EmployeeId})"
        //           }),
        //           "EmployeeId",
        //           "DisplayName"
        //       );

        //       ViewBag.ReturnUrl = returnUrl;
        //       return View(grievance);

        //   }
        [HttpGet]
        public async Task<IActionResult> GrievanceDetails(int id, string? returnUrl)
        {
            try
            {
                // 1. Get grievance details
                var grievance = await _context.Grievance
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == id && x.IsActive);

                if (grievance == null)
                {
                    return NotFound();
                }

                // 2. Load employees from database
                // Do not use string interpolation inside the EF query.
                var employees = await _context.Employee
                    .AsNoTracking()
                    .OrderBy(e => e.FirstName)
                    .ThenBy(e => e.LastName)
                    .Select(e => new
                    {
                        EmployeeId = e.EmployeeId,
                        FirstName = e.FirstName,
                        LastName = e.LastName
                    })
                    .ToListAsync();

                // 3. Generate employee display names after ToListAsync()
                var employeeList = employees.Select(e => new
                {
                    EmployeeId = e.EmployeeId,
                    DisplayName = $"{e.FirstName} {e.LastName} ({e.EmployeeId})"
                }).ToList();

                ViewBag.EmployeeList = new SelectList(
                    employeeList,
                    "EmployeeId",
                    "DisplayName",
                    grievance.AssignedTo
                );

                // 4. Get assigned employee name
                ViewBag.AssignedEmployeeName = null;

                if (grievance.AssignedTo.HasValue)
                {
                    var assignedEmployee = employees
                        .FirstOrDefault(e => e.EmployeeId == grievance.AssignedTo.Value);

                    if (assignedEmployee != null)
                    {
                        ViewBag.AssignedEmployeeName =
                            $"{assignedEmployee.FirstName} {assignedEmployee.LastName}";
                    }
                }

                // 5. Get manager name who assigned the grievance
                ViewBag.AssignedByName = null;

                if (grievance.AssignedBy.HasValue)
                {
                    var assignedByEmployee = employees
                        .FirstOrDefault(e => e.EmployeeId == grievance.AssignedBy.Value);

                    if (assignedByEmployee != null)
                    {
                        ViewBag.AssignedByName =
                            $"{assignedByEmployee.FirstName} {assignedByEmployee.LastName}";
                    }
                }
                ViewBag.ReopenedByName = "-";

                if (grievance.ReopenedBy.HasValue)
                {
                    var reopenedByEmployee = employees
                        .FirstOrDefault(e =>
                            e.EmployeeId == grievance.ReopenedBy.Value);

                    if (reopenedByEmployee != null)
                    {
                        ViewBag.ReopenedByName =
                            $"{reopenedByEmployee.FirstName} {reopenedByEmployee.LastName}";
                    }
                }
                // 6. Check whether grievance is pending manager approval
                ViewBag.IsPendingApproval =
                    grievance.Status == "Resolved" &&
                    grievance.ApprovalStatus == "Pending Approval";

                // 7. Keep return URL safe (only allow local URLs)
                ViewBag.ReturnUrl = !string.IsNullOrWhiteSpace(returnUrl) &&
                                    Url.IsLocalUrl(returnUrl)
                    ? returnUrl
                    : Url.Action("GrievanceList", "Grievance",
                        new { area = "HRMS" });

                return View(grievance);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error loading grievance details. GrievanceId: {GrievanceId}",
                    id
                );

                TempData["msg"] = "Unable to load grievance details. Please try again.";

                return RedirectToAction(
                    "GrievanceList",
                    "Grievance",
                    new { area = "HRMS" }
                );
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GrievanceDetails(Grievance model,IFormFile? ReportFile)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var employee = await _context.Employee
                .FirstOrDefaultAsync(x => x.ApplicationUserId == currentUser.Id);

            if (employee == null)
            {
                return Unauthorized();
            }

            int employeeId = employee.EmployeeId;

            var grievance = await _context.Grievance
                .FirstOrDefaultAsync(x => x.Id == model.Id);

            if (grievance == null)
            {
                return NotFound();
            }
            var oldStatus = grievance.Status;


            grievance.Status = model.Status;
            grievance.Priority = model.Priority;
            grievance.TeamRemarks = model.TeamRemarks;
            grievance.UpdatedDate = DateTime.UtcNow;


            // Manager directly resolves the grievance
            if (model.Status == "Resolved")
            {
                grievance.Status = "Closed";
                grievance.ApprovalStatus = null;

                grievance.ResolvedBy = employeeId;
                grievance.ResolvedDate = DateTime.UtcNow;

                grievance.ApprovedBy = null;
                grievance.ApprovedDate = null;
                grievance.ApprovalRemarks = null;
            }
            else
            {
                grievance.Status = model.Status;
            }

            if (ReportFile != null && ReportFile.Length > 0)
            {
                if (ReportFile.Length > 5 * 1024 * 1024)
                {
                    TempData["msg"] = "Report file size cannot exceed 5 MB.";

                    return RedirectToAction(
                        nameof(GrievanceDetails),
                        new { id = grievance.Id }
                    );
                }

                var allowedExtensions = new[]
                {
            ".pdf",
            ".doc",
            ".docx"
        };

                var extension = Path
                    .GetExtension(ReportFile.FileName)
                    .ToLowerInvariant();

                if (!allowedExtensions.Contains(extension))
                {
                    TempData["msg"] =
                        "Only PDF, DOC and DOCX files are allowed.";

                    return RedirectToAction(
                        nameof(GrievanceDetails),
                        new { id = grievance.Id }
                    );
                }

                var uploadFolder = Path.Combine(
                    _environment.WebRootPath,
                    "Uploads",
                    "Grievances",
                    "Reports"
                );

                if (!Directory.Exists(uploadFolder))
                {
                    Directory.CreateDirectory(uploadFolder);
                }

                var fileName =
                    $"{grievance.GrievanceNo}_Report_{Guid.NewGuid():N}{extension}";

                var filePath = Path.Combine(
                    uploadFolder,
                    fileName
                );

                await using (var stream = new FileStream(
                    filePath,
                    FileMode.Create))
                {
                    await ReportFile.CopyToAsync(stream);
                }

                grievance.ReportPath =
                    $"/Uploads/Grievances/Reports/{fileName}";
            }


            await _context.SaveChangesAsync();


            if (oldStatus != grievance.Status)
            {
                if (grievance.Status == "Pending" ||
                    grievance.Status == "In Progress")
                {
                    await SendGrievanceStatusEmail(grievance);
                }

                else if (grievance.Status == "Resolved")
                {
                    await SendGrievanceResolvedEmail(grievance);
                }
            }


            TempData["msg"] =
                "Grievance updated successfully.";


            return RedirectToAction(
                nameof(GrievanceDetails),
                new { id = grievance.Id }
            );
        }
        private async Task SendGrievanceStatusEmail(Grievance grievance)
        {
            string templatePath = Path.Combine(
                _environment.WebRootPath,
                "EmailTemplates",
                "GrievanceStatusUpdated.html"
            );

            var replacements = new Dictionary<string, string>
    {
        { "Name", grievance.Name },
        { "GrievanceNo", grievance.GrievanceNo },
        { "Status", grievance.Status },
        { "UpdatedDate", grievance.UpdatedDate.HasValue
            ? grievance.UpdatedDate.Value.ToString("dd-MMM-yyyy hh:mm tt")
            : DateTime.Now.ToString("dd-MMM-yyyy hh:mm tt")
        }
    };

            await _emailService.SendEmailAsync(
                grievance.Email,
                $"Grievance Status Updated - {grievance.GrievanceNo}",
                templatePath,
                replacements
            );
        }
        private async Task SendGrievanceResolvedEmail(Grievance grievance)
        {
            string templatePath = Path.Combine(
                _environment.WebRootPath,
                "EmailTemplates",
                "GrievanceResolved.html"
            );


            string reportUrl = string.Empty;

            if (!string.IsNullOrEmpty(grievance.ReportPath))
            {
                reportUrl =
                    $"{Request.Scheme}://{Request.Host}{grievance.ReportPath}";
            }


            var replacements = new Dictionary<string, string>
    {
        { "Name", grievance.Name },

        { "GrievanceNo", grievance.GrievanceNo },

        { "Status", grievance.Status },

        { "ResolvedDate", grievance.ResolvedDate.HasValue
            ? grievance.ResolvedDate.Value.ToString("dd-MMM-yyyy hh:mm tt")
            : DateTime.Now.ToString("dd-MMM-yyyy hh:mm tt")
        },

        { "ResolvedBy", grievance.ResolvedBy?.ToString() ?? "TSSC Grievance Team" },

        { "TeamRemarks", string.IsNullOrWhiteSpace(grievance.TeamRemarks)
            ? "No remarks provided."
            : grievance.TeamRemarks
        },

        { "ReportUrl", reportUrl }
    };


            await _emailService.SendEmailAsync(
                grievance.Email,
                $"Grievance Resolved - {grievance.GrievanceNo}",
                templatePath,
                replacements
            );
        }

        [HttpGet]
        public async Task<IActionResult> PendingGrievance()
        {
            var list = await _context.Grievance
                .AsNoTracking()
                .Where(x => x.Status == "Submitted")
                .OrderByDescending(x => x.CreatedDate)
                .ToListAsync();

            return View("GrievanceList", list);
        }
        [HttpGet]
        public async Task<IActionResult> InProgressGrievance()
        {
            var list = await _context.Grievance
                .AsNoTracking()
                .Where(x => x.Status == "In Progress")
                .OrderByDescending(x => x.CreatedDate)
                .ToListAsync();

            return View("GrievanceList", list);
        }


        //[HttpGet]
        //public async Task<IActionResult> ResolvedGrievance()
        //{
        //    var list = await _context.Grievance
        //        .AsNoTracking()
        //        .Where(x => x.Status == "Resolved")
        //        .OrderByDescending(x => x.CreatedDate)
        //        .ToListAsync();

        //    return View("GrievanceList", list);
        //}
        [HttpGet]
        public async Task<IActionResult> PendingApprovalGrievance()
        {
            var grievances = await _context.Grievance
                .AsNoTracking()
                .Where(x =>
                    x.IsActive &&
                    x.Status == "Resolved" &&
                    x.ApprovalStatus == "Pending Approval" &&
                    x.ResolvedBy.HasValue &&
                    x.AssignedTo.HasValue)
                .OrderByDescending(x => x.ResolvedDate)
                .ToListAsync();

            return View("GrievanceList", grievances);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignEmployee(
            int GrievanceId,
            int EmployeeId,
            string AssignedEmployeeRemarks)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);

                if (currentUser == null)
                    return Unauthorized();

                var manager = await _context.Employee
                    .FirstOrDefaultAsync(x =>
                        x.ApplicationUserId == currentUser.Id);

                if (manager == null)
                    return Unauthorized();

                var grievance = await _context.Grievance
                    .FirstOrDefaultAsync(x =>
                        x.Id == GrievanceId && x.IsActive);

                if (grievance == null)
                    return NotFound();

                // Validate employee selection
                if (EmployeeId <= 0)
                {
                    TempData["msg"] = "Please select an employee.";
                    return RedirectToAction(
                        nameof(GrievanceDetails),
                        new { id = GrievanceId });
                }

                var assignedEmployee = await _context.Employee
                    .FirstOrDefaultAsync(x =>
                        x.EmployeeId == EmployeeId);

                if (assignedEmployee == null)
                {
                    TempData["msg"] = "Selected employee not found.";
                    return RedirectToAction(
                        nameof(GrievanceDetails),
                        new { id = GrievanceId });
                }

                // Only new submitted grievances can be assigned
                if (grievance.Status != "Submitted" ||
                    grievance.AssignedTo.HasValue)
                {
                    TempData["msg"] =
                        "This grievance cannot be assigned in its current status.";

                    return RedirectToAction(
                        nameof(GrievanceDetails),
                        new { id = GrievanceId });
                }

                // Validate assignment remarks
                if (string.IsNullOrWhiteSpace(AssignedEmployeeRemarks))
                {
                    TempData["msg"] = "Assignment remarks are required.";
                    return RedirectToAction(
                        nameof(GrievanceDetails),
                        new { id = GrievanceId });
                }

                if (AssignedEmployeeRemarks.Trim().Length > 5000)
                {
                    TempData["msg"] = "Remarks cannot exceed 5000 characters.";
                    return RedirectToAction(
                        nameof(GrievanceDetails),
                        new { id = GrievanceId });
                }

                // Assign grievance
                grievance.AssignedTo = assignedEmployee.EmployeeId;
                grievance.AssignedBy = manager.EmployeeId;
                grievance.AssignedDate = DateTime.UtcNow;
                grievance.AssignedEmployeeRemarks =
                    AssignedEmployeeRemarks.Trim();

                grievance.Status = "Assigned";
                grievance.ApprovalStatus = null;
                grievance.UpdatedDate = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                TempData["msg"] =
                    "Grievance assigned successfully to " +
                    assignedEmployee.FirstName + " " +
                    assignedEmployee.LastName + ".";

                return RedirectToAction(
                    nameof(GrievanceDetails),
                    new { id = GrievanceId });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error assigning grievance. GrievanceId: {GrievanceId}",
                    GrievanceId);

                TempData["msg"] =
                    "Something went wrong while assigning grievance.";

                return RedirectToAction(
                    nameof(GrievanceDetails),
                    new { id = GrievanceId });
            }
        }

        [HttpGet]
        public async Task<IActionResult> MyAssignedGrievances()
        {
            var currentUser = await _userManager.GetUserAsync(User);

            if (currentUser == null)
                return Unauthorized();

            var employee = await _context.Employee
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.ApplicationUserId == currentUser.Id);

            if (employee == null)
                return Unauthorized();

            var list = await _context.Grievance
                .AsNoTracking()
                .Where(x =>
                    x.IsActive &&
                    x.AssignedTo == employee.EmployeeId &&
                    (
                        x.Status == "Assigned" ||
                        x.Status == "In Progress" ||
                        x.Status == "Reopened"
                    ))
                .OrderByDescending(x => x.UpdatedDate)
                .ToListAsync();

            return View(list);
        }
        [HttpGet]
        public async Task<IActionResult> MyInProgressGrievances()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
                return Unauthorized();

            var employee = await _context.Employee
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.ApplicationUserId == currentUser.Id);

            if (employee == null)
                return Unauthorized();

            var list = await _context.Grievance
                .AsNoTracking()
                .Where(x =>
                    x.AssignedTo == employee.EmployeeId &&
                    x.Status == "In Progress")
                .OrderByDescending(x => x.UpdatedDate)
                .ToListAsync();

            return View("MyAssignedGrievances", list);
        }
        [HttpGet]
        public async Task<IActionResult> MyResolvedGrievances()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
                return Unauthorized();

            var employee = await _context.Employee
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.ApplicationUserId == currentUser.Id);

            if (employee == null)
                return Unauthorized();

            var list = await _context.Grievance
                .AsNoTracking()
                .Where(x =>
                    x.AssignedTo == employee.EmployeeId &&
                    x.Status == "Resolved" &&
                    x.ApprovalStatus == "Pending Approval")
                .OrderByDescending(x => x.ResolvedDate)
                .ToListAsync();

            return View("MyAssignedGrievances", list);
        }

        [HttpGet]
        public async Task<IActionResult> EmployeeGrievanceDetails(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            if (currentUser == null)
                return Unauthorized();

            var employee = await _context.Employee
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.ApplicationUserId == currentUser.Id);

            if (employee == null)
                return Unauthorized();

            var grievance = await _context.Grievance
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.Id == id &&
                    x.AssignedTo == employee.EmployeeId &&
                    x.IsActive);

            if (grievance == null)
                return NotFound();

            var assignedByEmployee = await _context.Employee
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.EmployeeId == grievance.AssignedBy);

            ViewBag.AssignedByName = assignedByEmployee != null
                ? $"{assignedByEmployee.FirstName} {assignedByEmployee.LastName} ({assignedByEmployee.EmployeeId})"
                : "-";

            ViewBag.AssignedEmployeeName =
                $"{employee.FirstName} {employee.LastName} ({employee.EmployeeId})";

            return View(grievance);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitEmployeeResolution(
            int Id,
            string Status,
            string TeamRemarks,
            IFormFile? ReportFile)
        {
            string? filePath = null;

            try
            {
                var currentUser = await _userManager.GetUserAsync(User);

                if (currentUser == null)
                    return Unauthorized();

                var employee = await _context.Employee
                    .FirstOrDefaultAsync(x => x.ApplicationUserId == currentUser.Id);

                if (employee == null)
                    return Unauthorized();

                var grievance = await _context.Grievance
                    .FirstOrDefaultAsync(x =>
                        x.Id == Id &&
                        x.AssignedTo == employee.EmployeeId &&
                        x.IsActive);

                if (grievance == null)
                    return NotFound();

                // Do not allow updates after approval or while awaiting approval.
                if (grievance.Status == "Closed" ||
                    grievance.ApprovalStatus == "Pending Approval" ||
                    grievance.ApprovalStatus == "Approved")
                {
                    TempData["msg"] = "This grievance cannot be updated.";

                    return RedirectToAction(
                        nameof(EmployeeGrievanceDetails),
                        new { id = Id });
                }

                // Reopened grievances can be worked on again.
                bool validCurrentStatus =
                    grievance.Status == "Assigned" ||
                    grievance.Status == "In Progress" ||
                    grievance.Status == "Reopened";

                if (!validCurrentStatus)
                {
                    TempData["msg"] =
                        "This grievance cannot be updated in its current status.";

                    return RedirectToAction(
                        nameof(EmployeeGrievanceDetails),
                        new { id = Id });
                }

                // Employee can select Assigned only while it is still Assigned.
                if (Status != "Assigned" &&
                    Status != "In Progress" &&
                    Status != "Resolved")
                {
                    TempData["msg"] = "Invalid grievance status.";

                    return RedirectToAction(
                        nameof(EmployeeGrievanceDetails),
                        new { id = Id });
                }

                if (Status == "Assigned" && grievance.Status != "Assigned")
                {
                    TempData["msg"] = "You cannot move the grievance back to Assigned.";

                    return RedirectToAction(
                        nameof(EmployeeGrievanceDetails),
                        new { id = Id });
                }

                if (string.IsNullOrWhiteSpace(TeamRemarks))
                {
                    TempData["msg"] = "Please enter work remarks.";

                    return RedirectToAction(
                        nameof(EmployeeGrievanceDetails),
                        new { id = Id });
                }

                if (TeamRemarks.Trim().Length > 5000)
                {
                    TempData["msg"] = "Remarks cannot exceed 5000 characters.";

                    return RedirectToAction(
                        nameof(EmployeeGrievanceDetails),
                        new { id = Id });
                }

                // Resolved requires a report.
                if (Status == "Resolved" &&
                    (ReportFile == null || ReportFile.Length == 0))
                {
                    TempData["msg"] = "Please upload the resolution report.";

                    return RedirectToAction(
                        nameof(EmployeeGrievanceDetails),
                        new { id = Id });
                }

                // Validate report only when a file is supplied.
                // Therefore, In Progress can be saved without a report.
                if (ReportFile != null && ReportFile.Length > 0)
                {
                    if (ReportFile.Length > 5 * 1024 * 1024)
                    {
                        TempData["msg"] = "File size cannot exceed 5 MB.";

                        return RedirectToAction(
                            nameof(EmployeeGrievanceDetails),
                            new { id = Id });
                    }

                    var allowedExtensions = new[]
                    {
                ".pdf", ".doc", ".docx", ".jpg", ".jpeg", ".png"
            };

                    var extension = Path
                        .GetExtension(ReportFile.FileName)
                        .ToLowerInvariant();

                    if (!allowedExtensions.Contains(extension))
                    {
                        TempData["msg"] =
                            "Only PDF, DOC, DOCX, JPG, JPEG and PNG files are allowed.";

                        return RedirectToAction(
                            nameof(EmployeeGrievanceDetails),
                            new { id = Id });
                    }

                    var uploadFolder = Path.Combine(
                        _environment.WebRootPath,
                        "Uploads",
                        "Grievances",
                        "Reports");

                    Directory.CreateDirectory(uploadFolder);

                    var fileName = $"{Guid.NewGuid():N}{extension}";

                    filePath = Path.Combine(uploadFolder, fileName);

                    await using (var stream = new FileStream(
                        filePath,
                        FileMode.CreateNew,
                        FileAccess.Write,
                        FileShare.None))
                    {
                        await ReportFile.CopyToAsync(stream);
                    }

                    // Same ReportPath column for In Progress and Resolved.
                    grievance.ReportPath =
                        "/Uploads/Grievances/Reports/" + fileName;
                }

                grievance.TeamRemarks = TeamRemarks.Trim();
                grievance.UpdatedDate = DateTime.UtcNow;

                // Employee starts or resumes work.
                if (Status == "Assigned" || Status == "In Progress")
                {
                    grievance.Status = Status;

                    // Clear previous rejection state for the new work cycle.
                    // Keep RejectedBy, RejectedDate and RejectionRemarks as history.
                    if (grievance.Status == "In Progress")
                    {
                        grievance.ApprovalStatus = null;
                        grievance.ResolvedBy = null;
                        grievance.ResolvedDate = null;
                    }

                    await _context.SaveChangesAsync();

                    if (Status == "In Progress")
                    {
                        try
                        {
                            await SendGrievanceStatusEmail(grievance);
                        }
                        catch (Exception emailEx)
                        {
                            _logger.LogError(
                                emailEx,
                                "In Progress email failed. GrievanceId: {Id}",
                                Id);
                        }
                    }

                    TempData["msg"] = Status == "In Progress"
                        ? "Grievance updated to In Progress successfully."
                        : "Grievance status updated successfully.";

                    return RedirectToAction(
                        nameof(EmployeeGrievanceDetails),
                        new { id = Id });
                }

                // Employee submits final resolution for manager approval.
                if (Status == "Resolved")
                {
                    grievance.Status = "Resolved";
                    grievance.ApprovalStatus = "Pending Approval";

                    grievance.ResolvedBy = employee.EmployeeId;
                    grievance.ResolvedDate = DateTime.UtcNow;

                    await _context.SaveChangesAsync();

                    //try
                    //{
                    //    await SendGrievanceResolvedEmail(grievance);
                    //}
                    //catch (Exception emailEx)
                    //{
                    //    _logger.LogError(
                    //        emailEx,
                    //        "Resolution email failed. GrievanceId: {Id}",
                    //        Id);
                    //}

                    TempData["msg"] =
                        "Resolution submitted successfully for manager approval.";

                    return RedirectToAction(
                        nameof(EmployeeGrievanceDetails),
                        new { id = Id });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error submitting grievance update. GrievanceId: {Id}",
                    Id);

                // Delete newly uploaded file if the database update failed.
                if (!string.IsNullOrEmpty(filePath) &&
                    System.IO.File.Exists(filePath))
                {
                    try
                    {
                        System.IO.File.Delete(filePath);
                    }
                    catch (Exception deleteEx)
                    {
                        _logger.LogError(
                            deleteEx,
                            "Unable to delete uploaded report file: {FilePath}",
                            filePath);
                    }
                }

                TempData["msg"] =
                    "Unable to update grievance. Please try again.";
            }

            return RedirectToAction(
                nameof(EmployeeGrievanceDetails),
                new { id = Id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReviewGrievance(
    int Id,
    string ApprovalRemarks,
    string decision)
        {
            try
            {
                // 1. Get logged-in manager
                var currentUser = await _userManager.GetUserAsync(User);

                if (currentUser == null)
                    return Unauthorized();

                // 2. Get manager employee record
                var manager = await _context.Employee
                    .FirstOrDefaultAsync(x =>
                        x.ApplicationUserId == currentUser.Id);

                if (manager == null)
                    return Unauthorized();

                // 3. Get grievance
                var grievance = await _context.Grievance
                    .FirstOrDefaultAsync(x => x.Id == Id);

                if (grievance == null)
                    return NotFound();

                // 4. Check grievance approval status
                if (grievance.Status != "Resolved" ||
                    grievance.ApprovalStatus != "Pending Approval")
                {
                    TempData["msg"] =
                        "This grievance is not pending for approval.";

                    return RedirectToAction(
                        nameof(GrievanceDetails),
                        new { id = Id });
                }

                // 5. Validate remarks
                if (string.IsNullOrWhiteSpace(ApprovalRemarks))
                {
                    TempData["msg"] = "Please enter manager remarks.";

                    return RedirectToAction(
                        nameof(GrievanceDetails),
                        new { id = Id });
                }

                if (ApprovalRemarks.Trim().Length > 5000)
                {
                    TempData["msg"] = "Remarks cannot exceed 5000 characters.";

                    return RedirectToAction(
                        nameof(GrievanceDetails),
                        new { id = Id });
                }

                // 6. Approve or Reject
                if (decision == "Approve")
                {
                    grievance.ApprovalStatus = "Approved";

                    grievance.ApprovedBy = manager.EmployeeId;
                    grievance.ApprovedDate = DateTime.UtcNow;
                    grievance.ApprovalRemarks = ApprovalRemarks.Trim();

                    grievance.Status = "Closed";
                    grievance.UpdatedDate = DateTime.UtcNow;

                    await _context.SaveChangesAsync();

                    try
                    {
                        await SendGrievanceResolvedEmail(grievance);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex,
                            "Grievance approval email failed: {GrievanceNo}",
                            grievance.GrievanceNo);
                    }
                }
                else if (decision == "Reject")
                {
                    grievance.ApprovalStatus = "Rejected";

                    grievance.RejectedBy = manager.EmployeeId;
                    grievance.RejectedDate = DateTime.UtcNow;
                    grievance.RejectionRemarks = ApprovalRemarks.Trim();

                   
                }
                else
                {
                    return BadRequest("Invalid decision.");
                }

                grievance.UpdatedDate = DateTime.UtcNow;

                // 7. Save changes
                await _context.SaveChangesAsync();

                TempData["msg"] = decision == "Approve"
                    ? "Grievance approved successfully and closed."
                    : "Resolution rejected successfully. Grievance reopened.";

                return RedirectToAction(
                    nameof(GrievanceDetails),
                    new { id = Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error reviewing grievance. GrievanceId: {Id}",
                    Id);

                TempData["msg"] =
                    "Something went wrong while reviewing the grievance.";

                return RedirectToAction(
                    nameof(GrievanceDetails),
                    new { id = Id });
            }
        }
        public async Task<IActionResult> AssignedGrievances()
        {
            var data = await _context.Grievance
                .Where(x => x.IsActive &&
                            x.AssignedTo != null
                           /*&& x.Status == "Assigned"*/)
                .OrderByDescending(x => x.AssignedDate)
                .ToListAsync();

            return View("GrievanceList", data);
        }

        public async Task<IActionResult> RejectedGrievances()
        {
            var data = await _context.Grievance
                .Where(x => x.IsActive &&
                            x.Status == "Resolved" &&
                            x.ApprovalStatus == "Rejected")
                .OrderByDescending(x => x.RejectedDate)
                .ToListAsync();

            return View("GrievanceList", data);
        }
        public async Task<IActionResult> ReopenedGrievances()
        {
            var data = await _context.Grievance
                .Where(x => x.IsActive && x.Status == "Reopened")
                .OrderByDescending(x => x.ReopenedDate)
                .ToListAsync();

            return View("GrievanceList", data);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReopenGrievance(int id, string? returnUrl)
        {
            var grievance = await _context.Grievance
                .FirstOrDefaultAsync(x => x.Id == id && x.IsActive);

            if (grievance == null)
                return NotFound();

            // Only rejected grievances can be reopened
            if (grievance.Status != "Resolved" ||
                grievance.ApprovalStatus != "Rejected")
            {
                TempData["Error"] = "Only rejected grievances can be reopened.";

                return RedirectToAction(
                    "GrievanceDetails",
                    new { id, returnUrl });
            }

            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return Challenge();

            var manager = await _context.Employee
                .FirstOrDefaultAsync(x => x.OfficialEmail == user.Email);

            if (manager == null)
            {
                TempData["Error"] = "Manager employee record not found.";
                return Forbid();
            }

            // Reopen grievance
            grievance.Status = "Reopened";
            grievance.ReopenedBy = manager.EmployeeId;
            grievance.ReopenedDate = DateTime.UtcNow;
            grievance.ReopenRemarks = grievance.RejectionRemarks;
            grievance.UpdatedDate = DateTime.UtcNow;

            // Do not change AssignedTo.
            // Grievance will return to the same assigned employee.

            await _context.SaveChangesAsync();

            TempData["Success"] = "Grievance reopened successfully.";

            if (!string.IsNullOrWhiteSpace(returnUrl) &&
                Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction("GrievanceList");
        }
        public async Task<IActionResult> ClosedGrievances()
        {
            var data = await _context.Grievance
                .Where(x => x.IsActive && x.Status == "Closed")
                .OrderByDescending(x => x.UpdatedDate)
                .ToListAsync();

            return View("GrievanceList", data);
        }
    }
}
