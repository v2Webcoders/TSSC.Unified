using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QUIZAPP;
using QUIZAPP.Models;
using TSSC.Unified.Models;
using TSSC.Unified.Services;

namespace TSSC.Unified.Areas.HRMS.Controllers
{
    [Area("HRMS")]
    [Authorize]
    public class AttendanceController : Controller
    {
        private readonly AppdbContext _context;
        private readonly IAttendanceSyncService _syncService;
        private readonly UserManager<AppUser> _userManager;

        public AttendanceController(
            AppdbContext context,
            IAttendanceSyncService syncService, UserManager<AppUser> userManager)
        {
            _context = context;
            _syncService = syncService;
            _userManager = userManager;
        }


        // =========================================================
        // ATTENDANCE LIST
        // =========================================================

        public async Task<IActionResult> Index()
        {
            var attendance =
                await _context.EmployeeAttendance
                    .Include(x => x.Employee)
                    .OrderByDescending(
                        x => x.AttendanceDate)
                    .ThenBy(x => x.EmployeeCode)
                    .ToListAsync();

            return View(attendance);
        }


        // =========================================================
        // MANUAL BIOMETRIC SYNC
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Sync(
            DateTime fromDate,
            DateTime toDate)
        {
            if (fromDate == default)
                fromDate = DateTime.Today;

            if (toDate == default)
                toDate = fromDate;

            if (toDate < fromDate)
            {
                TempData["Error"] =
                    "To Date cannot be earlier than From Date.";

                return RedirectToAction(nameof(Index));
            }

            try
            {
                var result =
                    await _syncService.SyncBiometricAttendanceAsync(fromDate,toDate);

                TempData["Success"] =
                    $"Biometric sync completed successfully. " +
                    $"Records: {result.TotalBiometricRecords}, " +
                    $"Created: {result.AttendanceCreated}, " +
                    $"Updated: {result.AttendanceUpdated}, " +
                    $"Missing Punch: {result.MissingPunch}, " +
                    $"Unmapped: {result.UnmappedEmployees}, " +
                    $"Extra Punches: {result.DuplicatePunches}.";
            }
            catch (Exception ex)
            {
                TempData["Error"] =
                    "Biometric attendance sync failed: " +
                    ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        // MY ATTENDANCE CALENDAR
        // =========================================================

        public async Task<IActionResult> MyAttendance(
            int? year,
            int? month)
        {
            var currentUser =
                await _userManager.GetUserAsync(User);

            if (currentUser == null)
                return Unauthorized();

            // Current month if no month selected
            int selectedYear =
                year ?? DateTime.Today.Year;

            int selectedMonth =
                month ?? DateTime.Today.Month;

            // Get employee
            var employee =
                await _context.Employee
                    .FirstOrDefaultAsync(x =>
                        x.ApplicationUserId ==
                        currentUser.Id);

            if (employee == null)
            {
                TempData["Error"] =
                    "Employee record was not found.";

                return RedirectToAction(
                    "Index",
                    "Home",
                    new { area = "HRMS" });
            }

            // First and last date of selected month
            var startDate =
                new DateTime(
                    selectedYear,
                    selectedMonth,
                    1);

            var endDate =
                startDate.AddMonths(1);

            // Attendance records
            var attendance =
                await _context.EmployeeAttendance
                    .Where(x =>
                        x.EmployeeId ==
                            employee.EmployeeId
                        &&
                        x.AttendanceDate >=
                            startDate
                        &&
                        x.AttendanceDate <
                            endDate)
                    .OrderBy(x => x.AttendanceDate)
                    .ToListAsync();

            // Send to View
            ViewBag.EmployeeName =
                employee.FirstName + " " +
                employee.LastName;

            ViewBag.EmployeeId =
                employee.EmployeeId;

            ViewBag.Year =
                selectedYear;

            ViewBag.Month =
                selectedMonth;

            ViewBag.MonthName =
                startDate.ToString("MMMM yyyy");

            // Summary
            ViewBag.PresentCount =
                attendance.Count(x =>
                    x.AttendanceStatus == "Present");

            ViewBag.MissingPunchCount =
                attendance.Count(x =>
                    x.AttendanceStatus ==
                    "Present - Out Punch Missing");

            ViewBag.AbsentCount = 0;

            ViewBag.LeaveCount = 0;

            return View(attendance);
        }

        // =========================================================
        // EMPLOYEE - REGULARIZATION FORM
        // =========================================================

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Regularization(DateTime? date)
        {
            // =====================================================
            // CURRENT USER
            // =====================================================

            var currentUser =
                await _userManager.GetUserAsync(User);

            if (currentUser == null)
                return Unauthorized();


            // =====================================================
            // FIND EMPLOYEE
            // =====================================================

            var employee =
                await _context.Employee
                    .FirstOrDefaultAsync(x =>
                        x.ApplicationUserId == currentUser.Id);

            if (employee == null)
            {
                TempData["Error"] =
                    "Employee record not found.";

                return RedirectToAction(
                    "Index",
                    "Home",
                    new { area = "HRMS" });
            }


            // =====================================================
            // DATE IS REQUIRED
            // =====================================================

            if (!date.HasValue)
            {
                TempData["Error"] =
                    "Please select an attendance date.";

                return RedirectToAction(
                    nameof(MyAttendance));
            }


            // =====================================================
            // SELECTED DATE
            // =====================================================

            var attendanceDate =
                date.Value.Date;


            // =====================================================
            // FIND EXISTING ATTENDANCE
            // =====================================================

            var attendance =
                await _context.EmployeeAttendance
                    .FirstOrDefaultAsync(x =>
                        x.EmployeeId ==
                            employee.EmployeeId
                        &&
                        x.AttendanceDate ==
                            attendanceDate);


            // =====================================================
            // CHECK EXISTING PENDING REQUEST
            // =====================================================

            bool pendingRequest =
                await _context.AttendanceRegularization
                    .AnyAsync(x =>
                        x.EmployeeId ==
                            employee.EmployeeId
                        &&
                        x.AttendanceDate ==
                            attendanceDate
                        &&
                        x.Status == "Pending");


            if (pendingRequest)
            {
                TempData["Error"] =
                    "A regularization request is already pending for this date.";

                return RedirectToAction(
                    nameof(MyAttendance));
            }


            // =====================================================
            // BUILD REGULARIZATION MODEL
            // =====================================================

            var model =
    new AttendanceRegularization
    {
        EmployeeId =
            employee.EmployeeId,

        AttendanceDate =
            attendanceDate,

        // Existing biometric values
        ExistingInTime =
            attendance?.InTime,

        ExistingOutTime =
            attendance?.OutTime,

        // Requested correction
        // Keep these blank for the employee
        RequestedInTime =
            null,

        RequestedOutTime =
            null,

        Status =
            "Pending"
    };

            // =====================================================
            // INFORMATION FOR VIEW
            // =====================================================

            ViewBag.AttendanceExists =
                attendance != null;

            ViewBag.AttendanceStatus =
                attendance?.AttendanceStatus
                ?? "No biometric attendance found";


            return View(model);
        }


        // =========================================================
        // EMPLOYEE - SUBMIT REGULARIZATION
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Regularization(
        AttendanceRegularization model,
        IFormFile? AttachmentFile)
        {
            // =====================================================
            // CURRENT USER
            // =====================================================

            var currentUser =
                await _userManager.GetUserAsync(User);

            if (currentUser == null)
                return Unauthorized();

            // =====================================================
            // FIND EMPLOYEE
            // =====================================================

            var employee =
                await _context.Employee
                    .FirstOrDefaultAsync(x =>
                        x.ApplicationUserId ==
                        currentUser.Id);

            if (employee == null)
            {
                TempData["Error"] =
                    "Employee record not found.";

                return RedirectToAction(
                    nameof(MyAttendance));
            }

            // =====================================================
            // DATE
            // =====================================================

            model.AttendanceDate =
                model.AttendanceDate.Date;


            // =====================================================
            // FIND EXISTING ATTENDANCE
            // =====================================================

            var attendance =
                await _context.EmployeeAttendance
                    .FirstOrDefaultAsync(x =>
                        x.EmployeeId ==
                            employee.EmployeeId
                        &&
                        x.AttendanceDate ==
                            model.AttendanceDate);


            // =====================================================
            // VALIDATE REQUESTED TIME
            // =====================================================

            if (!model.RequestedInTime.HasValue &&
                !model.RequestedOutTime.HasValue)
            {
                ModelState.AddModelError(
                    "",
                    "Please provide at least one attendance time.");

                return View(model);
            }


            // =====================================================
            // VALIDATE IN / OUT TIME
            // =====================================================

            if (model.RequestedInTime.HasValue &&
                model.RequestedOutTime.HasValue &&
                model.RequestedOutTime <=
                model.RequestedInTime)
            {
                ModelState.AddModelError(
                    "",
                    "Out time must be later than In time.");

                return View(model);
            }


            // =====================================================
            // PREVENT DUPLICATE PENDING REQUEST
            // =====================================================

            bool exists =
                await _context.AttendanceRegularization
                    .AnyAsync(x =>
                        x.EmployeeId ==
                            employee.EmployeeId
                        &&
                        x.AttendanceDate ==
                            model.AttendanceDate
                        &&
                        x.Status == "Pending");

            if (exists)
            {
                ModelState.AddModelError(
                    "",
                    "A pending regularization already exists for this date.");

                return View(model);
            }


            // =====================================================
            // FIND REPORTING MANAGER
            // =====================================================

            if (!employee.ReportingManagerId.HasValue)
            {
                ModelState.AddModelError(
                    "",
                    "No reporting manager is assigned to this employee.");

                return View(model);
            }


            // =====================================================
            // ATTACHMENT
            // =====================================================

            string? fileName = null;

            if (AttachmentFile != null &&
                AttachmentFile.Length > 0)
            {
                var extension =
                    Path.GetExtension(
                        AttachmentFile.FileName);

                var allowedExtensions =
                    new[]
                    {
                ".pdf",
                ".jpg",
                ".jpeg",
                ".png"
                    };


                if (!allowedExtensions
                    .Contains(
                        extension.ToLowerInvariant()))
                {
                    ModelState.AddModelError(
                        "AttachmentFile",
                        "Only PDF, JPG, JPEG and PNG files are allowed.");

                    return View(model);
                }


                // -------------------------------------------------
                // Upload folder
                // -------------------------------------------------

                var uploadPath =
                    Path.Combine(
                        Directory.GetCurrentDirectory(),
                        "wwwroot",
                        "uploads",
                        "attendance");


                if (!Directory.Exists(uploadPath))
                    Directory.CreateDirectory(uploadPath);


                // -------------------------------------------------
                // Generate unique file name
                // -------------------------------------------------

                fileName =
                    Guid.NewGuid().ToString()
                    + extension;


                var filePath =
                    Path.Combine(
                        uploadPath,
                        fileName);


                await using var stream =
                    new FileStream(
                        filePath,
                        FileMode.Create);

                await AttachmentFile.CopyToAsync(stream);
            }


            // =====================================================
            // CREATE REGULARIZATION REQUEST
            // =====================================================

            var request =
                new AttendanceRegularization
                {
                    EmployeeId =
                        employee.EmployeeId,

                    AttendanceDate =
                        model.AttendanceDate,

                    // -------------------------------------------------
                    // If biometric attendance exists, save its values.
                    // If not, these remain NULL.
                    // -------------------------------------------------

                    ExistingInTime =
                        attendance?.InTime,

                    ExistingOutTime =
                        attendance?.OutTime,

                    // -------------------------------------------------
                    // Employee requested values
                    // -------------------------------------------------

                    RequestedInTime =
                        model.RequestedInTime,

                    RequestedOutTime =
                        model.RequestedOutTime,

                    Reason =
                        model.Reason,

                    Attachment =
                        fileName,

                    Status =
                        "Pending",

                    ApproverId =
                        employee.ReportingManagerId,

                    CreatedDate =
                        DateTime.Now
                };


            // =====================================================
            // SAVE REQUEST
            // =====================================================

            _context.AttendanceRegularization
                .Add(request);

            await _context.SaveChangesAsync();


            // =====================================================
            // SUCCESS
            // =====================================================

            TempData["Success"] =
                "Attendance regularization request submitted successfully.";

            return RedirectToAction(
                nameof(MyRegularizations));
        }


        // =========================================================
        // EMPLOYEE - MY REGULARIZATION REQUESTS
        // =========================================================

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> MyRegularizations(string? status)
        {
            var currentUser =
                await _userManager.GetUserAsync(User);

            if (currentUser == null)
                return Unauthorized();


            // =========================================
            // FIND EMPLOYEE
            // =========================================

            var employee =
                await _context.Employee
                    .FirstOrDefaultAsync(x =>
                        x.ApplicationUserId == currentUser.Id);

            if (employee == null)
                return Unauthorized();


            // =========================================
            // DEFAULT STATUS
            // =========================================

            if (string.IsNullOrWhiteSpace(status))
            {
                status = "Pending";
            }


            // =========================================
            // VALID STATUS
            // =========================================

            var allowedStatuses =
                new[] { "Pending", "Approved", "Rejected" };

            if (!allowedStatuses.Contains(status))
            {
                status = "Pending";
            }


            // =========================================
            // GET REQUESTS
            // =========================================

            var requests =
                await _context.AttendanceRegularization
                    .Include(x => x.Approver)
                    .Where(x =>
                        x.EmployeeId == employee.EmployeeId
                        &&
                        x.Status == status)
                    .OrderByDescending(x => x.CreatedDate)
                    .ToListAsync();


            // =========================================
            // COUNTS
            // =========================================

            ViewBag.PendingCount =
                await _context.AttendanceRegularization
                    .CountAsync(x =>
                        x.EmployeeId == employee.EmployeeId
                        &&
                        x.Status == "Pending");

            ViewBag.ApprovedCount =
                await _context.AttendanceRegularization
                    .CountAsync(x =>
                        x.EmployeeId == employee.EmployeeId
                        &&
                        x.Status == "Approved");

            ViewBag.RejectedCount =
                await _context.AttendanceRegularization
                    .CountAsync(x =>
                        x.EmployeeId == employee.EmployeeId
                        &&
                        x.Status == "Rejected");


            // Selected tab
            ViewBag.Status = status;


            return View(requests);
        }


        // =========================================================
        // MANAGER - REGULARIZATION REQUESTS
        // =========================================================

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> RegularizationRequests()
        {
            var currentUser =
                await _userManager.GetUserAsync(User);

            if (currentUser == null)
                return Unauthorized();


            var employee =
                await _context.Employee
                    .FirstOrDefaultAsync(x =>
                        x.ApplicationUserId == currentUser.Id);

            if (employee == null)
                return Unauthorized();


            var requests =
                await _context.AttendanceRegularization
                    .Include(x => x.Employee)
                    .Where(x =>
                        x.ApproverId == employee.EmployeeId
                        )
                    .OrderByDescending(x => x.CreatedDate)
                    .ToListAsync();


            return View(requests);
        }


        // =========================================================
        // MANAGER - APPROVE
        // =========================================================
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> ApproveRegularization(int id)
        {
            var currentUser =
                await _userManager.GetUserAsync(User);

            if (currentUser == null)
                return Unauthorized();


            // =========================================
            // FIND REPORTING MANAGER
            // =========================================

            var manager =
                await _context.Employee
                    .FirstOrDefaultAsync(x =>
                        x.ApplicationUserId == currentUser.Id);

            if (manager == null)
                return Unauthorized();


            // =========================================
            // FIND REQUEST
            // =========================================

            var request =
                await _context.AttendanceRegularization
                    .FirstOrDefaultAsync(x =>
                        x.RegularizationId == id
                        &&
                        x.ApproverId == manager.EmployeeId);

            if (request == null)
            {
                TempData["Error"] =
                    "Regularization request not found or you are not authorized to approve it.";

                return RedirectToAction(
                    nameof(RegularizationRequests));
            }


            // =========================================
            // ONLY PENDING REQUEST CAN BE APPROVED
            // =========================================

            if (request.Status != "Pending")
            {
                TempData["Error"] =
                    "This regularization request has already been processed.";

                return RedirectToAction(
                    nameof(RegularizationRequests));
            }


            // =========================================
            // FIND ATTENDANCE
            // =========================================

            var attendance =
                await _context.EmployeeAttendance
                    .FirstOrDefaultAsync(x =>
                        x.EmployeeId == request.EmployeeId
                        &&
                        x.AttendanceDate ==
                            request.AttendanceDate);

            // =========================================
            // IF NO ATTENDANCE EXISTS
            // CREATE ONE
            // =========================================

            if (attendance == null)
            {
                attendance = new EmployeeAttendance
                {
                    EmployeeId =
                        request.EmployeeId,

                    EmployeeCode =
                        request.Employee?.EmployeeCode ?? "",

                    AttendanceDate =
                        request.AttendanceDate,

                    InTime =
                        request.RequestedInTime,

                    OutTime =
                        request.RequestedOutTime,

                    AttendanceStatus =
                        "Present",

                    Remarks =
                        "Attendance created through approved regularization request.",

                    AttendanceSource =
                        "Regularization",

                    CreatedDate =
                        DateTime.Now
                };

                _context.EmployeeAttendance.Add(attendance);
            }
            else
            {
                // =========================================
                // UPDATE EXISTING ATTENDANCE
                // =========================================

                if (request.RequestedInTime.HasValue)
                {
                    attendance.InTime =
                        request.RequestedInTime;
                }

                if (request.RequestedOutTime.HasValue)
                {
                    attendance.OutTime =
                        request.RequestedOutTime;
                }

                attendance.AttendanceStatus =
                    "Present";

                attendance.AttendanceSource =
                    "Regularization";

                attendance.Remarks =
                    "Attendance updated through approved regularization request.";
            }


            // =========================================
            // APPROVE REQUEST
            // =========================================

            request.Status =
                "Approved";

            request.ApprovedDate =
                DateTime.Now;

            request.ApprovalRemarks =
                "Attendance regularization approved.";


            await _context.SaveChangesAsync();


            TempData["Success"] =
                "Attendance regularization approved successfully.";


            return RedirectToAction(
                nameof(RegularizationRequests));
        }


        // =========================================================
        // MANAGER - REJECT
        // =========================================================

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> RejectRegularization(int id)
        {
            var currentUser =
                await _userManager.GetUserAsync(User);

            if (currentUser == null)
                return Unauthorized();


            var manager =
                await _context.Employee
                    .FirstOrDefaultAsync(x =>
                        x.ApplicationUserId == currentUser.Id);

            if (manager == null)
                return Unauthorized();


            var request =
                await _context.AttendanceRegularization
                    .FirstOrDefaultAsync(x =>
                        x.RegularizationId == id
                        &&
                        x.ApproverId == manager.EmployeeId);

            if (request == null)
            {
                TempData["Error"] =
                    "Regularization request not found.";

                return RedirectToAction(
                    nameof(RegularizationRequests));
            }


            if (request.Status != "Pending")
            {
                TempData["Error"] =
                    "This request has already been processed.";

                return RedirectToAction(
                    nameof(RegularizationRequests));
            }


            request.Status =
                "Rejected";

            request.ApprovedDate =
                DateTime.Now;

            request.ApprovalRemarks =
                "Attendance regularization rejected.";


            await _context.SaveChangesAsync();


            TempData["Success"] =
                "Attendance regularization rejected successfully.";


            return RedirectToAction(
                nameof(RegularizationRequests));
        }

        [Authorize(Roles = "HR")]
        [HttpGet]
        public async Task<IActionResult> AllRegularizations(string? status)
        {
            if (string.IsNullOrWhiteSpace(status))
                status = "Pending";

            var allowedStatuses = new[]
            {
                "Pending",
                "Approved",     
                "Rejected"
            };

            if (!allowedStatuses.Contains(status))
                status = "Pending";

            var requests =
                await _context.AttendanceRegularization
                    .Include(x => x.Employee)
                    .Include(x => x.Approver)
                    .Where(x => x.Status == status)
                    .OrderByDescending(x => x.CreatedDate)
                    .ToListAsync();

            ViewBag.Status = status;

            ViewBag.PendingCount =
                await _context.AttendanceRegularization
                    .CountAsync(x => x.Status == "Pending");

            ViewBag.ApprovedCount =
                await _context.AttendanceRegularization
                    .CountAsync(x => x.Status == "Approved");

            ViewBag.RejectedCount =
                await _context.AttendanceRegularization
                    .CountAsync(x => x.Status == "Rejected");

            return View(requests);
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> ApplyOD(DateTime? date)
        {
            var currentUser =
                await _userManager.GetUserAsync(User);

            if (currentUser == null)
                return Unauthorized();

            var employee =
                await _context.Employee
                    .FirstOrDefaultAsync(x =>
                        x.ApplicationUserId == currentUser.Id);

            if (employee == null)
            {
                TempData["Error"] =
                    "Employee record not found.";

                return RedirectToAction(nameof(MyAttendance));
            }

            // If a date is explicitly supplied, use it.
            // Otherwise leave the date empty.
            DateTime? selectedDate = date?.Date;

            // Only check duplicate when a date was supplied
            if (selectedDate.HasValue)
            {
                var existingOD =
                    await _context.ODRequest
                        .FirstOrDefaultAsync(x =>
                            x.EmployeeId == employee.EmployeeId
                            &&
                            x.ODDate == selectedDate.Value
                            &&
                            (
                                x.Status == "Pending" ||
                                x.Status == "Approved"
                            ));

                if (existingOD != null)
                {
                    TempData["Error"] =
                        "An OD request already exists for this date.";

                    return RedirectToAction(nameof(MyOD));
                }
            }

            var model = new ODRequest
            {
                EmployeeId = employee.EmployeeId,
                ODType = "Full Day",
                Status = "Pending"
            };

            // Only populate date when explicitly passed
            if (selectedDate.HasValue)
            {
                model.ODDate = selectedDate.Value;
            }

            return View(model);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApplyOD(
        ODRequest model,
        IFormFile? AttachmentFile)
        {
            var currentUser =
                await _userManager.GetUserAsync(User);

            if (currentUser == null)
                return Unauthorized();


            // =====================================================
            // FIND EMPLOYEE
            // =====================================================

            var employee =
                await _context.Employee
                    .FirstOrDefaultAsync(x =>
                        x.ApplicationUserId == currentUser.Id);

            if (employee == null)
            {
                TempData["Error"] =
                    "Employee record not found.";

                return RedirectToAction(
                    nameof(MyAttendance));
            }


            // =====================================================
            // VALIDATE OD DATE
            // =====================================================

            if (!model.ODDate.HasValue)
            {
                ModelState.AddModelError(
                    nameof(model.ODDate),
                    "Please select OD date.");

                return View(model);
            }


            // Remove time portion
            model.ODDate =
                model.ODDate.Value.Date;


            // =====================================================
            // VALIDATE PAST DATE
            // =====================================================

            if (model.ODDate.Value < DateTime.Today)
            {
                ModelState.AddModelError(
                    nameof(model.ODDate),
                    "OD date cannot be in the past.");

                return View(model);
            }


            // =====================================================
            // VALIDATE OD TYPE
            // =====================================================

            if (string.IsNullOrWhiteSpace(model.ODType))
            {
                ModelState.AddModelError(
                    nameof(model.ODType),
                    "Please select OD type.");

                return View(model);
            }


            // =====================================================
            // VALIDATE PURPOSE
            // =====================================================

            if (string.IsNullOrWhiteSpace(model.Purpose))
            {
                ModelState.AddModelError(
                    nameof(model.Purpose),
                    "Please enter the purpose of OD.");

                return View(model);
            }


            // =====================================================
            // VALIDATE PARTIAL DAY
            // =====================================================

            if (model.ODType == "Partial Day")
            {
                if (!model.RequestedFromTime.HasValue)
                {
                    ModelState.AddModelError(
                        nameof(model.RequestedFromTime),
                        "Please provide OD From time.");

                    return View(model);
                }


                if (!model.RequestedToTime.HasValue)
                {
                    ModelState.AddModelError(
                        nameof(model.RequestedToTime),
                        "Please provide OD To time.");

                    return View(model);
                }


                if (model.RequestedToTime <=
                    model.RequestedFromTime)
                {
                    ModelState.AddModelError(
                        nameof(model.RequestedToTime),
                        "OD To Time must be later than From Time.");

                    return View(model);
                }


                // Make sure partial-day times belong to OD date
                if (model.RequestedFromTime.Value.Date !=
                    model.ODDate.Value.Date)
                {
                    ModelState.AddModelError(
                        nameof(model.RequestedFromTime),
                        "From Time must be on the selected OD date.");

                    return View(model);
                }


                if (model.RequestedToTime.Value.Date !=
                    model.ODDate.Value.Date)
                {
                    ModelState.AddModelError(
                        nameof(model.RequestedToTime),
                        "To Time must be on the selected OD date.");

                    return View(model);
                }
            }


            // =====================================================
            // CHECK DUPLICATE OD
            // =====================================================

            bool duplicateOD =
                await _context.ODRequest
                    .AnyAsync(x =>
                        x.EmployeeId ==
                            employee.EmployeeId
                        &&
                        x.ODDate ==
                            model.ODDate.Value
                        &&
                        (
                            x.Status == "Pending"
                            ||
                            x.Status == "Approved"
                        ));


            if (duplicateOD)
            {
                ModelState.AddModelError(
                    nameof(model.ODDate),
                    "An OD request already exists for this date.");

                return View(model);
            }


            // =====================================================
            // REPORTING MANAGER
            // =====================================================

            if (!employee.ReportingManagerId.HasValue)
            {
                ModelState.AddModelError(
                    "",
                    "No reporting manager is assigned to this employee.");

                return View(model);
            }


            // =====================================================
            // ATTACHMENT
            // =====================================================

            string? fileName = null;

            if (AttachmentFile != null &&
                AttachmentFile.Length > 0)
            {
                var extension =
                    Path.GetExtension(
                        AttachmentFile.FileName)
                        .ToLowerInvariant();


                var allowedExtensions =
                    new[]
                    {
                ".pdf",
                ".jpg",
                ".jpeg",
                ".png"
                    };


                if (!allowedExtensions.Contains(extension))
                {
                    ModelState.AddModelError(
                        "AttachmentFile",
                        "Only PDF, JPG, JPEG and PNG files are allowed.");

                    return View(model);
                }


                var uploadPath =
                    Path.Combine(
                        Directory.GetCurrentDirectory(),
                        "wwwroot",
                        "uploads",
                        "od");


                if (!Directory.Exists(uploadPath))
                    Directory.CreateDirectory(uploadPath);


                fileName =
                    Guid.NewGuid().ToString()
                    + extension;


                var filePath =
                    Path.Combine(
                        uploadPath,
                        fileName);


                await using var stream =
                    new FileStream(
                        filePath,
                        FileMode.Create);


                await AttachmentFile.CopyToAsync(stream);
            }


            // =====================================================
            // CREATE OD REQUEST
            // =====================================================

            var request =
                new ODRequest
                {
                    EmployeeId =
                        employee.EmployeeId,

                    ODDate =
                        model.ODDate.Value,

                    ODType =
                        model.ODType,

                    RequestedFromTime =
                        model.ODType == "Partial Day"
                            ? model.RequestedFromTime
                            : null,

                    RequestedToTime =
                        model.ODType == "Partial Day"
                            ? model.RequestedToTime
                            : null,

                    Location =
                        model.Location,

                    Purpose =
                        model.Purpose.Trim(),

                    Reason =
                        model.Reason?.Trim(),

                    Attachment =
                        fileName,

                    Status =
                        "Pending",

                    ApproverId =
                        employee.ReportingManagerId,

                    CreatedDate =
                        DateTime.Now
                };


            _context.ODRequest.Add(request);

            await _context.SaveChangesAsync();


            // =====================================================
            // SUCCESS
            // =====================================================

            TempData["Success"] =
                "OD request submitted successfully.";


            return RedirectToAction(
                nameof(MyOD));
        }
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> MyOD()
        {
            var currentUser =
                await _userManager.GetUserAsync(User);

            if (currentUser == null)
                return Unauthorized();


            var employee =
                await _context.Employee
                    .FirstOrDefaultAsync(x =>
                        x.ApplicationUserId == currentUser.Id);

            if (employee == null)
                return Unauthorized();


            var requests =
                await _context.ODRequest
                    .Include(x => x.Approver)
                    .Where(x =>
                        x.EmployeeId ==
                            employee.EmployeeId)
                    .OrderByDescending(
                        x => x.CreatedDate)
                    .ToListAsync();


            return View(requests);
        }


        [Authorize]
        [HttpGet]
        public async Task<IActionResult> ODRequests(
        string status = "Pending")
        {
            var currentUser =
                await _userManager.GetUserAsync(User);

            if (currentUser == null)
                return Unauthorized();


            var employee =
                await _context.Employee
                    .FirstOrDefaultAsync(x =>
                        x.ApplicationUserId == currentUser.Id);

            if (employee == null)
                return Unauthorized();


            var query =
                _context.ODRequest
                    .Include(x => x.Employee)
                    .Include(x => x.Approver)
                    .Where(x =>
                        x.ApproverId ==
                            employee.EmployeeId);


            if (!string.Equals(
                status,
                "All",
                StringComparison.OrdinalIgnoreCase))
            {
                query =
                    query.Where(x =>
                        x.Status == status);
            }


            var requests =
                await query
                    .OrderByDescending(
                        x => x.CreatedDate)
                    .ToListAsync();


            ViewBag.Status = status;

            return View(requests);
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> ApproveOD(int id)
        {
            var currentUser =
                await _userManager.GetUserAsync(User);

            if (currentUser == null)
                return Unauthorized();


            var employee =
                await _context.Employee
                    .FirstOrDefaultAsync(x =>
                        x.ApplicationUserId == currentUser.Id);

            if (employee == null)
                return Unauthorized();


            var request =
                await _context.ODRequest
                    .Include(x => x.Employee)
                    .FirstOrDefaultAsync(x =>
                        x.ODRequestId == id);


            if (request == null)
            {
                TempData["Error"] =
                    "OD request not found.";

                return RedirectToAction(
                    User.IsInRole("HR")
                        ? nameof(AllODRequests)
                        : nameof(ODRequests));
            }


            // =====================================================
            // AUTHORIZATION
            // =====================================================

            bool isHR =
                User.IsInRole("HR");


            bool isApprover =
                request.ApproverId ==
                employee.EmployeeId;


            // HR OR assigned Reporting Manager
            if (!isHR && !isApprover)
            {
                return Forbid();
            }


            // =====================================================
            // ALREADY APPROVED
            // =====================================================

            if (request.Status == "Approved")
            {
                TempData["Error"] =
                    "This OD request is already approved.";

                return RedirectToAction(
                    isHR
                        ? nameof(AllODRequests)
                        : nameof(ODRequests));
            }


            // =====================================================
            // APPROVE
            // =====================================================

            request.Status =
                "Approved";

            request.ApprovedDate =
                DateTime.Now;

            request.ApprovalRemarks =
                isHR
                    ? "OD request approved by HR."
                    : "OD request approved by Reporting Manager.";


            // =====================================================
            // CREATE / UPDATE ATTENDANCE
            // =====================================================

            await CreateODAttendance(request);


            await _context.SaveChangesAsync();


            TempData["Success"] =
                "OD request approved and attendance updated successfully.";


            // =====================================================
            // REDIRECT
            // =====================================================

            return RedirectToAction(
                isHR
                    ? nameof(AllODRequests)
                    : nameof(ODRequests));
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> RejectOD(int id)
        {
            var currentUser =
                await _userManager.GetUserAsync(User);

            if (currentUser == null)
                return Unauthorized();


            // =====================================================
            // FIND CURRENT EMPLOYEE
            // =====================================================

            var employee =
                await _context.Employee
                    .FirstOrDefaultAsync(x =>
                        x.ApplicationUserId == currentUser.Id);

            if (employee == null)
                return Unauthorized();


            // =====================================================
            // FIND OD REQUEST
            // =====================================================

            var request =
                await _context.ODRequest
                    .FirstOrDefaultAsync(x =>
                        x.ODRequestId == id);

            if (request == null)
            {
                TempData["Error"] =
                    "OD request not found.";

                return RedirectToAction(
                    User.IsInRole("HR")
                        ? nameof(AllODRequests)
                        : nameof(ODRequests));
            }


            // =====================================================
            // AUTHORIZATION
            // =====================================================

            bool isHR =
                User.IsInRole("HR");


            bool isApprover =
                request.ApproverId ==
                employee.EmployeeId;


            // HR OR assigned Reporting Manager
            if (!isHR && !isApprover)
            {
                return Forbid();
            }


            // =====================================================
            // ALREADY REJECTED
            // =====================================================

            if (request.Status == "Rejected")
            {
                TempData["Error"] =
                    "This OD request is already rejected.";

                return RedirectToAction(
                    isHR
                        ? nameof(AllODRequests)
                        : nameof(ODRequests));
            }


            // =====================================================
            // REMOVE OD-GENERATED ATTENDANCE
            // ONLY WHEN REJECTING APPROVED OD
            // =====================================================

            if (request.Status == "Approved")
            {
                var startDate =
                    request.ODDate.Value.Date;

                var endDate =
                    startDate.AddDays(1);


                var attendance =
                    await _context.EmployeeAttendance
                        .FirstOrDefaultAsync(x =>
                            x.EmployeeId ==
                                request.EmployeeId
                            &&
                            x.AttendanceDate >=
                                startDate
                            &&
                            x.AttendanceDate <
                                endDate
                            &&
                            x.AttendanceSource ==
                                "OD");


                if (attendance != null)
                {
                    _context.EmployeeAttendance
                        .Remove(attendance);
                }
            }


            // =====================================================
            // UPDATE OD STATUS
            // =====================================================

            request.Status =
                "Rejected";

            request.ApprovedDate =
                DateTime.Now;

            request.ApprovalRemarks =
                isHR
                    ? "OD request rejected by HR."
                    : "OD request rejected by Reporting Manager.";


            await _context.SaveChangesAsync();


            // =====================================================
            // SUCCESS MESSAGE
            // =====================================================

            TempData["Success"] =
                "OD request rejected successfully.";


            // =====================================================
            // REDIRECT
            // =====================================================

            return RedirectToAction(
                isHR
                    ? nameof(AllODRequests)
                    : nameof(ODRequests));
        }

        private async Task CreateODAttendance(
        ODRequest request)
        {
            if (request.Employee == null)
            {
                request.Employee =
                    await _context.Employee
                        .FirstOrDefaultAsync(x =>
                            x.EmployeeId ==
                            request.EmployeeId);
            }


            if (request.Employee == null)
                throw new Exception(
                    "Employee record not found.");


            // =====================================================
            // STANDARD OFFICE TIMING
            // =====================================================

            TimeSpan officeIn =
                new TimeSpan(9, 30, 0);

            TimeSpan officeOut =
                new TimeSpan(18, 0, 0);


            DateTime inTime;

            DateTime outTime;


            // =====================================================
            // FULL DAY OD
            // =====================================================

            if (request.ODType == "Full Day")
            {
                var odDate = request.ODDate.Value.Date;

                inTime = odDate.Add(officeIn);

                outTime = odDate.Add(officeOut);
            }
            else
            {
                // =================================================
                // PARTIAL DAY OD
                // =================================================

                if (!request.RequestedFromTime.HasValue ||
                    !request.RequestedToTime.HasValue)
                {
                    throw new Exception(
                        "Partial day OD requires From and To time.");
                }


                inTime =
                    request.RequestedFromTime.Value;

                outTime =
                    request.RequestedToTime.Value;
            }


            // =====================================================
            // FIND EXISTING ATTENDANCE
            // =====================================================

            var attendance =
                await _context.EmployeeAttendance
                    .FirstOrDefaultAsync(x =>
                        x.EmployeeId ==
                            request.EmployeeId
                        &&
                        x.AttendanceDate ==
                            request.ODDate.Value.Date);


            // =====================================================
            // CREATE
            // =====================================================

            if (attendance == null)
            {
                attendance =
                    new EmployeeAttendance
                    {
                        EmployeeId =
                            request.EmployeeId,

                        EmployeeCode =
                            request.Employee.EmployeeCode,

                        AttendanceDate = request.ODDate.Value.Date,

                        InTime =
                            inTime,

                        OutTime =
                            outTime,

                        AttendanceStatus =
                            "Present",

                        AttendanceSource =
                            "OD",

                        Remarks =
                            "Attendance generated from approved OD request.",

                        CreatedDate =
                            DateTime.Now
                    };


                _context.EmployeeAttendance
                    .Add(attendance);
            }
            else
            {
                // =================================================
                // UPDATE EXISTING ATTENDANCE
                // =================================================

                attendance.InTime =
                    inTime;

                attendance.OutTime =
                    outTime;

                attendance.AttendanceStatus =
                    "On Duty";

                attendance.AttendanceSource =
                    "OD";

                attendance.Remarks =
                    "Attendance updated from approved OD request.";
            }
        }

        [Authorize(Roles = "HR")]
        [HttpGet]
        public async Task<IActionResult> AllODRequests(
        string status = "All")
        {
            var query =
                _context.ODRequest
                    .Include(x => x.Employee)
                    .Include(x => x.Approver)
                    .AsQueryable();


            // =========================================
            // STATUS FILTER
            // =========================================

            if (!string.Equals(
                status,
                "All",
                StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(x =>
                    x.Status == status);
            }


            // =========================================
            // COUNTS
            // =========================================

            ViewBag.AllCount =
                await _context.ODRequest.CountAsync();

            ViewBag.PendingCount =
                await _context.ODRequest
                    .CountAsync(x =>
                        x.Status == "Pending");

            ViewBag.ApprovedCount =
                await _context.ODRequest
                    .CountAsync(x =>
                        x.Status == "Approved");

            ViewBag.RejectedCount =
                await _context.ODRequest
                    .CountAsync(x =>
                        x.Status == "Rejected");


            var requests =
                await query
                    .OrderByDescending(x =>
                        x.CreatedDate)
                    .ToListAsync();


            ViewBag.Status = status;

            return View(requests);
        }
    }
}