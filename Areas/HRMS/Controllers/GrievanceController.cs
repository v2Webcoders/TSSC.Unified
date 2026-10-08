using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
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
        

        [HttpGet]
        public async Task<IActionResult> GrievanceDetails(int id, string? returnUrl)
        {
            var grievance = await _context.Grievance
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);

            if (grievance == null)
            {
                return NotFound();
            }
            ViewBag.ReturnUrl = returnUrl;
            return View(grievance);

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


            if (model.Status == "Resolved")
            {
                grievance.ResolvedBy = employeeId;
                grievance.ResolvedDate = DateTime.UtcNow;
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


        [HttpGet]
        public async Task<IActionResult> ResolvedGrievance()
        {
            var list = await _context.Grievance
                .AsNoTracking()
                .Where(x => x.Status == "Resolved")
                .OrderByDescending(x => x.CreatedDate)
                .ToListAsync();

            return View("GrievanceList", list);
        }
    }
}
