using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QUIZAPP;
using QUIZAPP.Models;
using TSSC.Unified.Models;
using TSSC.Unified.Services;
using TSSC.Unified.ViewModel;

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
                    await _syncService.SyncBiometricAttendanceAsync(fromDate, toDate);

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

        //public async Task<IActionResult> MyAttendance(
        //    int? year,
        //    int? month)
        //{
        //    var currentUser =
        //        await _userManager.GetUserAsync(User);

        //    if (currentUser == null)
        //        return Unauthorized();

        //    // Current month if no month selected
        //    int selectedYear =
        //        year ?? DateTime.Today.Year;

        //    int selectedMonth =
        //        month ?? DateTime.Today.Month;

        //    // Get employee
        //    var employee =
        //        await _context.Employee
        //            .FirstOrDefaultAsync(x =>
        //                x.ApplicationUserId ==
        //                currentUser.Id);

        //    if (employee == null)
        //    {
        //        TempData["Error"] =
        //            "Employee record was not found.";

        //        return RedirectToAction(
        //            "Index",
        //            "Home",
        //            new { area = "HRMS" });
        //    }

        //    // First and last date of selected month
        //    var startDate =
        //        new DateTime(
        //            selectedYear,
        //            selectedMonth,
        //            1);

        //    var endDate =
        //        startDate.AddMonths(1);

        //    // Attendance records
        //    var attendance =
        //        await _context.EmployeeAttendance
        //            .Where(x =>
        //                x.EmployeeId ==
        //                    employee.EmployeeId
        //                &&
        //                x.AttendanceDate >=
        //                    startDate
        //                &&
        //                x.AttendanceDate <
        //                    endDate)
        //            .OrderBy(x => x.AttendanceDate)
        //            .ToListAsync();

        //    // Send to View
        //    ViewBag.EmployeeName =
        //        employee.FirstName + " " +
        //        employee.LastName;

        //    ViewBag.EmployeeId =
        //        employee.EmployeeId;

        //    ViewBag.Year =
        //        selectedYear;

        //    ViewBag.Month =
        //        selectedMonth;

        //    ViewBag.MonthName =
        //        startDate.ToString("MMMM yyyy");

        //    // Employees who have biometric attendance today
        //    var attendanceEmployeeIds =
        //    attendance
        //    .Select(x => x.EmployeeId)
        //    .Distinct()
        //    .ToHashSet();

        //    // Present
        //    ViewBag.PresentCount =
        //        attendance.Count(x =>
        //            x.AttendanceStatus == "Present" || x.AttendanceStatus == "Late");

        //    int lateCount = attendance.Count(x =>
        //    x.AttendanceStatus == "Late");

        //    int lateLeaveDeduction = lateCount / 3;

        //    ViewBag.LateCount = lateCount;
        //    ViewBag.LateLeaveDeduction = lateLeaveDeduction;

        //    // Half Day
        //    ViewBag.HalfDayCount =
        //        attendance.Count(x =>
        //            x.AttendanceStatus == "Half Day");

        //    // Missing Punch
        //    ViewBag.MissingPunchCount =
        //        attendance.Count(x =>
        //            x.AttendanceStatus.Contains("Check Out Missing"));

        //    // Leave
        //    ViewBag.LeaveCount =
        //    await _context.LeaveRequest
        //    .CountAsync(x =>
        //    x.EmployeeId != null &&
        //    x.Status == "Approved" &&
        //    x.FromDate < endDate &&
        //    x.ToDate >= startDate);

        //    var activeEmployees = await _context.Employee
        //    .Where(x => x.IsActive == true && x.ProfileStatus=="Active")
        //    .ToListAsync();

        //    // Absent
        //    ViewBag.AbsentCount =
        //        activeEmployees.Count(x =>
        //            !attendanceEmployeeIds.Contains(x.EmployeeId));
        //    var leaveList = await _context.LeaveRequest
        //    .Where(x =>
        //        x.EmployeeId == employee.EmployeeId &&
        //        x.Status == "Approved" &&
        //        x.FromDate < endDate &&
        //        x.ToDate >= startDate)
        //    .ToListAsync();

        //    ViewBag.LeaveList = leaveList;
        //    return View(attendance);
        //}


        public async Task<IActionResult> MyAttendance(
        int? year,
        int? month, string? mode)
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

            // Leave records for the month
            var leaveList = await _context.LeaveRequest
                .Where(x =>
                    x.EmployeeId == employee.EmployeeId &&
                    x.Status == "Approved" &&
                    x.FromDate < endDate &&
                    x.ToDate >= startDate)
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

            // Page title based on query string
            ViewBag.PageTitle =
                string.Equals(mode, "regularization",
                    StringComparison.OrdinalIgnoreCase)
                    ? "Attendance Regularization"
                    : "View Attendance";

            // Present (including Late)
            ViewBag.PresentCount =
                attendance.Count(x =>
                    x.AttendanceStatus == "Present" ||
                    x.AttendanceStatus == "Late");

            // Late
            int lateCount = attendance.Count(x =>
                x.AttendanceStatus == "Late");

            int lateLeaveDeduction = lateCount / 3;

            ViewBag.LateCount = lateCount;
            ViewBag.LateLeaveDeduction = lateLeaveDeduction;

            // Half Day
            ViewBag.HalfDayCount =
                attendance.Count(x =>
                    x.AttendanceStatus == "Half Day");

            // Missing Punch
            ViewBag.MissingPunchCount =
                attendance.Count(x =>
                    x.AttendanceStatus.Contains("Check Out Missing"));

            // Leave
            ViewBag.LeaveCount = leaveList.Count();

            ViewBag.LeaveList = leaveList;

            // Absent Count - CORRECTED
            // Count only from 1st to today (if current month) or entire month (if past month)
            // Since EmployeeAttendance only has punched-in records

            int daysInMonth = DateTime.DaysInMonth(selectedYear, selectedMonth);

            // If viewing current month, count until today; if past month, count entire month
            DateTime countUntilDate = DateTime.Today;

            // If selected month is in future or today is before end of selected month
            if (DateTime.Today.Year > selectedYear ||
                (DateTime.Today.Year == selectedYear && DateTime.Today.Month > selectedMonth))
            {
                // Past month - count entire month
                countUntilDate = new DateTime(selectedYear, selectedMonth, daysInMonth);
            }

            // Count only weekdays (Mon-Fri) from 1st of month until countUntilDate
            var weekdaysInRange = Enumerable.Range(0, (countUntilDate - startDate).Days + 1)
                .Select(day => startDate.AddDays(day))
                .Count(date => 
                               date.DayOfWeek != DayOfWeek.Sunday);

            // Days with attendance records
            int attendanceRecordDays = attendance.Count();

            // Days on approved leave
            int leaveDays = leaveList.Count();

            // Absent = Weekdays in range - (Days with attendance) - (Days on leave)
            int absentDays = weekdaysInRange - attendanceRecordDays - leaveDays;

            ViewBag.AbsentCount = Math.Max(0, absentDays); // Prevent negative values
                                                           // Holiday records for the month
            var holidayList = await _context.Holiday
                .Where(x =>
                    x.HolidayDate >= startDate &&
                    x.HolidayDate < endDate)
                .ToListAsync();

            ViewBag.HolidayList = holidayList;

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

        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> Regularization(
        //AttendanceRegularization model,
        //IFormFile? AttachmentFile)
        //{
        //    // =====================================================
        //    // CURRENT USER
        //    // =====================================================

        //    var currentUser =
        //        await _userManager.GetUserAsync(User);

        //    if (currentUser == null)
        //        return Unauthorized();

        //    // =====================================================
        //    // FIND EMPLOYEE
        //    // =====================================================

        //    var employee =
        //        await _context.Employee
        //            .FirstOrDefaultAsync(x =>
        //                x.ApplicationUserId ==
        //                currentUser.Id);

        //    if (employee == null)
        //    {
        //        TempData["Error"] =
        //            "Employee record not found.";

        //        return RedirectToAction(
        //            nameof(MyAttendance));
        //    }

        //    // =====================================================
        //    // DATE
        //    // =====================================================

        //    model.AttendanceDate =
        //        model.AttendanceDate.Date;


        //    // =====================================================
        //    // FIND EXISTING ATTENDANCE
        //    // =====================================================

        //    var attendance =
        //        await _context.EmployeeAttendance
        //            .FirstOrDefaultAsync(x =>
        //                x.EmployeeId ==
        //                    employee.EmployeeId
        //                &&
        //                x.AttendanceDate ==
        //                    model.AttendanceDate);


        //    // =====================================================
        //    // VALIDATE REQUESTED TIME
        //    // =====================================================

        //    if (!model.RequestedInTime.HasValue &&
        //        !model.RequestedOutTime.HasValue)
        //    {
        //        ModelState.AddModelError(
        //            "",
        //            "Please provide at least one attendance time.");

        //        return View(model);
        //    }


        //    // =====================================================
        //    // VALIDATE IN / OUT TIME
        //    // =====================================================

        //    if (model.RequestedInTime.HasValue &&
        //        model.RequestedOutTime.HasValue &&
        //        model.RequestedOutTime <=
        //        model.RequestedInTime)
        //    {
        //        ModelState.AddModelError(
        //            "",
        //            "Out time must be later than In time.");

        //        return View(model);
        //    }


        //    // =====================================================
        //    // PREVENT DUPLICATE PENDING REQUEST
        //    // =====================================================

        //    bool exists =
        //        await _context.AttendanceRegularization
        //            .AnyAsync(x =>
        //                x.EmployeeId ==
        //                    employee.EmployeeId
        //                &&
        //                x.AttendanceDate ==
        //                    model.AttendanceDate
        //                &&
        //                x.Status == "Pending");

        //    if (exists)
        //    {
        //        ModelState.AddModelError(
        //            "",
        //            "A pending regularization already exists for this date.");

        //        return View(model);
        //    }


        //    // =====================================================
        //    // FIND REPORTING MANAGER
        //    // =====================================================

        //    if (!employee.ReportingManagerId.HasValue)
        //    {
        //        ModelState.AddModelError(
        //            "",
        //            "No reporting manager is assigned to this employee.");

        //        return View(model);
        //    }


        //    // =====================================================
        //    // ATTACHMENT
        //    // =====================================================

        //    string? fileName = null;

        //    if (AttachmentFile != null &&
        //        AttachmentFile.Length > 0)
        //    {
        //        var extension =
        //            Path.GetExtension(
        //                AttachmentFile.FileName);

        //        var allowedExtensions =
        //            new[]
        //            {
        //        ".pdf",
        //        ".jpg",
        //        ".jpeg",
        //        ".png"
        //            };


        //        if (!allowedExtensions
        //            .Contains(
        //                extension.ToLowerInvariant()))
        //        {
        //            ModelState.AddModelError(
        //                "AttachmentFile",
        //                "Only PDF, JPG, JPEG and PNG files are allowed.");

        //            return View(model);
        //        }


        //        // -------------------------------------------------
        //        // Upload folder
        //        // -------------------------------------------------

        //        var uploadPath =
        //            Path.Combine(
        //                Directory.GetCurrentDirectory(),
        //                "wwwroot",
        //                "uploads",
        //                "attendance");


        //        if (!Directory.Exists(uploadPath))
        //            Directory.CreateDirectory(uploadPath);


        //        // -------------------------------------------------
        //        // Generate unique file name
        //        // -------------------------------------------------

        //        fileName =
        //            Guid.NewGuid().ToString()
        //            + extension;


        //        var filePath =
        //            Path.Combine(
        //                uploadPath,
        //                fileName);


        //        await using var stream =
        //            new FileStream(
        //                filePath,
        //                FileMode.Create);

        //        await AttachmentFile.CopyToAsync(stream);
        //    }


        //    // =====================================================
        //    // CREATE REGULARIZATION REQUEST
        //    // =====================================================

        //    var request =
        //        new AttendanceRegularization
        //        {
        //            EmployeeId =
        //                employee.EmployeeId,

        //            AttendanceDate =
        //                model.AttendanceDate,

        //            // -------------------------------------------------
        //            // If biometric attendance exists, save its values.
        //            // If not, these remain NULL.
        //            // -------------------------------------------------

        //            ExistingInTime =
        //                attendance?.InTime,

        //            ExistingOutTime =
        //                attendance?.OutTime,

        //            // -------------------------------------------------
        //            // Employee requested values
        //            // -------------------------------------------------

        //            RequestedInTime =
        //                model.RequestedInTime,

        //            RequestedOutTime =
        //                model.RequestedOutTime,

        //            Reason =
        //                model.Reason,

        //            Attachment =
        //                fileName,

        //            Status =
        //                "Pending",

        //            ApproverId =
        //                employee.ReportingManagerId,

        //            CreatedDate =
        //                DateTime.Now
        //        };


        //    // =====================================================
        //    // SAVE REQUEST
        //    // =====================================================

        //    _context.AttendanceRegularization
        //        .Add(request);

        //    await _context.SaveChangesAsync();


        //    // =====================================================
        //    // SUCCESS
        //    // =====================================================

        //    TempData["Success"] =
        //        "Attendance regularization request submitted successfully.";

        //    return RedirectToAction(
        //        nameof(MyRegularizations));
        //}


        // =========================================================
        // EMPLOYEE - MY REGULARIZATION REQUESTS
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
                        x.ApplicationUserId == currentUser.Id);

            if (employee == null)
            {
                TempData["Error"] =
                    "Employee record not found.";

                return RedirectToAction(nameof(MyAttendance));
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
                        x.EmployeeId == employee.EmployeeId &&
                        x.AttendanceDate == model.AttendanceDate);


            // =====================================================
            // VALIDATE REGULARIZATION TYPE
            // =====================================================

            var validTypes = new[]
            {
        "Both",
        "In",
        "Out"
    };

            if (string.IsNullOrWhiteSpace(model.RegularizationType) ||
                !validTypes.Contains(model.RegularizationType))
            {
                ModelState.AddModelError(
                    "RegularizationType",
                    "Please select a valid regularization type.");

                return View(model);
            }


            // =====================================================
            // VALIDATE REQUESTED TIME BASED ON TYPE
            // =====================================================

            if (model.RegularizationType == "Both")
            {
                if (!model.RequestedInTime.HasValue)
                {
                    ModelState.AddModelError(
                        "RequestedInTime",
                        "Requested In Time is required.");
                }

                if (!model.RequestedOutTime.HasValue)
                {
                    ModelState.AddModelError(
                        "RequestedOutTime",
                        "Requested Out Time is required.");
                }
            }
            else if (model.RegularizationType == "In")
            {
                if (!model.RequestedInTime.HasValue)
                {
                    ModelState.AddModelError(
                        "RequestedInTime",
                        "Requested In Time is required.");
                }

                // Don't save an Out value for In-only request
                model.RequestedOutTime = null;
            }
            else if (model.RegularizationType == "Out")
            {
                if (!model.RequestedOutTime.HasValue)
                {
                    ModelState.AddModelError(
                        "RequestedOutTime",
                        "Requested Out Time is required.");
                }

                // Don't save an In value for Out-only request
                model.RequestedInTime = null;
            }


            // =====================================================
            // VALIDATE IN / OUT TIME
            // =====================================================

            if (model.RequestedInTime.HasValue &&
                model.RequestedOutTime.HasValue &&
                model.RequestedOutTime <= model.RequestedInTime)
            {
                ModelState.AddModelError(
                    "",
                    "Out time must be later than In time.");
            }


            // =====================================================
            // STOP IF VALIDATION FAILED
            // =====================================================

            if (!ModelState.IsValid)
            {
                // Restore existing values for display
                model.ExistingInTime =
                    attendance?.InTime;

                model.ExistingOutTime =
                    attendance?.OutTime;

                return View(model);
            }


            // =====================================================
            // PREVENT DUPLICATE PENDING REQUEST
            // =====================================================

            bool exists =
                await _context.AttendanceRegularization
                    .AnyAsync(x =>
                        x.EmployeeId == employee.EmployeeId &&
                        x.AttendanceDate == model.AttendanceDate &&
                        x.Status == "Pending");

            if (exists)
            {
                ModelState.AddModelError(
                    "",
                    "A pending regularization already exists for this date.");

                model.ExistingInTime = attendance?.InTime;
                model.ExistingOutTime = attendance?.OutTime;

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

                model.ExistingInTime = attendance?.InTime;
                model.ExistingOutTime = attendance?.OutTime;

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
                    .Contains(extension.ToLowerInvariant()))
                {
                    ModelState.AddModelError(
                        "AttachmentFile",
                        "Only PDF, JPG, JPEG and PNG files are allowed.");

                    model.ExistingInTime = attendance?.InTime;
                    model.ExistingOutTime = attendance?.OutTime;

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
                    Guid.NewGuid().ToString() +
                    extension;


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
                    // Existing attendance from DATABASE
                    // -------------------------------------------------

                    ExistingInTime =
                        attendance?.InTime,

                    ExistingOutTime =
                        attendance?.OutTime,

                    // -------------------------------------------------
                    // Regularization type
                    // -------------------------------------------------

                    RegularizationType =
                        model.RegularizationType,

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

            //if (manager == null)
            //    return Unauthorized();


            // =========================================
            // FIND REQUEST
            // =========================================

            var request =
                await _context.AttendanceRegularization
                    .FirstOrDefaultAsync(x =>
                        x.RegularizationId == id);

            if (request == null)
            {
                TempData["Error"] =
                    "Regularization request not found or you are not authorized to approve it.";

                return RedirectToAction(
                    nameof(AllRegularizations));
            }


            // =========================================
            // ONLY PENDING REQUEST CAN BE APPROVED
            // =========================================

            if (request.Status != "Pending")
            {
                TempData["Error"] =
                    "This regularization request has already been processed.";

                return RedirectToAction(
                    nameof(AllRegularizations));
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
                nameof(AllRegularizations));
        }


        // =========================================================
        // MANAGER - REJECT
        // =========================================================

        
        [HttpGet]
        public async Task<IActionResult> RejectRegularization(int id)
        {
            var currentUser =
                await _userManager.GetUserAsync(User);

            if (currentUser == null)
                return Unauthorized();


            //var manager =
            //    await _context.Employee
            //        .FirstOrDefaultAsync(x =>
            //            x.ApplicationUserId == currentUser.Id);

            //if (manager == null)
            //    return Unauthorized();


            var request =
                await _context.AttendanceRegularization
                    .FirstOrDefaultAsync(x =>
                        x.RegularizationId == id
                        );

            if (request == null)
            {
                TempData["Error"] =
                    "Regularization request not found.";

                return RedirectToAction(
                    nameof(AllRegularizations));
            }


            if (request.Status != "Pending")
            {
                TempData["Error"] =
                    "This request has already been processed.";

                return RedirectToAction(
                    nameof(AllRegularizations));
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
                nameof(AllRegularizations));
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
        //[HttpGet]
        //public async Task<IActionResult> ViewAttendance(
        //int? employeeId,
        //int? month,
        //int? year)
        //{
        //    try
        //    {
        //        // =====================================================
        //        // SELECTED MONTH / YEAR
        //        // =====================================================

        //        var selectedYear =
        //            year ?? DateTime.Today.Year;

        //        var selectedMonth =
        //            month ?? DateTime.Today.Month;

        //        var selectedEmployeeId =
        //            employeeId ?? 0;


        //        // =====================================================
        //        // EMPLOYEE LIST
        //        // =====================================================

        //        var employees = await _context.Employee
        //            .Where(x => x.IsActive)
        //            .OrderBy(x => x.FirstName)
        //            .ThenBy(x => x.LastName)
        //            .ToListAsync();


        //        // =====================================================
        //        // SELECTED EMPLOYEE
        //        // =====================================================

        //        Employee? selectedEmployee = null;

        //        if (selectedEmployeeId > 0)
        //        {
        //            selectedEmployee = employees
        //                .FirstOrDefault(x =>
        //                    x.EmployeeId == selectedEmployeeId);
        //        }


        //        // =====================================================
        //        // MONTH RANGE
        //        // =====================================================

        //        var firstDay =
        //            new DateTime(
        //                selectedYear,
        //                selectedMonth,
        //                1);

        //        var lastDay =
        //            firstDay.AddMonths(1).AddDays(-1);


        //        // =====================================================
        //        // ATTENDANCE DATA
        //        // =====================================================

        //        var attendance = new List<EmployeeAttendance>();

        //        if (selectedEmployeeId>0)
        //        {
        //            attendance = await _context.EmployeeAttendance
        //                .Where(x =>
        //                    x.AttendanceDate >= firstDay &&
        //                    x.AttendanceDate <= lastDay &&
        //                    x.EmployeeId == selectedEmployeeId)
        //                .OrderBy(x => x.AttendanceDate)
        //                .ToListAsync();
        //        }


        //        // =====================================================
        //        // CREATE VIEW MODEL
        //        // =====================================================

        //        var model = new HRAttendanceCalendarVM
        //        {
        //            EmployeeId =
        //                selectedEmployeeId > 0
        //                    ? selectedEmployeeId
        //                    : null,

        //            EmployeeName =
        //                selectedEmployee != null
        //                    ? $"{selectedEmployee.FirstName} {selectedEmployee.LastName}".Trim()
        //                    : null,

        //            EmployeeCode =
        //                selectedEmployee?.EmployeeCode,

        //            Year = selectedYear,

        //            Month = selectedMonth
        //        };


        //        // =====================================================
        //        // CREATE CALENDAR DAYS
        //        // =====================================================
        //        if(selectedEmployeeId>0)
        //        {
        //            for (
        //            var date = firstDay;
        //            date <= lastDay;
        //            date = date.AddDays(1))
        //            {
        //                var recordsForDay =
        //                    attendance
        //                        .Where(x =>
        //                            x.AttendanceDate.Date ==
        //                            date.Date)
        //                        .ToList();


        //                // -------------------------------------------------
        //                // If employee selected, there should normally be
        //                // only one attendance record.
        //                // -------------------------------------------------

        //                var record =
        //                    recordsForDay.FirstOrDefault();

        //                var day =
        //                    new AttendanceCalendarDayVM
        //                    {
        //                        Date = date,
        //                        IsCurrentMonth = true
        //                    };

        //                if (record != null)
        //                {
        //                    day.Status =
        //                        record.AttendanceStatus;

        //                    day.InTime =
        //                        record.InTime;

        //                    day.OutTime =
        //                        record.OutTime;


        //                    // ---------------------------------------------
        //                    // WORKING HOURS
        //                    // ---------------------------------------------

        //                    if (
        //                        record.InTime.HasValue &&
        //                        record.OutTime.HasValue)
        //                    {
        //                        day.WorkingHours =
        //                            (
        //                                record.OutTime.Value -
        //                                record.InTime.Value
        //                            ).TotalHours;
        //                    }
        //                    day.AttendanceSource = record.AttendanceSource;
        //                    day.CheckInLocation = record.CheckInLocation;
        //                }
        //                else
        //                {
        //                    // ---------------------------------------------
        //                    // NO ATTENDANCE RECORD
        //                    // ---------------------------------------------

        //                    if (date.DayOfWeek ==
        //                        DayOfWeek.Sunday)
        //                    {
        //                        day.Status = "Sunday";
        //                    }
        //                    else if (date.Date <= DateTime.Today)
        //                    {
        //                        day.Status = "Absent";
        //                    }
        //                }
        //                model.Days.Add(day);
        //            }
        //        }

        //        // =====================================================
        //        // DROPDOWNS
        //        // =====================================================

        //        ViewBag.EmployeeList = employees;

        //        ViewBag.SelectedEmployeeId =
        //            selectedEmployeeId;

        //        ViewBag.SelectedMonth =
        //            selectedMonth;

        //        ViewBag.SelectedYear =
        //            selectedYear;


        //        ViewBag.YearList =
        //            Enumerable
        //                .Range(
        //                    DateTime.Today.Year - 2,
        //                    5)
        //                .OrderByDescending(x => x)
        //                .ToList();


        //        return View(model);
        //    }
        //    catch (Exception ex)
        //    {
        //        // Log exception here

        //        TempData["Error"] =
        //            "Unable to load attendance.";

        //        return RedirectToAction("Index", "Home");
        //    }
        //}

        [HttpGet]
        public async Task<IActionResult> ViewAttendance(
        int? employeeId,
        int? month,
        int? year)
        {
            try
            {
                // =====================================================
                // SELECTED MONTH / YEAR
                // =====================================================

                var selectedYear = year ?? DateTime.Today.Year;
                var selectedMonth = month ?? DateTime.Today.Month;
                var selectedEmployeeId = employeeId ?? 0;


                // =====================================================
                // EMPLOYEE LIST
                // =====================================================

                var employees = await _context.Employee
                    .Where(x => x.IsActive)
                    .OrderBy(x => x.FirstName)
                    .ThenBy(x => x.LastName)
                    .ToListAsync();


                // =====================================================
                // SELECTED EMPLOYEE
                // =====================================================

                Employee? selectedEmployee = null;

                if (selectedEmployeeId > 0)
                {
                    selectedEmployee = employees
                        .FirstOrDefault(x =>
                            x.EmployeeId == selectedEmployeeId);
                }


                // =====================================================
                // MONTH RANGE
                // =====================================================

                var firstDay = new DateTime(
                    selectedYear,
                    selectedMonth,
                    1);

                var lastDay = firstDay
                    .AddMonths(1)
                    .AddDays(-1);


                // =====================================================
                // ATTENDANCE DATA
                // =====================================================

                var attendance = new List<EmployeeAttendance>();

                if (selectedEmployeeId > 0)
                {
                    attendance = await _context.EmployeeAttendance
                        .Where(x =>
                            x.EmployeeId == selectedEmployeeId &&
                            x.AttendanceDate >= firstDay &&
                            x.AttendanceDate <= lastDay)
                        .OrderBy(x => x.AttendanceDate)
                        .ToListAsync();
                }


                // =====================================================
                // ATTENDANCE LOOKUP
                // =====================================================

                var attendanceByDate = attendance
                    .GroupBy(x => x.AttendanceDate.Date)
                    .ToDictionary(
                        g => g.Key,
                        g => g.First());


                // =====================================================
                // APPROVED LEAVE DATA
                // =====================================================

                var leaveRequests = new List<LeaveRequest>();

                if (selectedEmployeeId > 0)
                {
                    leaveRequests = await _context.LeaveRequest
                        .Where(x =>
                            x.EmployeeId == selectedEmployeeId &&
                            x.Status == "Approved" &&
                            x.FromDate <= lastDay &&
                            x.ToDate >= firstDay)
                        .ToListAsync();
                }


                // =====================================================
                // HOLIDAY DATA
                // =====================================================

                var holidayList = await _context.Holiday
                    .Where(x =>
                        x.HolidayDate >= firstDay &&
                        x.HolidayDate <= lastDay)
                    .ToListAsync();


                // =====================================================
                // HOLIDAY LOOKUP
                // =====================================================

                var holidayByDate = holidayList
                    .GroupBy(x => x.HolidayDate.Date)
                    .ToDictionary(
                        g => g.Key,
                        g => g.First());



                // =====================================================
                // VIEW MODEL
                // =====================================================

                var model = new HRAttendanceCalendarVM
                {
                    EmployeeId = selectedEmployeeId > 0
                        ? selectedEmployeeId
                        : null,

                    EmployeeName = selectedEmployee != null
                        ? $"{selectedEmployee.FirstName} {selectedEmployee.LastName}".Trim()
                        : null,

                    EmployeeCode = selectedEmployee?.EmployeeCode,

                    Year = selectedYear,
                    Month = selectedMonth
                };


                // =====================================================
                // CREATE CALENDAR DAYS
                // =====================================================

                if (selectedEmployeeId > 0)
                {
                    for (
                        var date = firstDay;
                        date <= lastDay;
                        date = date.AddDays(1))
                    {
                        var day = new AttendanceCalendarDayVM
                        {
                            Date = date,
                            IsCurrentMonth = true
                        };


                        // =================================================
                        // GET ATTENDANCE FOR THIS DATE
                        // =================================================

                        attendanceByDate.TryGetValue(
                            date.Date,
                            out var record);


                        // =================================================
                        // GET LEAVE FOR THIS DATE
                        // =================================================

                        var leaveRecord = leaveRequests
                            .FirstOrDefault(x =>
                                x.FromDate.Date <= date.Date &&
                                x.ToDate.Date >= date.Date);

                        // =================================================
                        // GET HOLIDAY FOR THIS DATE
                        // =================================================

                        holidayByDate.TryGetValue(
                            date.Date,
                            out var holidayRecord);
                        // =================================================
                        // COPY ATTENDANCE DETAILS
                        // Even when employee is on leave
                        // =================================================

                        if (record != null)
                        {
                            day.InTime = record.InTime;
                            day.OutTime = record.OutTime;

                            if (record.InTime.HasValue &&
                                record.OutTime.HasValue)
                            {
                                day.WorkingHours =
                                    (
                                        record.OutTime.Value -
                                        record.InTime.Value
                                    ).TotalHours;
                            }

                            day.AttendanceSource =
                                record.AttendanceSource;

                            day.CheckInLocation =
                                record.CheckInLocation;

                            day.CheckOutLocation =
                                record.CheckOutLocation;
                        }
                        // =================================================
                        // 1. HOLIDAY
                        // =================================================

                        if (holidayRecord != null)
                        {
                            day.IsHoliday = true;
                            day.HolidayName = holidayRecord.HolidayName;

                            day.Status = "Holiday";
                        }

                        // =================================================
                        // 2. LEAVE
                        // =================================================

                        else if (leaveRecord != null)
                        {
                            day.Status = "Leave";

                            day.LeaveFromDate =
                                leaveRecord.FromDate;

                            day.LeaveToDate =
                                leaveRecord.ToDate;

                            day.LeaveType =
                                leaveRecord.LeaveDuration;
                        }

                        // =================================================
                        // 3. ATTENDANCE
                        // =================================================

                        else if (record != null &&
                                 date.DayOfWeek != DayOfWeek.Saturday)
                        {
                            day.Status =
                                record.AttendanceStatus;
                        }

                        // =================================================
                        // 4. SUNDAY
                        // =================================================

                        else if (date.DayOfWeek == DayOfWeek.Sunday)
                        {
                            day.Status = "Weekend";
                        }

                        // =================================================
                        // 5. SATURDAY
                        // =================================================

                        else if (date.DayOfWeek == DayOfWeek.Saturday)
                        {
                            day.Status = "Work From Home";
                        }

                        // =================================================
                        // 6. PAST DATE WITHOUT ATTENDANCE
                        // =================================================

                        else if (date.Date < DateTime.Today)
                        {
                            day.Status = "Absent";
                        }

                        // =================================================
                        // 7. TODAY / FUTURE
                        // =================================================

                        else
                        {
                            day.Status = null;
                        }


                        model.Days.Add(day);
                    }
                }


                // =====================================================
                // DROPDOWNS
                // =====================================================

                ViewBag.EmployeeList = employees;

                ViewBag.SelectedEmployeeId =
                    selectedEmployeeId;

                ViewBag.SelectedMonth =
                    selectedMonth;

                ViewBag.SelectedYear =
                    selectedYear;


                ViewBag.YearList =
                    Enumerable
                        .Range(
                            DateTime.Today.Year - 2,
                            5)
                        .OrderByDescending(x => x)
                        .ToList();


                return View(model);
            }
            catch (Exception)
            {
                // Log exception here

                TempData["Error"] =
                    "Unable to load attendance.";

                return RedirectToAction(
                    "Index",
                    "Home");
            }
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> ManagerApproveRegularization(int id)
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
            {
                TempData["Error"] =
                    "Manager employee record not found.";

                return RedirectToAction(
                    nameof(RegularizationRequests));
            }


            // =========================================
            // FIND REQUEST
            // =========================================

            var request =
                await _context.AttendanceRegularization
                    .Include(x => x.Employee)
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
                        x.AttendanceDate == request.AttendanceDate);


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


            // =========================================
            // SAVE
            // =========================================

            await _context.SaveChangesAsync();


            TempData["Success"] =
                "Attendance regularization approved successfully.";


            return RedirectToAction(
                nameof(RegularizationRequests));
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> ManagerRejectRegularization(int id)
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
            {
                TempData["Error"] =
                    "Manager employee record not found.";

                return RedirectToAction(
                    nameof(RegularizationRequests));
            }


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
                    "Regularization request not found or you are not authorized to reject it.";

                return RedirectToAction(
                    nameof(RegularizationRequests));
            }


            // =========================================
            // ONLY PENDING REQUEST CAN BE REJECTED
            // =========================================

            if (request.Status != "Pending")
            {
                TempData["Error"] =
                    "This regularization request has already been processed.";

                return RedirectToAction(
                    nameof(RegularizationRequests));
            }


            // =========================================
            // REJECT REQUEST
            // =========================================

            request.Status =
                "Rejected";

            request.ApprovedDate =
                DateTime.Now;

            request.ApprovalRemarks =
                "Attendance regularization rejected.";


            // =========================================
            // SAVE
            // =========================================

            await _context.SaveChangesAsync();


            TempData["Success"] =
                "Attendance regularization rejected successfully.";


            return RedirectToAction(
                nameof(RegularizationRequests));
        }

        [HttpPost]
        public async Task<IActionResult> BulkApproveRegularization(
        List<int> selectedIds)
        {
            if (selectedIds == null || !selectedIds.Any())
            {
                TempData["Error"] =
                    "Please select at least one regularization request.";

                return RedirectToAction(nameof(AllRegularizations));
            }

            var currentUser =
                await _userManager.GetUserAsync(User);

            if (currentUser == null)
                return Unauthorized();

            int approvedCount = 0;
            int skippedCount = 0;


            // =========================================
            // PROCESS SELECTED REQUESTS
            // =========================================

            foreach (var id in selectedIds)
            {
                // =========================================
                // FIND REQUEST
                // =========================================

                var request =
                    await _context.AttendanceRegularization
                        .Include(x => x.Employee)
                        .FirstOrDefaultAsync(x =>
                            x.RegularizationId == id);

                if (request == null)
                {
                    skippedCount++;
                    continue;
                }


                // =========================================
                // ONLY PENDING REQUEST CAN BE APPROVED
                // =========================================

                if (request.Status != "Pending")
                {
                    skippedCount++;
                    continue;
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

                approvedCount++;
            }


            // =========================================
            // SAVE ALL CHANGES ONCE
            // =========================================

            await _context.SaveChangesAsync();


            // =========================================
            // RESULT MESSAGE
            // =========================================

            if (approvedCount > 0 && skippedCount == 0)
            {
                TempData["Success"] =
                    $"{approvedCount} regularization request(s) approved successfully.";
            }
            else if (approvedCount > 0 && skippedCount > 0)
            {
                TempData["Success"] =
                    $"{approvedCount} request(s) approved successfully. " +
                    $"{skippedCount} request(s) skipped.";
            }
            else
            {
                TempData["Error"] =
                    "No selected regularization requests could be approved.";
            }


            return RedirectToAction(nameof(AllRegularizations));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkRejectRegularization(
        List<int> selectedIds)
        {
            if (selectedIds == null || !selectedIds.Any())
            {
                TempData["Error"] =
                    "Please select at least one regularization request.";

                return RedirectToAction(nameof(AllRegularizations));
            }

            var currentUser =
                await _userManager.GetUserAsync(User);

            if (currentUser == null)
                return Unauthorized();


            int rejectedCount = 0;
            int skippedCount = 0;


            // =========================================
            // PROCESS SELECTED REQUESTS
            // =========================================

            foreach (var id in selectedIds)
            {
                // =========================================
                // FIND REQUEST
                // =========================================

                var request =
                    await _context.AttendanceRegularization
                        .FirstOrDefaultAsync(x =>
                            x.RegularizationId == id);

                if (request == null)
                {
                    skippedCount++;
                    continue;
                }


                // =========================================
                // ONLY PENDING REQUEST CAN BE REJECTED
                // =========================================

                if (request.Status != "Pending")
                {
                    skippedCount++;
                    continue;
                }


                // =========================================
                // REJECT REQUEST
                // =========================================

                request.Status =
                    "Rejected";

                request.ApprovedDate =
                    DateTime.Now;

                request.ApprovalRemarks =
                    "Attendance regularization rejected.";

                rejectedCount++;
            }


            // =========================================
            // SAVE ALL CHANGES
            // =========================================

            await _context.SaveChangesAsync();


            // =========================================
            // RESULT MESSAGE
            // =========================================

            if (rejectedCount > 0 && skippedCount == 0)
            {
                TempData["Success"] =
                    $"{rejectedCount} regularization request(s) rejected successfully.";
            }
            else if (rejectedCount > 0 && skippedCount > 0)
            {
                TempData["Success"] =
                    $"{rejectedCount} request(s) rejected successfully. " +
                    $"{skippedCount} request(s) skipped.";
            }
            else
            {
                TempData["Error"] =
                    "No selected regularization requests could be rejected.";
            }


            return RedirectToAction(nameof(AllRegularizations));
        }
    }
}

    