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
    public class LeaveRequestController : Controller
    {
        private readonly ILogger<EmployeeController> _logger;
        private readonly AppdbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IWebHostEnvironment _environment;
        public LeaveRequestController(
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

        //public async Task<IActionResult> Create()
        //{
        //    var applicationUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);


        //    var employee = await _context.Employee
        //        .Include(x => x.Department)
        //        .FirstOrDefaultAsync(x => x.ApplicationUserId == applicationUserId);


        //    if (employee == null)
        //    {
        //        return NotFound("Employee record not found."+ applicationUserId.ToString());
        //    }


        //    var model = new LeaveRequestVM
        //    {
        //        EmployeeId = employee.EmployeeId,

        //        EmployeeName = employee.Prefix + " " +
        //                       employee.FirstName + " " +
        //                       employee.LastName,

        //        DepartmentName = employee.Department != null
        //                         ? employee.Department.DepartmentName
        //                         : "",

        //        FromDate = DateTime.Today,

        //        ToDate = DateTime.Today,

        //        Status = "Pending"
        //    };

        //    // Leave Type Dropdown
        //    ViewBag.LeaveTypes = await _context.LeaveType
        //        .Where(x => x.IsActive)
        //        .OrderBy(x => x.LeaveTypeName)
        //        .ToListAsync();
        //    return View(model);
        //}

        public async Task<IActionResult> Create(int? id)
        {
            var applicationUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var employee = await _context.Employee
                .Include(x => x.Department)
                .FirstOrDefaultAsync(x => x.ApplicationUserId == applicationUserId);

            if (employee == null)
            {
                return NotFound("Employee record not found.");
            }

            // Load Leave Types
            ViewBag.LeaveTypes = await _context.LeaveType
                .Where(x => x.IsActive)
                .OrderBy(x => x.LeaveTypeName)
                .ToListAsync();

            LeaveRequestVM model;

            // ==========================
            // ADD NEW
            // ==========================
            if (id == null || id == 0)
            {
                model = new LeaveRequestVM
                {
                    EmployeeId = employee.EmployeeId,
                    EmployeeName = $"{employee.Prefix} {employee.FirstName} {employee.LastName}",
                    DepartmentName = employee.Department?.DepartmentName ?? "",
                    FromDate = DateTime.Today,
                    ToDate = DateTime.Today,
                    Status = "Pending"
                };
            }
            // ==========================
            // EDIT
            // ==========================
            else
            {
                var leave = await _context.LeaveRequest
                    .FirstOrDefaultAsync(x => x.LeaveRequestId == id);

                if (leave == null)
                    return NotFound();

                model = new LeaveRequestVM
                {
                    LeaveRequestId = leave.LeaveRequestId,

                    EmployeeId = employee.EmployeeId,
                    EmployeeName = $"{employee.Prefix} {employee.FirstName} {employee.LastName}",
                    DepartmentName = employee.Department?.DepartmentName ?? "",

                    LeaveTypeId = leave.LeaveTypeId,
                    LeaveDuration = leave.LeaveDuration,
                    FromDate = leave.FromDate,
                    ToDate = leave.ToDate,
                    TotalDays = leave.TotalDays,
                    Reason = leave.Reason,
                    EmergencyContact = leave.EmergencyContact,
                    Attachment = leave.Attachment,
                    Status = leave.Status
                };
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(LeaveRequestVM model)
        {
            await LoadFormData(model);

            // Reload dropdown if validation fails
            ViewBag.LeaveTypes = await _context.LeaveType
                .Where(x => x.IsActive)
                .OrderBy(x => x.LeaveTypeName)
                .ToListAsync();

            if (!ModelState.IsValid)
            {
                await LoadFormData(model);
                return View(model);
            }

            // Validate dates
            if (model.ToDate < model.FromDate)
            {
                ModelState.AddModelError("", "To Date cannot be earlier than From Date.");
                return View(model);
            }

            // Calculate Total Days
            model.TotalDays = (model.ToDate - model.FromDate).Days + 1;

            // Validate Total Days
            if (model.TotalDays <= 0)
            {
                ModelState.AddModelError(nameof(model.TotalDays), "Total days must be greater than 0.");
                return View(model);
            }

            // Upload Attachment
            string fileName = null;

            if (model.AttachmentFile != null && model.AttachmentFile.Length > 0)
            {
                string uploadsFolder = Path.Combine(
                    _environment.WebRootPath,
                    "uploads",
                    "leaves");

                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                fileName = Guid.NewGuid().ToString() +
                           Path.GetExtension(model.AttachmentFile.FileName);

                string filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await model.AttachmentFile.CopyToAsync(stream);
                }
            }
            var applicationUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var employee = await _context.Employee
                .FirstOrDefaultAsync(x => x.ApplicationUserId == applicationUserId);

            if (employee == null)
            {
                return NotFound("Employee not found.");
            }
            if (employee.ReportingManagerId == null)
            {
                ModelState.AddModelError("", "No Reporting Manager has been assigned to your account. Please contact the HR department.");

                await LoadFormData(model);

                return View(model);
            }
            // Save Leave Request
            // Add / Update Leave Request
            LeaveRequest leaveRequest;

            if (model.LeaveRequestId == 0)
            {
                // New Request
                leaveRequest = new LeaveRequest
                {
                    EmployeeId = model.EmployeeId,
                    CreatedDate = DateTime.Now,
                    Status = "Pending",
                    ApproverId = employee.ReportingManagerId
                };

                _context.LeaveRequest.Add(leaveRequest);
            }
            else
            {
                // Edit Existing Request
                leaveRequest = await _context.LeaveRequest
                    .FirstOrDefaultAsync(x => x.LeaveRequestId == model.LeaveRequestId);

                if (leaveRequest == null)
                    return NotFound();

                // Optional: Don't allow editing once approved/rejected
                if (leaveRequest.Status != "Pending")
                {
                    TempData["Error"] = "Only pending leave requests can be edited.";
                    return RedirectToAction(nameof(Index));
                }

                // Keep existing attachment if no new file uploaded
                if (string.IsNullOrEmpty(fileName))
                {
                    fileName = leaveRequest.Attachment;
                }
            }

            // Common fields
            leaveRequest.LeaveTypeId = model.LeaveTypeId;
            leaveRequest.LeaveDuration = model.LeaveDuration;
            leaveRequest.FromDate = model.FromDate;
            leaveRequest.ToDate = model.ToDate;
            leaveRequest.TotalDays = model.TotalDays;
            leaveRequest.Reason = model.Reason;
            leaveRequest.EmergencyContact = model.EmergencyContact;
            leaveRequest.Attachment = fileName;

            await _context.SaveChangesAsync();

            TempData["Success"] = model.LeaveRequestId == 0
                ? "Leave request submitted successfully."
                : "Leave request updated successfully.";

            return RedirectToAction(nameof(MyLeaves));
        }
        private async Task LoadFormData(LeaveRequestVM model)
        {
            ViewBag.LeaveTypes = await _context.LeaveType
                .Where(x => x.IsActive)
                .OrderBy(x => x.LeaveTypeName)
                .ToListAsync();

            var applicationUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var employee = await _context.Employee
                .Include(x => x.Department)
                .FirstOrDefaultAsync(x => x.ApplicationUserId == applicationUserId);

            if (employee != null)
            {
                model.EmployeeId = employee.EmployeeId;
                model.EmployeeName = employee.FirstName+" "+employee.LastName;
                model.DepartmentName = employee.Department?.DepartmentName;
            }
        }
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
            var leaveRequests = await _context.LeaveRequest
                .Include(x => x.LeaveType)
                .Where(x => x.EmployeeId == employee.EmployeeId
                         || x.ApproverId == employee.EmployeeId)
                .OrderByDescending(x => x.CreatedDate)
                .ToListAsync();

            return View(leaveRequests);
        }

        //public async Task<IActionResult> Approve(int id)
        //{
        //    var applicationUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        //    var employee = await _context.Employee
        //        .FirstOrDefaultAsync(x => x.ApplicationUserId == applicationUserId);

        //    var leave = await _context.LeaveRequest
        //        .FirstOrDefaultAsync(x => x.LeaveRequestId == id
        //                              && x.ApproverId == employee.EmployeeId);

        //    if (leave == null)
        //        return NotFound();

        //    leave.Status = "Approved";
        //    leave.ApprovedBy = employee.EmployeeId;
        //    leave.ApprovedDate = DateTime.Now;

        //    await _context.SaveChangesAsync();

        //    TempData["Success"] = "Leave request approved successfully.";

        //    return RedirectToAction(nameof(MyLeaves));
        //}

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            var applicationUserId =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            var employee = await _context.Employee
                .FirstOrDefaultAsync(x =>
                    x.ApplicationUserId == applicationUserId);

            if (employee == null)
                return NotFound("Approver employee record not found.");

            var leave = await _context.LeaveRequest
                .FirstOrDefaultAsync(x =>
                    x.LeaveRequestId == id &&
                    x.ApproverId == employee.EmployeeId);

            if (leave == null)
                return NotFound();

            // Prevent duplicate deduction
            if (leave.Status == "Approved")
            {
                TempData["Error"] = "This leave request is already approved.";
                return RedirectToAction(nameof(MyLeaves));
            }

            // Get employee leave balance
            var balance = await _context.EmployeeLeaveBalance
                .FirstOrDefaultAsync(x =>
                    x.EmployeeId == leave.EmployeeId &&
                    x.LeaveTypeId == leave.LeaveTypeId);

            if (balance == null)
            {
                TempData["Error"] =
                    "Leave balance record not found for this employee.";

                return RedirectToAction(nameof(MyLeaves));
            }

            // Check available balance
            if (balance.CurrentBalance < leave.TotalDays)
            {
                TempData["Error"] =
                    $"Insufficient leave balance. Available balance: {balance.CurrentBalance} days.";

                return RedirectToAction(nameof(MyLeaves));
            }

            // Deduct leave
            balance.UsedLeaves += leave.TotalDays;
            balance.LastUpdated = DateTime.Now;

            // Approve leave
            leave.Status = "Approved";
            leave.ApprovedBy = employee.EmployeeId;
            leave.ApprovedDate = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Leave request approved and leave balance updated successfully.";

            return RedirectToAction(nameof(MyLeaves));
        }

        //public async Task<IActionResult> Reject(int id)
        //{
        //    var applicationUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        //    var employee = await _context.Employee
        //        .FirstOrDefaultAsync(x => x.ApplicationUserId == applicationUserId);

        //    var leave = await _context.LeaveRequest
        //        .FirstOrDefaultAsync(x => x.LeaveRequestId == id
        //                              && x.ApproverId == employee.EmployeeId);

        //    if (leave == null)
        //        return NotFound();

        //    leave.Status = "Rejected";
        //    leave.ApprovedBy = employee.EmployeeId;
        //    leave.ApprovedDate = DateTime.Now;

        //    await _context.SaveChangesAsync();

        //    TempData["Success"] = "Leave request rejected successfully.";

        //    return RedirectToAction(nameof(MyLeaves));
        //}

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id)
        {
            var applicationUserId =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            var employee = await _context.Employee
                .FirstOrDefaultAsync(x =>
                    x.ApplicationUserId == applicationUserId);

            if (employee == null)
                return NotFound("Approver employee record not found.");

            var leave = await _context.LeaveRequest
                .FirstOrDefaultAsync(x =>
                    x.LeaveRequestId == id &&
                    x.ApproverId == employee.EmployeeId);

            if (leave == null)
                return NotFound();

            // Already rejected
            if (leave.Status == "Rejected")
            {
                TempData["Error"] = "This leave request is already rejected.";
                return RedirectToAction(nameof(MyLeaves));
            }

            // If an already-approved leave is being changed to rejected,
            // restore the previously deducted balance.
            if (leave.Status == "Approved")
            {
                var balance = await _context.EmployeeLeaveBalance
                    .FirstOrDefaultAsync(x =>
                        x.EmployeeId == leave.EmployeeId &&
                        x.LeaveTypeId == leave.LeaveTypeId);

                if (balance == null)
                {
                    TempData["Error"] =
                        "Leave balance record not found.";

                    return RedirectToAction(nameof(MyLeaves));
                }

                balance.UsedLeaves -= leave.TotalDays;

                if (balance.UsedLeaves < 0)
                    balance.UsedLeaves = 0;

                balance.LastUpdated = DateTime.Now;
            }

            leave.Status = "Rejected";
            leave.ApprovedBy = employee.EmployeeId;
            leave.ApprovedDate = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Leave request rejected successfully.";

            return RedirectToAction(nameof(MyLeaves));
        }
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var applicationUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var employee = await _context.Employee
                .FirstOrDefaultAsync(x => x.ApplicationUserId == applicationUserId);

            if (employee == null)
            {
                return NotFound("Employee not found.");
            }

            var leaveRequest = await _context.LeaveRequest
                .FirstOrDefaultAsync(x => x.LeaveRequestId == id &&
                                          x.EmployeeId == employee.EmployeeId);

            if (leaveRequest == null)
            {
                return NotFound();
            }

            // Don't allow deleting approved/rejected requests
            if (leaveRequest.Status != "Pending")
            {
                TempData["Error"] = "Only pending leave requests can be deleted.";
                return RedirectToAction(nameof(MyLeaves));
            }

            // Delete attachment if exists
            if (!string.IsNullOrEmpty(leaveRequest.Attachment))
            {
                var filePath = Path.Combine(
                    _environment.WebRootPath,
                    "uploads",
                    "leaves",
                    leaveRequest.Attachment);

                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }
            }

            _context.LeaveRequest.Remove(leaveRequest);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Leave request deleted successfully.";

            return RedirectToAction(nameof(MyLeaves));
        }
    }
}
