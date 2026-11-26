using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QUIZAPP;
using QUIZAPP.Models;
using TSSC.Unified.Models;
using TSSC.Unified.ViewModel;

namespace TSSC.Unified.Areas.HRMS.Controllers
{
    [Area("HRMS")]
    public class ProjectManagementController : Controller
    {
        private readonly AppdbContext _context;
        private readonly IWebHostEnvironment _enviroment;
        private readonly UserManager<AppUser> _userManager;
        private readonly ILogger<ProjectManagementController> _logger;

        public ProjectManagementController(AppdbContext context,IWebHostEnvironment enviroment,UserManager<AppUser>userManager, ILogger<ProjectManagementController> logger)
        {
            _context=context;
            _enviroment=enviroment;
            _userManager= userManager;
            _logger = logger;
        }
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var currentUser = await _userManager.GetUserAsync(User);

            if (currentUser == null)
                return Challenge();

            var employee = await _context.Employee
                .FirstOrDefaultAsync(
                    x => x.ApplicationUserId == currentUser.Id);

            if (employee == null)
                return NotFound("Employee record not found.");

            // Fetch projects assigned to logged-in Business Head
            var projects = await _context.AdminProjects
                .AsNoTracking()
                .Where(x => x.BusinessHeadId == employee.EmployeeId)
                .OrderByDescending(x => x.CreatedDate)
                .ToListAsync();

            return View(projects);
        }
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            ViewBag.Vendors = await _context.ProjectVendors
                .Where(x => x.IsActive)
                .Select(x => new SelectListItem
                {
                    Value = x.VendorId.ToString(),
                    Text = x.VendorName
                })
                .ToListAsync();

            ViewBag.Employees = await _context.Employee
                .Select(x => new SelectListItem
                {
                    Value = x.EmployeeId.ToString(),
                    Text = x.FirstName + " " + x.LastName + " (" + x.EmployeeId + ")"
                })
                .ToListAsync();
            ViewBag.ProjectMilestones = await _context.ProjectMilestones
                .Where(x => x.ProjectId == id)
                .Include(x => x.Vendor)
                .ToListAsync();

            ViewBag.ProjectTeams = await _context.ProjectTeams
                .Where(x => x.ProjectId == id)
                .Include(x => x.Employee)
                .ToListAsync();

            ViewBag.ProjectBudgets = await _context.ProjectDepartmentBudgets
                .Where(x => x.ProjectId == id)
                .ToListAsync();
            var project = await _context.AdminProjects
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.ProjectId == id);

            if (project == null)
                return NotFound();

            return View(project);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReviewProject(int projectId,string decision,string remarks)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            if (currentUser == null)
                return Challenge();

            var employee = await _context.Employee
                .FirstOrDefaultAsync(x => x.ApplicationUserId == currentUser.Id);

            if (employee == null)
                return NotFound("Employee record not found.");

            var project = await _context.AdminProjects
                .FirstOrDefaultAsync(x =>
                    x.ProjectId == projectId &&
                    x.BusinessHeadId == employee.EmployeeId);

            if (project == null)
                return NotFound("Project not found.");

            if (!string.IsNullOrEmpty(project.BusinessHeadStatus))
            {
                TempData["Error"] = "Project review has already been submitted.";
                return RedirectToAction("Details", new { id = projectId });
            }

            if (string.IsNullOrWhiteSpace(decision))
            {
                TempData["Error"] = "Please select a review decision.";
                return RedirectToAction("Details", new { id = projectId });
            }

            switch (decision)
            {
                case "Ok":
                    project.BusinessHeadStatus = "Approved";
                    break;

                case "Reject":
                    project.BusinessHeadStatus = "Rejected";
                    break;

                case "On Hold":
                    project.BusinessHeadStatus = "On Hold";
                    break;

                default:
                    TempData["Error"] = "Invalid review decision.";
                    return RedirectToAction("Details", new { id = projectId });
            }

            project.BusinessHeadRemarks = remarks;
            project.BusinessHeadReviewedBy = employee.EmployeeId;
            project.BusinessHeadReviewedDate = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Project review submitted successfully.";

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveProjectPlanning(ProjectPlanningViewModel model)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            if (currentUser == null)
                return Challenge();

            var employee = await _context.Employee
                .FirstOrDefaultAsync(x => x.ApplicationUserId == currentUser.Id);

            if (employee == null)
                return NotFound("Employee record not found.");

            // 1. Check project belongs to current Business Head
            var project = await _context.AdminProjects
                .FirstOrDefaultAsync(x =>
                    x.ProjectId == model.ProjectId &&
                    x.BusinessHeadId == employee.EmployeeId);

            if (project == null)
                return NotFound("Project not found.");

            // 2. Planning allowed only after Business Head approval
            if (project.BusinessHeadStatus != "Approved")
            {
                TempData["Error"] =
                    "Please approve the project before submitting its planning.";

                return RedirectToAction("Details", new { id = model.ProjectId });
            }

            // 3. Prevent duplicate planning submission
            if (!string.IsNullOrEmpty(project.CEOStatus))
            {
                TempData["Error"] =
                    "Project planning has already been submitted to CEO.";

                return RedirectToAction("Details", new { id = model.ProjectId });
            }

            // 4. Check all three sections
            if (model.Milestones == null || !model.Milestones.Any() ||
                model.Employees == null || !model.Employees.Any() ||
                model.Budgets == null || !model.Budgets.Any())
            {
                TempData["Error"] =
                    "Please add at least one milestone, employee and department budget.";

                return RedirectToAction("Details", new { id = model.ProjectId });
            }

            // 5. Validate milestone details
            foreach (var milestone in model.Milestones)
            {
                if (string.IsNullOrWhiteSpace(milestone.MilestoneName) ||
                    !milestone.StartDate.HasValue ||
                    !milestone.EndDate.HasValue ||
                    milestone.StartDate.Value.Date > milestone.EndDate.Value.Date)
                {
                    TempData["Error"] =
                        "Please enter valid milestone details and dates.";

                    return RedirectToAction("Details", new { id = model.ProjectId });
                }
            }

            // 6. Validate employee selection
            if (model.Employees.Any(x =>
                !x.EmployeeId.HasValue || x.EmployeeId.Value <= 0))
            {
                TempData["Error"] = "Please select all assigned employees.";

                return RedirectToAction("Details", new { id = model.ProjectId });
            }

            var employeeIds = model.Employees
                .Select(x => x.EmployeeId!.Value)
                .ToList();

            // Prevent duplicate employees
            if (employeeIds.Distinct().Count() != employeeIds.Count)
            {
                TempData["Error"] =
                    "Same employee cannot be assigned more than once.";

                return RedirectToAction("Details", new { id = model.ProjectId });
            }

            // Validate employee records without Contains()
            foreach (var employeeId in employeeIds)
            {
                var employeeExists = await _context.Employee
                    .AnyAsync(x => x.EmployeeId == employeeId);

                if (!employeeExists)
                {
                    TempData["Error"] =
                        "One or more selected employees are invalid.";

                    return RedirectToAction("Details", new { id = model.ProjectId });
                }
            }

            // 7. Validate budget details
            if (model.Budgets.Any(x =>
                string.IsNullOrWhiteSpace(x.Department) ||
                x.BudgetAmount <= 0))
            {
                TempData["Error"] =
                    "Please enter valid department budget details.";

                return RedirectToAction("Details", new { id = model.ProjectId });
            }

            // 8. Validate vendor IDs without Contains()
            foreach (var milestone in model.Milestones)
            {
                if (milestone.VendorId.HasValue)
                {
                    var vendorExists = await _context.ProjectVendors
                        .AnyAsync(x =>
                            x.VendorId == milestone.VendorId.Value &&
                            x.IsActive);

                    if (!vendorExists)
                    {
                        TempData["Error"] =
                            "Invalid or inactive vendor selected.";

                        return RedirectToAction("Details", new { id = model.ProjectId });
                    }
                }
            }

            // 9. Save all records in one transaction
            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            try
            {
                // Save Milestones
                foreach (var item in model.Milestones)
                {
                    var milestone = new ProjectMilestone
                    {
                        ProjectId = project.ProjectId,
                        MilestoneName = item.MilestoneName.Trim(),
                        StartDate = item.StartDate.Value,
                        EndDate = item.EndDate.Value,
                        VendorId = item.VendorId,
                        CreatedDate = DateTime.Now
                    };

                    _context.ProjectMilestones.Add(milestone);
                }

                // Save Assigned Employees
                foreach (var item in model.Employees)
                {
                    var projectTeam = new ProjectTeam
                    {
                        ProjectId = project.ProjectId,
                        EmployeeId = item.EmployeeId.Value,
                        CreatedDate = DateTime.Now
                    };

                    _context.ProjectTeams.Add(projectTeam);
                }

                // Save Department Budgets
                foreach (var item in model.Budgets)
                {
                    var budget = new ProjectDepartmentBudget
                    {
                        ProjectId = project.ProjectId,
                        Department = item.Department.Trim(),
                        BudgetAmount = item.BudgetAmount,
                        AllocationPurpose = item.AllocationPurpose,
                        Remarks = item.Remarks,
                        CreatedDate = DateTime.Now
                    };

                    _context.ProjectDepartmentBudgets.Add(budget);
                }

                // Update CEO submission details
                project.CEOStatus = "Pending";
                project.SubmittedToCEODate = DateTime.Now;

                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                TempData["Success"] =
                    "Project planning successfully submitted to CEO.";

                return RedirectToAction("Details", new { id = project.ProjectId });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();

                // Log actual error
                Console.WriteLine("Project Planning Error: " + ex.ToString());

                TempData["Error"] =
                    "Unable to submit project planning. Please try again.";

                return RedirectToAction("Details", new { id = model.ProjectId });
            }
        }

        [HttpGet]
        public async Task<IActionResult> CEOIndex(string? status = "All")
        {
            var query = _context.AdminProjects
                .Where(x => x.IsActive &&
                            x.BusinessHeadStatus == "Approved" &&
                            x.CEOStatus != null &&
                            x.CEOStatus != "");

            // Status-wise filtering
            if (status != "All")
            {
                query = query.Where(x => x.CEOStatus == status);
            }

            var projects = await query
                .OrderByDescending(x => x.SubmittedToCEODate)
                .ToListAsync();

            // Tab counts
            var allProjects = _context.AdminProjects
                .Where(x => x.IsActive &&
                            x.BusinessHeadStatus == "Approved" &&
                            x.CEOStatus != null &&
                            x.CEOStatus != "");

            ViewBag.AllCount = await allProjects.CountAsync();
            ViewBag.PendingCount = await allProjects.CountAsync(x => x.CEOStatus == "Pending");
            ViewBag.ApprovedCount = await allProjects.CountAsync(x => x.CEOStatus == "Approved");
            ViewBag.OnHoldCount = await allProjects.CountAsync(x => x.CEOStatus == "On Hold");
            ViewBag.RejectedCount = await allProjects.CountAsync(x => x.CEOStatus == "Rejected");

            ViewBag.SelectedStatus = status;

            return View(projects);
        }



        [HttpGet]
        public async Task<IActionResult> CEODetails(int id, string? status = "All")
        {
            var project = await _context.AdminProjects
                .FirstOrDefaultAsync(x =>
                    x.ProjectId == id &&
                    x.IsActive &&
                    x.BusinessHeadStatus == "Approved" &&
                    x.CEOStatus != null &&
                    x.CEOStatus != "");

            if (project == null)
            {
                TempData["Error"] = "Project not found.";
                return RedirectToAction("CEOIndex", new { status });
            }

            ViewBag.ReturnStatus = status;

            ViewBag.ProjectMilestones = await _context.ProjectMilestones
                .Where(x => x.ProjectId == id)
                .Include(x => x.Vendor)
                .ToListAsync();

            ViewBag.ProjectTeams = await _context.ProjectTeams
                .Where(x => x.ProjectId == id)
                .Include(x => x.Employee)
                .ToListAsync();

            ViewBag.ProjectBudgets = await _context.ProjectDepartmentBudgets
                .Where(x => x.ProjectId == id)
                .ToListAsync();

            return View(project);
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CEOReviewProject(
            int projectId,
            string decision,
            string? remarks,
            string? returnStatus)
        {
            // Allow only valid return statuses
            var validStatuses = new[] { "All", "Pending", "Approved", "Rejected", "On Hold" };

            if (string.IsNullOrWhiteSpace(returnStatus) ||
                !validStatuses.Contains(returnStatus))
            {
                returnStatus = "All";
            }

            try
            {
                var currentUser = await _userManager.GetUserAsync(User);

                if (currentUser == null)
                {
                    return RedirectToAction("Login", "Account");
                }

                var employee = await _context.Employee
                    .FirstOrDefaultAsync(x =>
                        x.ApplicationUserId == currentUser.Id);

                if (employee == null)
                {
                    TempData["Error"] = "Employee record not found.";
                    return RedirectToAction("CEOIndex", new { status = returnStatus });
                }

                // Validate decision
                if (decision != "Approved" &&
                    decision != "Rejected" &&
                    decision != "On Hold")
                {
                    TempData["Error"] = "Invalid review decision.";
                    return RedirectToAction("CEODetails",
                        new { id = projectId, status = returnStatus });
                }

                // Validate remarks
                if (string.IsNullOrWhiteSpace(remarks))
                {
                    TempData["Error"] = "Please enter remarks.";
                    return RedirectToAction("CEODetails",
                        new { id = projectId, status = returnStatus });
                }

                remarks = remarks.Trim();

                if (remarks.Length > 4000)
                {
                    TempData["Error"] = "Remarks cannot exceed 4000 characters.";
                    return RedirectToAction("CEODetails",
                        new { id = projectId, status = returnStatus });
                }

                // Fetch pending project
                var project = await _context.AdminProjects
                    .FirstOrDefaultAsync(x =>
                        x.ProjectId == projectId &&
                        x.IsActive &&
                        x.BusinessHeadStatus == "Approved" &&
                        x.CEOStatus == "Pending");

                if (project == null)
                {
                    TempData["Error"] = "Project not found or already reviewed.";
                    return RedirectToAction("CEOIndex",
                        new { status = returnStatus });
                }

                // Save review details
                project.CEOReviewedBy = employee.EmployeeId;
                project.CEOReviewedDate = DateTime.Now;
                project.CEORemarks = remarks;
                project.CEOStatus = decision;

                await _context.SaveChangesAsync();

                TempData["Success"] = decision switch
                {
                    "Approved" => "Project approved successfully.",
                    "Rejected" => "Project rejected successfully.",
                    "On Hold" => "Project put on hold successfully.",
                    _ => "Project review submitted successfully."
                };

                return RedirectToAction("CEOIndex",
                    new { status = returnStatus });
            }
            catch (Exception ex)
            {
                // Replace with ILogger logging
                TempData["Error"] = "An error occurred while reviewing the project.";

                return RedirectToAction("CEOIndex",
                    new { status = returnStatus });
            }
        }

        [HttpGet]
        public async Task<IActionResult> ProjectManagerIndex()
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);

                if (currentUser == null)
                {
                    return RedirectToAction("Login", "Account");
                }

                var employee = await _context.Employee
                    .FirstOrDefaultAsync(x =>
                        x.ApplicationUserId == currentUser.Id);

                if (employee == null)
                {
                    TempData["Error"] = "Employee record not found.";
                    return View(new List<AdminProject>());
                }

                var projects = await _context.AdminProjects
                    .Where(x =>
                        x.IsActive &&
                        x.ProjectManagerId == employee.EmployeeId)
                    .OrderByDescending(x => x.CreatedDate)
                    .ToListAsync();

                return View(projects);
            }
            catch (Exception ex)
            {
                // Log exception using ILogger in your controller.
                TempData["Error"] = "Unable to load assigned projects.";

                return View(new List<AdminProject>());
            }
        }

        [HttpGet]
        public async Task<IActionResult> ProjectManagerDetails(int id)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);

                if (currentUser == null)
                {
                    return RedirectToAction("Login", "Account");
                }

                var employee = await _context.Employee
                    .FirstOrDefaultAsync(x =>
                        x.ApplicationUserId == currentUser.Id);

                if (employee == null)
                {
                    TempData["Error"] = "Employee record not found.";
                    return RedirectToAction("ProjectManagerIndex");
                }

                // Verify project assignment
                var project = await _context.AdminProjects
                    .FirstOrDefaultAsync(x =>
                        x.ProjectId == id &&
                        x.ProjectManagerId == employee.EmployeeId &&
                        x.IsActive);

                if (project == null)
                {
                    TempData["Error"] = "Project not found or not assigned to you.";
                    return RedirectToAction("ProjectManagerIndex");
                }

                // Load milestones and vendors
                ViewBag.ProjectMilestones = await _context.ProjectMilestones
                    .Where(x => x.ProjectId == id)
                    .Include(x => x.Vendor)
                    .ToListAsync();

                // Load assigned employees
                ViewBag.ProjectTeams = await _context.ProjectTeams
                    .Where(x => x.ProjectId == id)
                    .Include(x => x.Employee)
                    .ToListAsync();

                // Load department budgets
                ViewBag.ProjectBudgets = await _context.ProjectDepartmentBudgets
                    .Where(x => x.ProjectId == id)
                    .ToListAsync();

                // Load project tasks
                ViewBag.ProjectTasks = await _context.ProjectTasks
                    .Where(x => x.ProjectId == id)
                    .Include(x => x.Employee)
                    .Include(x => x.Milestone)
                    .OrderByDescending(x => x.CreatedDate)
                    .ToListAsync();

                // Load milestone update history
                ViewBag.MilestoneUpdates = await _context.ProjectMilestoneUpdates
                    .Where(x => x.ProjectId == id)
                    .Include(x => x.Milestone)
                    .Include(x => x.Employee)
                    .OrderByDescending(x => x.UpdateDate)
                    .ToListAsync();

                ViewBag.ProjectManagerEmployeeId = employee.EmployeeId;

                return View("ProjectManagerDetails", project);
            }
            catch (Exception ex)
            {
                // Log exception using ILogger
                TempData["Error"] = "Unable to load project details.";

                return RedirectToAction("ProjectManagerIndex");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignProjectTask(
    int ProjectId,
    int MilestoneId,
    int EmployeeId,
    string TaskName,
    string? TaskDescription,
    DateTime StartDate,
    DateTime DueDate)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);

                if (currentUser == null)
                    return RedirectToAction("Login", "Account");

                var employee = await _context.Employee
                    .FirstOrDefaultAsync(x => x.ApplicationUserId == currentUser.Id);

                if (employee == null)
                {
                    TempData["Error"] = "Employee record not found.";
                    return RedirectToAction("ProjectManagerIndex");
                }

                var project = await _context.AdminProjects
                    .FirstOrDefaultAsync(x =>
                        x.ProjectId == ProjectId &&
                        x.ProjectManagerId == employee.EmployeeId &&
                        x.IsActive);

                if (project == null)
                {
                    TempData["Error"] = "Project not found or not assigned to you.";
                    return RedirectToAction("ProjectManagerIndex");
                }

                var milestone = await _context.ProjectMilestones
                    .FirstOrDefaultAsync(x =>
                        x.MilestoneId == MilestoneId &&
                        x.ProjectId == ProjectId);

                if (milestone == null)
                {
                    TempData["Error"] = "Invalid milestone selected.";
                    return RedirectToAction("ProjectManagerDetails", new { id = ProjectId });
                }

                var employeeAssigned = await _context.ProjectTeams
                    .AnyAsync(x =>
                        x.ProjectId == ProjectId &&
                        x.EmployeeId == EmployeeId);

                if (!employeeAssigned)
                {
                    TempData["Error"] = "Selected employee is not assigned to this project.";
                    return RedirectToAction("ProjectManagerDetails", new { id = ProjectId });
                }

                if (string.IsNullOrWhiteSpace(TaskName) ||
                    TaskName.Trim().Length > 200)
                {
                    TempData["Error"] = "Please enter a valid task name.";
                    return RedirectToAction("ProjectManagerDetails", new { id = ProjectId });
                }

                if (DueDate.Date < StartDate.Date ||
                    StartDate.Date < milestone.StartDate.Date ||
                    DueDate.Date > milestone.EndDate.Date)
                {
                    TempData["Error"] = "Task dates must be within the milestone timeline.";
                    return RedirectToAction("ProjectManagerDetails", new { id = ProjectId });
                }

                if (TaskDescription?.Length > 10000)
                {
                    TempData["Error"] = "Task description is too long.";
                    return RedirectToAction("ProjectManagerDetails", new { id = ProjectId });
                }

                var task = new ProjectTask
                {
                    ProjectId = ProjectId,
                    MilestoneId = MilestoneId,
                    EmployeeId = EmployeeId,
                    TaskName = TaskName.Trim(),
                    TaskDescription = string.IsNullOrWhiteSpace(TaskDescription)
                        ? null : TaskDescription.Trim(),
                    StartDate = StartDate.Date,
                    DueDate = DueDate.Date,
                    Status = "Assigned",
                    AssignedBy = employee.EmployeeId,
                    CreatedDate = DateTime.Now
                };

                _context.ProjectTasks.Add(task);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Task assigned successfully.";

                return RedirectToAction("ProjectManagerDetails", new { id = ProjectId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning task to project {ProjectId}", ProjectId);

                TempData["Error"] = "Unable to assign task.";
                return RedirectToAction("ProjectManagerDetails", new { id = ProjectId });
            }
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveMilestoneUpdate(
            int ProjectId,
            int MilestoneId,
            DateTime UpdateDate,
            string CurrentStatus,
            decimal ProgressPercentage,
            IFormFile? SupportingFile,
            string? Remarks)
        {
            string? savedFilePath = null;
            string? physicalFilePath = null;

            try
            {
                // 1. Logged-in user
                var currentUser = await _userManager.GetUserAsync(User);

                if (currentUser == null)
                    return RedirectToAction("Login", "Account");

                // 2. Employee verification
                var employee = await _context.Employee
                    .FirstOrDefaultAsync(x =>
                        x.ApplicationUserId == currentUser.Id);

                if (employee == null)
                {
                    TempData["Error"] = "Employee record not found.";
                    return RedirectToAction("ProjectManagerIndex");
                }

                // 3. Project verification
                var project = await _context.AdminProjects
                    .FirstOrDefaultAsync(x =>
                        x.ProjectId == ProjectId &&
                        x.ProjectManagerId == employee.EmployeeId &&
                        x.IsActive);

                if (project == null)
                {
                    TempData["Error"] =
                        "Project not found or not assigned to you.";

                    return RedirectToAction("ProjectManagerIndex");
                }

                // 4. Milestone verification
                var milestone = await _context.ProjectMilestones
                    .FirstOrDefaultAsync(x =>
                        x.MilestoneId == MilestoneId &&
                        x.ProjectId == ProjectId);

                if (milestone == null)
                {
                    TempData["Error"] = "Invalid milestone selected.";

                    return RedirectToAction(
                        "ProjectManagerDetails",
                        new { id = ProjectId });
                }

                // 5. Status validation
                var allowedStatuses = new[]
                {
            "Not Started",
            "In Progress",
            "Completed",
            "On Hold"
        };

                if (string.IsNullOrWhiteSpace(CurrentStatus) ||
                    !allowedStatuses.Contains(CurrentStatus))
                {
                    TempData["Error"] = "Invalid milestone status.";

                    return RedirectToAction(
                        "ProjectManagerDetails",
                        new { id = ProjectId });
                }

                // 6. Progress validation
                if (ProgressPercentage < 0 || ProgressPercentage > 100)
                {
                    TempData["Error"] =
                        "Progress must be between 0 and 100.";

                    return RedirectToAction(
                        "ProjectManagerDetails",
                        new { id = ProjectId });
                }

                // 7. Remarks validation
                if (Remarks?.Length > 4000)
                {
                    TempData["Error"] =
                        "Remarks cannot exceed 4000 characters.";

                    return RedirectToAction(
                        "ProjectManagerDetails",
                        new { id = ProjectId });
                }

                // 8. Document upload
                if (SupportingFile != null && SupportingFile.Length > 0)
                {
                    const long maxFileSize = 5 * 1024 * 1024;

                    if (SupportingFile.Length > maxFileSize)
                    {
                        TempData["Error"] = "Maximum file size is 5 MB.";

                        return RedirectToAction(
                            "ProjectManagerDetails",
                            new { id = ProjectId });
                    }

                    var allowedExtensions = new[]
                    {
                ".pdf",
                ".doc",
                ".docx"
            };

                    var extension = Path.GetExtension(
                        SupportingFile.FileName).ToLowerInvariant();

                    if (!allowedExtensions.Contains(extension))
                    {
                        TempData["Error"] =
                            "Only PDF, DOC and DOCX files are allowed.";

                        return RedirectToAction(
                            "ProjectManagerDetails",
                            new { id = ProjectId });
                    }

                    // Resolve wwwroot directory
                    var webRootPath = _enviroment.WebRootPath;

                    if (string.IsNullOrWhiteSpace(webRootPath))
                    {
                        webRootPath = Path.Combine(
                            _enviroment.ContentRootPath,
                            "wwwroot");
                    }

                    // Create upload directory
                    var uploadFolder = Path.Combine(
                        webRootPath,
                        "uploads",
                        "project-milestone-updates");

                    Directory.CreateDirectory(uploadFolder);

                    // Generate unique filename
                    var fileName = $"{Guid.NewGuid():N}{extension}";

                    physicalFilePath = Path.Combine(
                        uploadFolder,
                        fileName);

                    // Save document physically
                    await using (var stream = new FileStream(
                        physicalFilePath,
                        FileMode.CreateNew,
                        FileAccess.Write,
                        FileShare.None))
                    {
                        await SupportingFile.CopyToAsync(stream);
                    }

                    // Relative path saved in database
                    savedFilePath =
                        $"/uploads/project-milestone-updates/{fileName}";
                }

                // 9. Save milestone update
                var milestoneUpdate = new ProjectMilestoneUpdate
                {
                    ProjectId = ProjectId,
                    MilestoneId = MilestoneId,
                    UpdateDate = UpdateDate.Date,
                    CurrentStatus = CurrentStatus,
                    ProgressPercentage = ProgressPercentage,
                    SupportingDocument = savedFilePath,
                    Remarks = string.IsNullOrWhiteSpace(Remarks)
                        ? null
                        : Remarks.Trim(),
                    UpdatedBy = employee.EmployeeId,
                    CreatedDate = DateTime.Now
                };

                _context.ProjectMilestoneUpdates.Add(milestoneUpdate);

                await _context.SaveChangesAsync();

                TempData["Success"] =
                    "Milestone progress and document saved successfully.";

                return RedirectToAction(
                    "ProjectManagerDetails",
                    new { id = ProjectId });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error saving milestone update for Project {ProjectId}, Milestone {MilestoneId}",
                    ProjectId,
                    MilestoneId);

                // Delete uploaded file if database save fails
                if (!string.IsNullOrWhiteSpace(physicalFilePath))
                {
                    try
                    {
                        if (System.IO.File.Exists(physicalFilePath))
                        {
                            System.IO.File.Delete(physicalFilePath);
                        }
                    }
                    catch (Exception fileEx)
                    {
                        _logger.LogError(
                            fileEx,
                            "Error deleting uploaded milestone document.");
                    }
                }

                TempData["Error"] =
                    "Unable to save milestone update. Please try again.";

                return RedirectToAction(
                    "ProjectManagerDetails",
                    new { id = ProjectId });
            }
        }


        [HttpGet]
        public async Task<IActionResult> MyTasks()
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);

                if (currentUser == null)
                    return RedirectToAction("Login", "Account");

                var employee = await _context.Employee
                    .FirstOrDefaultAsync(x => x.ApplicationUserId == currentUser.Id);

                if (employee == null)
                {
                    TempData["Error"] = "Employee record not found.";
                    return RedirectToAction("Index", "Home");
                }

                // Check whether employee belongs to any project team
                var isProjectTeamEmployee = await _context.ProjectTeams
                    .AnyAsync(x => x.EmployeeId == employee.EmployeeId);

                if (!isProjectTeamEmployee)
                {
                    TempData["Error"] = "You are not assigned to any project.";
                    return RedirectToAction("Index", "Home");
                }

                // Get only logged-in employee's tasks
                var tasks = await _context.ProjectTasks
                    .Where(x => x.EmployeeId == employee.EmployeeId)
                    .OrderByDescending(x => x.CreatedDate)
                    .ToListAsync();

                return View("MyTasks", tasks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading employee project tasks.");

                TempData["Error"] = "Unable to load your project tasks.";
                return RedirectToAction("Index", "Home");
            }
        }
        [HttpGet]
        public async Task<IActionResult> MyTaskDetails(int id)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);

                if (currentUser == null)
                    return RedirectToAction("Login", "Account");

                var employee = await _context.Employee
                    .FirstOrDefaultAsync(x => x.ApplicationUserId == currentUser.Id);

                if (employee == null)
                {
                    TempData["Error"] = "Employee record not found.";
                    return RedirectToAction("MyTasks");
                }

                // Verify employee belongs to the project team
                var isProjectTeamEmployee = await _context.ProjectTeams
                    .AnyAsync(x =>
                        x.EmployeeId == employee.EmployeeId);

                if (!isProjectTeamEmployee)
                {
                    TempData["Error"] = "You are not assigned to any project.";
                    return RedirectToAction("MyTasks");
                }

                // Get only logged-in employee's task
                var task = await _context.ProjectTasks
                    .FirstOrDefaultAsync(x =>
                        x.TaskId == id &&
                        x.EmployeeId == employee.EmployeeId);

                if (task == null)
                {
                    TempData["Error"] = "Task not found or not assigned to you.";
                    return RedirectToAction("MyTasks");
                }

                return View("MyTaskDetails", task);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error loading task details. TaskId: {TaskId}", id);

                TempData["Error"] = "Unable to load task details.";
                return RedirectToAction("MyTasks");
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateMyTask(
    int id,
    string Status,
    string? EmployeeRemarks,
    IFormFile? ReportFile)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);

                if (currentUser == null)
                    return RedirectToAction("Login", "Account");

                var employee = await _context.Employee
                    .FirstOrDefaultAsync(x => x.ApplicationUserId == currentUser.Id);

                if (employee == null)
                {
                    TempData["Error"] = "Employee record not found.";
                    return RedirectToAction("MyTasks");
                }

                var task = await _context.ProjectTasks.FirstOrDefaultAsync(x =>
                    x.TaskId == id &&
                    x.EmployeeId == employee.EmployeeId);

                if (task == null)
                {
                    TempData["Error"] = "Task not found.";
                    return RedirectToAction("MyTasks");
                }

                var validStatuses = new[] { "In Progress", "Completed" };

                if (!validStatuses.Contains(Status))
                {
                    TempData["Error"] = "Invalid task status.";
                    return RedirectToAction("MyTaskDetails", new { id });
                }

                if (task.Status == "Completed")
                {
                    TempData["Error"] = "Task is already completed.";
                    return RedirectToAction("MyTaskDetails", new { id });
                }

                if (Status == "Completed")
                {
                    if (string.IsNullOrWhiteSpace(EmployeeRemarks))
                    {
                        TempData["Error"] = "Please enter completion remarks.";
                        return RedirectToAction("MyTaskDetails", new { id });
                    }

                    if (ReportFile == null || ReportFile.Length == 0)
                    {
                        TempData["Error"] = "Please upload completion report.";
                        return RedirectToAction("MyTaskDetails", new { id });
                    }
                }

                if (ReportFile != null && ReportFile.Length > 0)
                {
                    var allowedExtensions = new[] { ".pdf", ".doc", ".docx" };
                    var extension = Path.GetExtension(ReportFile.FileName).ToLowerInvariant();

                    if (!allowedExtensions.Contains(extension))
                    {
                        TempData["Error"] = "Only PDF, DOC and DOCX files are allowed.";
                        return RedirectToAction("MyTaskDetails", new { id });
                    }

                    if (ReportFile.Length > 5 * 1024 * 1024)
                    {
                        TempData["Error"] = "Maximum file size is 5 MB.";
                        return RedirectToAction("MyTaskDetails", new { id });
                    }

                    var folder = Path.Combine(
                        Directory.GetCurrentDirectory(),
                        "wwwroot/uploads/ProjectTaskReports");

                    if (!Directory.Exists(folder))
                        Directory.CreateDirectory(folder);

                    var fileName = Guid.NewGuid() + extension;
                    var filePath = Path.Combine(folder, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await ReportFile.CopyToAsync(stream);
                    }

                    task.EmployeeAttachmentPath =
                        "/uploads/ProjectTaskReports/" + fileName;
                }

                // SAVE TASK DETAILS
                task.Status = Status;
                task.EmployeeRemarks = EmployeeRemarks?.Trim();
                task.ModifiedDate = DateTime.Now;

                // SAVE CHANGES IN DATABASE
                await _context.SaveChangesAsync();

                TempData["Success"] = "Task status updated successfully.";

                return RedirectToAction("MyTasks", new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating task {TaskId}", id);

                TempData["Error"] = "Unable to update task.";
                return RedirectToAction("MyTaskDetails", new { id });
            }
        }
        [HttpGet]
        public async Task<IActionResult> CompletedProjectTasks()
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);

                if (currentUser == null)
                    return RedirectToAction("Login", "Account");

                var employee = await _context.Employee
                    .FirstOrDefaultAsync(x => x.ApplicationUserId == currentUser.Id);

                if (employee == null)
                {
                    TempData["Error"] = "Employee record not found.";
                    return RedirectToAction("ProjectManagerIndex");
                }

                var completedTasks = await (
                    from task in _context.ProjectTasks
                    join project in _context.AdminProjects
                        on task.ProjectId equals project.ProjectId
                    where project.ProjectManagerId == employee.EmployeeId
                          && project.IsActive
                          && task.Status == "Completed"
                    orderby task.ModifiedDate descending
                    select task
                ).ToListAsync();

                return View("CompletedProjectTasks", completedTasks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading completed project tasks.");

                TempData["Error"] = "Unable to load completed tasks.";
                return RedirectToAction("ProjectManagerIndex");
            }
        }

        [HttpGet]
        public async Task<IActionResult> Timeline()
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);

                if (currentUser == null)
                    return RedirectToAction("Login", "Account");

                var employee = await _context.Employee
                    .FirstOrDefaultAsync(x => x.ApplicationUserId == currentUser.Id);

                if (employee == null)
                {
                    TempData["Error"] = "Employee record not found.";
                    return RedirectToAction("ProjectManagerIndex");
                }

                var projects = await _context.AdminProjects
                    .Where(x => x.ProjectManagerId == employee.EmployeeId && x.IsActive)
                    .OrderByDescending(x => x.CreatedDate)
                    .ToListAsync();

                return View("Timeline", projects);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading project list.");
                TempData["Error"] = "Unable to load projects.";
                return RedirectToAction("ProjectManagerIndex");
            }
        }
        [HttpGet]
        public async Task<IActionResult> ProjectTimeline(int id)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);

                if (currentUser == null)
                    return RedirectToAction("Login", "Account");

                var employee = await _context.Employee
                    .FirstOrDefaultAsync(x => x.ApplicationUserId == currentUser.Id);

                if (employee == null)
                {
                    TempData["Error"] = "Employee record not found.";
                    return RedirectToAction("Timeline");
                }

                var project = await _context.AdminProjects
                    .FirstOrDefaultAsync(x =>
                        x.ProjectId == id &&
                        x.ProjectManagerId == employee.EmployeeId &&
                        x.IsActive);

                if (project == null)
                {
                    TempData["Error"] = "Project not found or not assigned to you.";
                    return RedirectToAction("Timeline");
                }

                return View("ProjectTimeline", project);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading project timeline. ProjectId: {ProjectId}", id);

                TempData["Error"] = "Unable to load project timeline.";
                return RedirectToAction("Timeline");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProjectTimeline(
    int ProjectId,
    string ProjectApprovalStatus,
    string InfrastructureSetupStatus,
    string TrainerAssignmentStatus,
    string BatchCreationStatus,
    string TrainingStartStatus,
    string AssessmentStatus,
    string CertificationStatus)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);

                if (currentUser == null)
                    return RedirectToAction("Login", "Account");

                var employee = await _context.Employee
                    .FirstOrDefaultAsync(x => x.ApplicationUserId == currentUser.Id);

                if (employee == null)
                {
                    TempData["Error"] = "Employee record not found.";
                    return RedirectToAction("Timeline");
                }

                var project = await _context.AdminProjects
                    .FirstOrDefaultAsync(x =>
                        x.ProjectId == ProjectId &&
                        x.ProjectManagerId == employee.EmployeeId &&
                        x.IsActive);

                if (project == null)
                {
                    TempData["Error"] = "Project not found or not assigned to you.";
                    return RedirectToAction("Timeline");
                }

                var validStatuses = new HashSet<string>
        {
            "Pending",
            "In Progress",
            "Completed",
            "Delayed"
        };

                var statuses = new[]
                {
            ProjectApprovalStatus,
            InfrastructureSetupStatus,
            TrainerAssignmentStatus,
            BatchCreationStatus,
            TrainingStartStatus,
            AssessmentStatus,
            CertificationStatus
        };

                if (statuses.Any(x =>
                    string.IsNullOrWhiteSpace(x) ||
                    !validStatuses.Contains(x)))
                {
                    TempData["Error"] = "Please select valid statuses.";

                    return RedirectToAction(
                        "ProjectTimeline",
                        new { id = ProjectId });
                }

                project.ProjectApprovalStatus = ProjectApprovalStatus;
                project.InfrastructureSetupStatus = InfrastructureSetupStatus;
                project.TrainerAssignmentStatus = TrainerAssignmentStatus;
                project.BatchCreationStatus = BatchCreationStatus;
                project.TrainingStartStatus = TrainingStartStatus;
                project.AssessmentStatus = AssessmentStatus;
                project.CertificationStatus = CertificationStatus;

                project.ModifiedBy = currentUser.Id;
                project.ModifiedDate = DateTime.Now;

                await _context.SaveChangesAsync();

                TempData["Success"] = "Project timeline updated successfully.";

                return RedirectToAction(
                    "ProjectTimeline",
                    new { id = ProjectId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error updating project timeline. ProjectId: {ProjectId}",
                    ProjectId);

                TempData["Error"] = "Unable to update project timeline.";

                return RedirectToAction(
                    "ProjectTimeline",
                    new { id = ProjectId });
            }
        }


        [HttpGet]
        public async Task<IActionResult> BudgetTracking()
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);

                if (currentUser == null)
                    return RedirectToAction("Login", "Account");

                var employee = await _context.Employee
                    .FirstOrDefaultAsync(x => x.ApplicationUserId == currentUser.Id);

                if (employee == null)
                {
                    TempData["Error"] = "Employee record not found.";
                    return RedirectToAction("Index", "Home");
                }

                var projects = await _context.AdminProjects
                    .Where(x => x.ProjectManagerId == employee.EmployeeId
                                && x.IsActive)
                    .OrderByDescending(x => x.CreatedDate)
                    .ToListAsync();

                return View("BudgetTracking", projects);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading project budget tracking list.");

                TempData["Error"] = "Unable to load budget tracking projects.";
                return RedirectToAction("Index", "Home");
            }
        }


        [HttpGet]
        public async Task<IActionResult> ProjectBudgetDetails(int id)
        {
            try
            {
                // 1. Get logged-in user
                var currentUser = await _userManager.GetUserAsync(User);

                if (currentUser == null)
                    return RedirectToAction("Login", "Account");

                // 2. Get employee
                var employee = await _context.Employee
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.ApplicationUserId == currentUser.Id);

                if (employee == null)
                {
                    TempData["Error"] = "Employee record not found.";
                    return RedirectToAction(nameof(BudgetTracking));
                }

                // 3. Verify project ownership
                var project = await _context.AdminProjects
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x =>
                        x.ProjectId == id &&
                        x.ProjectManagerId == employee.EmployeeId &&
                        x.IsActive);

                if (project == null)
                {
                    TempData["Error"] = "Project not found or not assigned to you.";
                    return RedirectToAction(nameof(BudgetTracking));
                }

                // 4. Fetch vendors assigned to this project's milestones
                var vendors = await _context.ProjectVendors
                    .AsNoTracking()
                    .Where(v => v.IsActive &&
                        _context.ProjectMilestones.Any(m =>
                            m.ProjectId == project.ProjectId &&
                            m.VendorId == v.VendorId))
                    .OrderBy(v => v.VendorName)
                    .ToListAsync();

                // 5. Create expense model
                var expense = new ProjectBudgetExpense
                {
                    ProjectId = project.ProjectId,
                    ExpenseDate = DateTime.Today
                };

                // 6. Project name
                ViewBag.ProjectName = project.ProjectName;

                // 7. Convert vendors into SelectListItem
                var vendorList = vendors.Select(v =>
                    new SelectListItem
                    {
                        Value = v.VendorId.ToString(),
                        Text = v.VendorName
                    }).ToList();

                ViewBag.VendorList = vendorList;
                ViewBag.VendorCount = vendorList.Count;

                // 8. Single vendor auto selection
                if (vendorList.Count == 1)
                {
                    expense.VendorId = Convert.ToInt32(vendorList[0].Value);

                    ViewBag.HasSingleVendor = true;
                    ViewBag.SingleVendorName = vendorList[0].Text;
                }
                else
                {
                    ViewBag.HasSingleVendor = false;
                }

                // 9. Return view
                return View("ProjectBudgetDetails", expense);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error loading budget expense form for ProjectId: {ProjectId}", id);

                TempData["Error"] = ex.GetBaseException().Message;

                return RedirectToAction(nameof(BudgetTracking));
            }
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddBudgetExpense(
            ProjectBudgetExpense model,
            IFormFile? InvoiceFile)
        {
            try
            {
                // 1. Get logged-in user
                var currentUser = await _userManager.GetUserAsync(User);

                if (currentUser == null)
                    return RedirectToAction("Login", "Account", new { area = "" });

                // 2. Get employee
                var employee = await _context.Employee
                    .FirstOrDefaultAsync(x => x.ApplicationUserId == currentUser.Id);

                if (employee == null)
                {
                    TempData["Error"] = "Employee record not found.";
                    return RedirectToAction(nameof(BudgetTracking));
                }

                // 3. Validate project
                var project = await _context.AdminProjects
                    .FirstOrDefaultAsync(x =>
                        x.ProjectId == model.ProjectId &&
                        x.ProjectManagerId == employee.EmployeeId &&
                        x.IsActive);

                if (project == null)
                {
                    TempData["Error"] = "Invalid project or project not assigned to you.";
                    return RedirectToAction(nameof(BudgetTracking));
                }

                // 4. Validate expense title
                if (string.IsNullOrWhiteSpace(model.ExpenseTitle))
                    ModelState.AddModelError("ExpenseTitle", "Expense title is required.");

                // 5. Validate amount
                if (model.ExpenseAmount <= 0)
                    ModelState.AddModelError("ExpenseAmount", "Enter a valid expense amount.");

                // 6. Validate vendor
                if (!model.VendorId.HasValue)
                {
                    ModelState.AddModelError("VendorId", "Please select a vendor.");
                }
                else
                {
                    bool validVendor = await _context.ProjectVendors.AnyAsync(v =>
                        v.VendorId == model.VendorId.Value &&
                        v.IsActive &&
                        _context.ProjectMilestones.Any(m =>
                            m.ProjectId == project.ProjectId &&
                            m.VendorId == v.VendorId));

                    if (!validVendor)
                        ModelState.AddModelError("VendorId", "Invalid vendor selected.");
                }

                // 7. Validate invoice
                if (InvoiceFile != null && InvoiceFile.Length > 0)
                {
                    string extension = Path.GetExtension(InvoiceFile.FileName)
                        .ToLowerInvariant();

                    string[] allowedExtensions =
                    {
                ".pdf", ".jpg", ".jpeg", ".png"
            };

                    if (!allowedExtensions.Contains(extension))
                        ModelState.AddModelError("InvoiceFile", "Invalid invoice file format.");

                    if (InvoiceFile.Length > 5 * 1024 * 1024)
                        ModelState.AddModelError("InvoiceFile", "Maximum file size is 5 MB.");
                }

                // 8. Reload form if validation fails
                if (!ModelState.IsValid)
                {
                    var vendors = await _context.ProjectVendors
                        .AsNoTracking()
                        .Where(v => v.IsActive &&
                            _context.ProjectMilestones.Any(m =>
                                m.ProjectId == project.ProjectId &&
                                m.VendorId == v.VendorId))
                        .OrderBy(v => v.VendorName)
                        .ToListAsync();

                    ViewBag.ProjectName = project.ProjectName;

                    ViewBag.VendorList = vendors.Select(v => new SelectListItem
                    {
                        Value = v.VendorId.ToString(),
                        Text = v.VendorName
                    }).ToList();

                    ViewBag.VendorCount = vendors.Count;
                    ViewBag.HasSingleVendor = vendors.Count == 1;
                    ViewBag.SingleVendorName = vendors.Count == 1
                        ? vendors[0].VendorName : null;

                    return View("ProjectBudgetDetails", model);
                }

                // 9. Get department budget
                var departmentBudget = await _context.ProjectDepartmentBudgets
                    .FirstOrDefaultAsync(x =>
                        x.ProjectId == project.ProjectId);

                if (departmentBudget == null)
                {
                    TempData["Error"] = "Department budget not found for this project.";
                    return RedirectToAction(nameof(BudgetTracking));
                }

                // 10. Save invoice
                if (InvoiceFile != null && InvoiceFile.Length > 0)
                {
                    string folder = Path.Combine(
                        _enviroment.WebRootPath,
                        "uploads",
                        "ProjectBudgetInvoices");

                    Directory.CreateDirectory(folder);

                    string fileName = Guid.NewGuid().ToString("N") +
                        Path.GetExtension(InvoiceFile.FileName).ToLowerInvariant();

                    string filePath = Path.Combine(folder, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await InvoiceFile.CopyToAsync(stream);
                    }

                    model.InvoiceAttachment =
                        "/uploads/ProjectBudgetInvoices/" + fileName;
                }

                // 11. Set database values
                model.BudgetId = departmentBudget.BudgetId;
                model.ProjectId = project.ProjectId;
                model.CreatedBy = employee.EmployeeId;
                model.CreatedDate = DateTime.Now;
                model.PaymentMode = string.IsNullOrWhiteSpace(model.PaymentMode)
                    ? null
                    : model.PaymentMode.Trim();
                model.InvoiceNumber = string.IsNullOrWhiteSpace(model.InvoiceNumber)
                    ? null
                    : model.InvoiceNumber.Trim();
                model.Remarks = string.IsNullOrWhiteSpace(model.Remarks)
                    ? null
                    : model.Remarks.Trim();

                // LEVEL 1: Business Head Approval
                model.BusinessHeadStatus = "Pending";
                model.BusinessHeadRemarks = null;
                model.BusinessHeadReviewedBy = null;
                model.BusinessHeadReviewedDate = null;

                // LEVEL 2: CEO Approval
                model.CEOStatus = null;
                model.CEORemarks = null;
                model.CEOReviewedBy = null;
                model.CEOReviewedDate = null;

                // 12. Save expense
                _context.ProjectBudgetExpenses.Add(model);

                await _context.SaveChangesAsync();

                TempData["Success"] =
                    "Expense submitted successfully to Business Head.";

                return RedirectToAction(
                    nameof(ProjectBudgetDetails),
                    new { id = project.ProjectId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AddBudgetExpense failed.");

                TempData["Error"] =
                    "Unable to save expense: " + ex.GetBaseException().Message;

                return RedirectToAction(nameof(BudgetTracking));
            }
        }

        [HttpGet]
        public async Task<IActionResult> ApprovalBudgetTracking()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);

                if (user == null)
                    return RedirectToAction("Login", "Account");

                var employee = await _context.Employee
                    .FirstOrDefaultAsync(x => x.ApplicationUserId == user.Id);

                if (employee == null)
                    return NotFound("Employee record not found.");

                var roles = await _userManager.GetRolesAsync(user);

                IQueryable<AdminProject> query = _context.AdminProjects
                    .Where(x => x.IsActive);

                if (roles.Contains("BusinessHead"))
                {
                    // Business Head ke assigned projects
                    query = query.Where(x =>
                        x.BusinessHeadId == employee.EmployeeId);
                }
                else if (roles.Contains("CEO"))
                {
                    // Sirf woh projects jinke expenses Business Head ne approve kiye hain
                    var approvedProjectIds = _context.ProjectBudgetExpenses
                        .Where(x => x.BusinessHeadStatus == "Approved")
                        .Select(x => x.ProjectId)
                        .Distinct();

                    query = query.Where(x =>
                        approvedProjectIds.Contains(x.ProjectId));
                }
                else
                {
                    return Forbid();
                }

                var projects = await query
                    .OrderByDescending(x => x.CreatedDate)
                    .ToListAsync();

                return View("ApprovalBudgetTracking", projects);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading approval budget tracking.");

                TempData["Error"] = "Unable to load budget tracking.";
                return RedirectToAction("Index");
            }
        }



        [HttpGet]
        public async Task<IActionResult> ApprovalBudgetDetails(int id)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return RedirectToAction("Login", "Account", new { area = "" });

            var employee = await _context.Employee
                .FirstOrDefaultAsync(x => x.ApplicationUserId == user.Id);

            if (employee == null)
                return NotFound("Employee not found.");

            var roles = await _userManager.GetRolesAsync(user);

            bool isBusinessHead = roles.Contains("BusinessHead");
            bool isCEO = roles.Contains("CEO");

            if (!isBusinessHead && !isCEO)
                return Forbid();

            var project = await _context.AdminProjects
                .FirstOrDefaultAsync(x => x.ProjectId == id && x.IsActive);

            if (project == null)
                return NotFound("Project not found.");

            IQueryable<ProjectBudgetExpense> query =
                _context.ProjectBudgetExpenses
                    .Where(x => x.ProjectId == id);

            if (isBusinessHead)
            {
                if (project.BusinessHeadId != employee.EmployeeId)
                    return Forbid();

            }
            else if (isCEO)
            {
                query = query.Where(x => x.BusinessHeadStatus == "Approved");
            }

            var expenses = await query
                .OrderByDescending(x => x.CreatedDate)
                .ToListAsync();

            ViewBag.ProjectName = project.ProjectName;
            ViewBag.ProjectId = project.ProjectId;
            ViewBag.IsCEO = isCEO;
            var vendorNames = await _context.ProjectVendors
    .AsNoTracking()
    .Where(v => _context.ProjectMilestones.Any(m =>
        m.ProjectId == id &&
        m.VendorId == v.VendorId))
    .ToDictionaryAsync(
        v => v.VendorId,
        v => v.VendorName);

            ViewBag.VendorNames = vendorNames;

            return View("ApprovalBudgetDetails", expenses);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApprovalExpenseReview(
            int expenseId,
            string status,
            string remarks)
        {
            try
            {
                // 1. Get logged-in user
                var currentUser = await _userManager.GetUserAsync(User);

                if (currentUser == null)
                    return RedirectToAction("Login", "Account", new { area = "" });

                // 2. Get employee
                var employee = await _context.Employee
                    .FirstOrDefaultAsync(x => x.ApplicationUserId == currentUser.Id);

                if (employee == null)
                {
                    TempData["Error"] = "Employee record not found.";
                    return RedirectToAction(nameof(Index));
                }

                // 3. Get user roles
                var roles = await _userManager.GetRolesAsync(currentUser);

                bool isBusinessHead = roles.Contains("BusinessHead");
                bool isCEO = roles.Contains("CEO");

                if (!isBusinessHead && !isCEO)
                    return Forbid();

                // 4. Validate status
                string[] allowedStatuses =
                {
            "Approved",
            "Rejected",
            "On Hold"
        };

                if (!allowedStatuses.Contains(status))
                {
                    TempData["Error"] = "Invalid approval status.";
                    return RedirectToAction(nameof(ApprovalBudgetTracking));
                }

                // 5. Validate remarks
                if (string.IsNullOrWhiteSpace(remarks))
                {
                    TempData["Error"] = "Please enter remarks.";

                    return RedirectToAction(
                        nameof(ApprovalBudgetDetails),
                        new { id = expenseId });
                }

                // 6. Get expense
                var expense = await _context.ProjectBudgetExpenses
                    .FirstOrDefaultAsync(x => x.ExpenseId == expenseId);

                if (expense == null)
                {
                    TempData["Error"] = "Expense not found.";
                    return RedirectToAction(nameof(ApprovalBudgetTracking));
                }

                // 7. Get project
                var project = await _context.AdminProjects
                    .FirstOrDefaultAsync(x =>
                        x.ProjectId == expense.ProjectId &&
                        x.IsActive);

                if (project == null)
                {
                    TempData["Error"] = "Project not found.";
                    return RedirectToAction(nameof(ApprovalBudgetTracking));
                }

                // 8. BUSINESS HEAD REVIEW
                if (isBusinessHead)
                {
                    // Verify assigned Business Head
                    if (project.BusinessHeadId != employee.EmployeeId)
                        return Forbid();

                    // Only Pending or On Hold expenses can be reviewed
                    if (expense.BusinessHeadStatus != "Pending" &&
                        expense.BusinessHeadStatus != "On Hold")
                    {
                        TempData["Error"] =
                            "This expense is not available for Business Head review.";

                        return RedirectToAction(
                            nameof(ApprovalBudgetDetails),
                            new { id = expense.ProjectId });
                    }

                    expense.BusinessHeadStatus = status;
                    expense.BusinessHeadRemarks = remarks.Trim();
                    expense.BusinessHeadReviewedBy = employee.EmployeeId;
                    expense.BusinessHeadReviewedDate = DateTime.Now;

                    // CEO review becomes available only after BH approval
                    if (status == "Approved")
                    {
                        expense.CEOStatus = "Pending";
                        expense.CEORemarks = null;
                        expense.CEOReviewedBy = null;
                        expense.CEOReviewedDate = null;
                    }
                    else
                    {
                        // Rejected or On Hold cannot go to CEO
                        expense.CEOStatus = "Pending";
                        expense.CEORemarks = null;
                        expense.CEOReviewedBy = null;
                        expense.CEOReviewedDate = null;
                    }

                    TempData["Success"] =
                        $"Expense {status} successfully by Business Head.";
                }

                // 9. CEO REVIEW
                else if (isCEO)
                {
                    // CEO can review only BH-approved expenses
                    if (expense.BusinessHeadStatus != "Approved")
                    {
                        TempData["Error"] =
                            "Business Head has not approved this expense.";

                        return RedirectToAction(
                            nameof(ApprovalBudgetDetails),
                            new { id = expense.ProjectId });
                    }

                    // Only Pending or On Hold CEO expenses can be reviewed
                    if (expense.CEOStatus != "Pending" &&
                        expense.CEOStatus != "On Hold")
                    {
                        TempData["Error"] =
                            "This expense is not available for CEO review.";

                        return RedirectToAction(
                            nameof(ApprovalBudgetDetails),
                            new { id = expense.ProjectId });
                    }

                    expense.CEOStatus = status;
                    expense.CEORemarks = remarks.Trim();
                    expense.CEOReviewedBy = employee.EmployeeId;
                    expense.CEOReviewedDate = DateTime.Now;

                    TempData["Success"] =
                        $"Expense {status} successfully by CEO.";
                }

                // 10. Save changes
                await _context.SaveChangesAsync();

                // 11. Redirect back to project details
                return RedirectToAction(
                    nameof(ApprovalBudgetDetails),
                    new { id = expense.ProjectId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ApprovalExpenseReview failed.");

                TempData["Error"] =
                    "Unable to update expense: " + ex.GetBaseException().Message;

                return RedirectToAction(nameof(ApprovalBudgetTracking));
            }
        }

    }
}
