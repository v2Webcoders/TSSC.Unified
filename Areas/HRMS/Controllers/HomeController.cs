using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QUIZAPP.Models;
using QUIZAPP.ViewModel;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Security.Claims;
using System.Xml.Linq;
using TSSC.Unified.ViewModel;
using iText.IO.Image;
using iText.Kernel.Colors;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using Paragraph = iText.Layout.Element.Paragraph;
using TSSC.Unified.Services;

namespace QUIZAPP.Areas.HRMS.Controllers
{
    [Area("HRMS")]
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly AppdbContext _db;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _environment;
        private readonly ITodayAttendanceLiveService _todayAttendanceLiveService;
        private readonly IAttendanceSummaryService _attendanceSummaryService;
        private readonly ApprovalService _approvalService;
        private static readonly TimeSpan LateCutOff = new(10, 0, 0);
        public HomeController(ITodayAttendanceLiveService todayAttendanceLiveService, ILogger<HomeController> logger, AppdbContext db, IConfiguration configuration, IWebHostEnvironment environment, IAttendanceSummaryService attendanceSummaryService, ApprovalService approvalService)
        {
            _logger = logger;
            _db = db;
            _configuration = configuration;
            _environment = environment;
            _todayAttendanceLiveService = todayAttendanceLiveService;
            _attendanceSummaryService = attendanceSummaryService;
            _approvalService = approvalService;
        }

        public async Task<IActionResult> Index()
        {
            if (User.IsInRole("CEO"))
                return RedirectToAction(nameof(CEODashboard));

            if (User.IsInRole("HR"))
                return RedirectToAction(nameof(HRDashboard));

            //if (User.IsInRole("StandardsTeam"))
            //{
            //    var designation = await _db.Employee
            //        .Where(e => e.OfficialEmail == User.Identity.Name)
            //        .Select(e => e.Designation.DesignationName)
            //        .FirstOrDefaultAsync();

            //    if (string.Equals(
            //            designation?.Trim(),
            //            "GM - Head Standards & Technology",
            //            StringComparison.OrdinalIgnoreCase))
            //    {
            //        return RedirectToAction(nameof(StandardsHeadDashboard));
            //    }
            //}

            return RedirectToAction(nameof(EmployeeDashboard));
        }

        [Authorize(Roles = "StandardsTeam")]
        public IActionResult StandardsHeadDashboard()
        {
            return View();
        }

        //        [Authorize(Roles = "CEO")]
        //        public async Task<IActionResult> CEODashboard(string? financialYear)
        //        {
        //            var applicationUserId =
        //                User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        //            if (string.IsNullOrEmpty(applicationUserId))
        //                return RedirectToAction("Login", "Account");

        //            var employee = await _db.Employee
        //                .FirstOrDefaultAsync(x =>
        //                    x.ApplicationUserId == applicationUserId);

        //            var today = DateTime.Today;
        //            var tomorrow = today.AddDays(1);

        //            var model = new CEODashboardVM
        //            {
        //                EmployeeName = employee?.FirstName ?? "CEO",
        //                EmployeeCode = employee?.EmployeeCode
        //            };

        //            // =====================================================
        //            // 1. HR - WORKFORCE
        //            // =====================================================

        //            // TOTAL ACTIVE EMPLOYEES

        //            var activeEmployeeIds = await _db.Employee
        //                .Where(x => x.IsActive)
        //                .Select(x => x.EmployeeId)
        //                .ToListAsync();

        //            var activeEmployeeSet = activeEmployeeIds.ToHashSet();

        //            model.Workforce.TotalEmployees = activeEmployeeSet.Count;

        //            // =====================================================
        //            // TODAY'S ATTENDANCE RECORDS
        //            // =====================================================

        //            var attendanceRecords = await _db.EmployeeAttendance
        //                .Where(x =>
        //                    x.AttendanceDate >= today &&
        //                    x.AttendanceDate < tomorrow &&
        //                    activeEmployeeSet.Contains(x.EmployeeId))
        //                .Select(x => new
        //                {
        //                    x.EmployeeId,
        //                    x.InTime,
        //                    x.AttendanceSource
        //                })
        //                .ToListAsync();

        //            // =====================================================
        //            // EMPLOYEES ON APPROVED LEAVE
        //            // =====================================================

        //            var employeesOnLeave = await _db.LeaveRequest
        //                .Where(x =>
        //                    x.Status == "Approved" &&
        //                    x.FromDate <= today &&
        //                    x.ToDate >= today &&
        //                    activeEmployeeSet.Contains(x.EmployeeId))
        //                .Select(x => x.EmployeeId)
        //                .Distinct()
        //                .ToListAsync();

        //            var onLeaveSet = employeesOnLeave.ToHashSet();

        //            model.Workforce.OnLeave = onLeaveSet.Count;

        //            // =====================================================
        //            // EMPLOYEES ON DUTY (OD)
        //            // =====================================================

        //            var onDutyIds = attendanceRecords
        //                .Where(x => x.AttendanceSource == "OD")
        //                .Select(x => x.EmployeeId)
        //                .Distinct()
        //                .ToHashSet();

        //            // Leave has priority over attendance classification
        //            onDutyIds.ExceptWith(onLeaveSet);

        //            model.Workforce.OnDuty = onDutyIds.Count;

        //            // =====================================================
        //            // PRESENT EMPLOYEES
        //            // =====================================================

        //            // Exclude OD and approved leave employees.
        //            // Consider the earliest check-in for each employee.

        //            var firstCheckIns = attendanceRecords
        //                .Where(x =>
        //                    x.InTime.HasValue &&
        //                    x.AttendanceSource != "OD" &&
        //                    !onLeaveSet.Contains(x.EmployeeId))
        //                .GroupBy(x => x.EmployeeId)
        //                .Select(g => new
        //                {
        //                    EmployeeId = g.Key,
        //                    InTime = g.Min(x => x.InTime!.Value)
        //                })
        //                .Where(x => !onDutyIds.Contains(x.EmployeeId))
        //                .ToList();

        //            // =====================================================
        //            // ON TIME - BEFORE 10:00 AM
        //            // =====================================================

        //            var onTimeCount = firstCheckIns
        //                .Count(x =>
        //                    x.InTime.TimeOfDay < new TimeSpan(10, 0, 0));

        //            // =====================================================
        //            // DELAYED - 10:00 AM ONWARDS
        //            // =====================================================

        //            var delayedCount = firstCheckIns
        //                .Count(x =>
        //                    x.InTime.TimeOfDay >= new TimeSpan(10, 0, 0));

        //            model.Workforce.OnTime = onTimeCount;

        //            model.Workforce.Delayed = delayedCount;

        //            model.Workforce.Present =
        //                onTimeCount + delayedCount;

        //            // =====================================================
        //            // ABSENT EMPLOYEES
        //            // =====================================================

        //            // Sunday and published holidays are not counted as absent.

        //            var isSunday = today.DayOfWeek == DayOfWeek.Sunday;

        //            var isHoliday = await _db.Holiday
        //                .AnyAsync(x =>
        //                    x.HolidayDate >= today &&
        //                    x.HolidayDate < tomorrow &&
        //                    x.Status == "Published");

        //            if (!isSunday && !isHoliday)
        //            {
        //                model.Workforce.Absent =
        //                    Math.Max(
        //                        0,
        //                        model.Workforce.TotalEmployees -
        //                        model.Workforce.Present -
        //                        model.Workforce.OnDuty -
        //                        model.Workforce.OnLeave);
        //            }
        //            else
        //            {
        //                model.Workforce.Absent = 0;
        //            }

        //            // =====================================================
        //            // PRESENT PERCENTAGE
        //            // =====================================================

        //            model.Workforce.PresentPercentage =
        //                model.Workforce.TotalEmployees > 0
        //                    ? Math.Round(
        //                        (decimal)model.Workforce.Present /
        //                        model.Workforce.TotalEmployees * 100, 0)
        //                    : 0;

        //            // =====================================================
        //            // 2. CEO - APPROVALS PENDING
        //            // =====================================================

        //            // Financial year (April to March)
        //            var currentFYStart = today.Month >= 4
        //                ? today.Year
        //                : today.Year - 1;

        //            var currentFY = $"{currentFYStart}-{(currentFYStart + 1) % 100:00}";

        //            var selectedFY = string.IsNullOrWhiteSpace(financialYear)
        //                ? currentFY
        //                : financialYear;

        //            model.FinancialYear = selectedFY;

        //            model.FinancialYears = new List<string>
        //{
        //    currentFY,
        //    $"{currentFYStart - 1}-{currentFYStart % 100:00}",
        //    $"{currentFYStart - 2}-{(currentFYStart - 1) % 100:00}"
        //};

        //            // =====================================================
        //            // GET APPROVAL RECORDS FROM SQL VIEW
        //            // =====================================================

        //            var approvalQuery = _db.CEOApprovals
        //                .AsNoTracking()
        //                .Where(x => x.FinancialYear == selectedFY);

        //            var allApprovals = await approvalQuery.ToListAsync();

        //            // =====================================================
        //            // SUMMARY CARDS
        //            // =====================================================

        //            model.ApprovalSummary.PendingLeave =
        //                allApprovals.Count(x =>
        //                    x.RequestType == "Leave" &&
        //                    x.Status == "Pending");

        //            model.ApprovalSummary.AwaitingApprovals =
        //                allApprovals.Count(x => x.Status == "Pending");

        //            // Priority is currently a placeholder in the SQL View.
        //            // Do not treat this count as a genuine business metric
        //            // until actual priority data is available.
        //            model.ApprovalSummary.HighPriority =
        //                allApprovals.Count(x =>
        //                    x.Status == "Pending" &&
        //                    x.Priority == "High");

        //            model.ApprovalSummary.Overdue =
        //                allApprovals.Count(x =>
        //                    x.Status == "Pending" &&
        //                    x.DueDate.HasValue &&
        //                    x.DueDate.Value.Date < today);

        //            model.ApprovalSummary.Decided =
        //                allApprovals.Count(x =>
        //                    x.Status == "Approved" ||
        //                    x.Status == "Rejected");

        //            // =====================================================
        //            // PENDING REQUESTS TABLE
        //            // =====================================================

        //            //model.PendingApprovals = allApprovals
        //            //    .Where(x => x.Status == "Pending")
        //            //    .OrderByDescending(x => x.SubmittedDate)
        //            //    .ToList();
        //            model.PendingApprovals = allApprovals
        //    .Where(x => x.Status == "Pending")
        //    .OrderByDescending(x => x.SubmittedDate)
        //    .Select(x => new CEOApprovalRequestVM
        //    {
        //        Id = x.ApprovalId,
        //        RequestId = x.RequestId,
        //        RequestTitle = x.RequestTitle,
        //        Department = x.Department,
        //        SubmittedBy = x.SubmittedBy,
        //        SubmittedDate = x.SubmittedDate,
        //        DueDate = x.DueDate,
        //        Priority = x.Priority,
        //        Status = x.Status,
        //        RequestType = x.RequestType,
        //        SourceTable = x.SourceTable
        //    })
        //    .ToList();
        //            // =====================================================
        //            // DEPARTMENT FILTER
        //            // =====================================================

        //            model.ApprovalDepartments = model.PendingApprovals
        //                .Select(x => x.Department)
        //                .Where(x => !string.IsNullOrWhiteSpace(x))
        //                .Distinct()
        //                .OrderBy(x => x)
        //                .ToList();


        //            return View(model);
        //        }

        [Authorize(Roles = "CEO")]
        public async Task<IActionResult> CEODashboard(string? financialYear, string? kpiFinancialYear)
        {
            var applicationUserId =
                User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(applicationUserId))
                return RedirectToAction("Login", "Account");

            var employee = await _db.Employee
                .FirstOrDefaultAsync(x =>
                    x.ApplicationUserId == applicationUserId);

            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);

            var model = new CEODashboardVM
            {
                EmployeeName = employee?.FirstName ?? "CEO",
                EmployeeCode = employee?.EmployeeCode
            };


            // =====================================================
            // TODAY'S BIRTHDAYS
            // =====================================================

            model.TodaysBirthdays = await _db.Employee
                .AsNoTracking()
                .Where(x =>
                    x.IsActive &&
                    x.DOB.HasValue &&
                    x.DOB.Value.Month == today.Month &&
                    x.DOB.Value.Day == today.Day)
                .Select(x => new CEOBirthdayVM
                {
                    EmployeeId = x.EmployeeId,
                    EmployeeName = (x.FirstName + " " + (x.LastName ?? "")).Trim(),
                    Department = x.Department.DepartmentName
                })
                .ToListAsync();

            //// =====================================================
            //// 1. HR - WORKFORCE
            //// =====================================================

            //// TOTAL ACTIVE EMPLOYEES

            //var activeEmployeeIds = await _db.Employee
            //    .Where(x => x.IsActive)
            //    .Select(x => x.EmployeeId)
            //    .ToListAsync();

            //var activeEmployeeSet = activeEmployeeIds.ToHashSet();

            //model.Workforce.TotalEmployees = activeEmployeeSet.Count;

            //// =====================================================
            //// TODAY'S ATTENDANCE RECORDS
            //// =====================================================

            //var attendanceRecords = await _db.EmployeeAttendance
            //    .Where(x =>
            //        x.AttendanceDate >= today &&
            //        x.AttendanceDate < tomorrow &&
            //        activeEmployeeSet.Contains(x.EmployeeId))
            //    .Select(x => new
            //    {
            //        x.EmployeeId,
            //        x.InTime,
            //        x.AttendanceSource
            //    })
            //    .ToListAsync();

            //// =====================================================
            //// EMPLOYEES ON APPROVED LEAVE
            //// =====================================================

            //var employeesOnLeave = await _db.LeaveRequest
            //    .Where(x =>
            //        x.Status == "Approved" &&
            //        x.FromDate <= today &&
            //        x.ToDate >= today &&
            //        activeEmployeeSet.Contains(x.EmployeeId))
            //    .Select(x => x.EmployeeId)
            //    .Distinct()
            //    .ToListAsync();

            //var onLeaveSet = employeesOnLeave.ToHashSet();

            //model.Workforce.OnLeave = onLeaveSet.Count;

            //// =====================================================
            //// EMPLOYEES ON DUTY (OD)
            //// =====================================================

            //var onDutyIds = attendanceRecords
            //    .Where(x => x.AttendanceSource == "OD")
            //    .Select(x => x.EmployeeId)
            //    .Distinct()
            //    .ToHashSet();

            //// Leave has priority over attendance classification
            //onDutyIds.ExceptWith(onLeaveSet);

            //model.Workforce.OnDuty = onDutyIds.Count;

            //// =====================================================
            //// PRESENT EMPLOYEES
            //// =====================================================

            //// Exclude OD and approved leave employees.
            //// Consider the earliest check-in for each employee.

            //var firstCheckIns = attendanceRecords
            //    .Where(x =>
            //        x.InTime.HasValue &&
            //        x.AttendanceSource != "OD" &&
            //        !onLeaveSet.Contains(x.EmployeeId))
            //    .GroupBy(x => x.EmployeeId)
            //    .Select(g => new
            //    {
            //        EmployeeId = g.Key,
            //        InTime = g.Min(x => x.InTime!.Value)
            //    })
            //    .Where(x => !onDutyIds.Contains(x.EmployeeId))
            //    .ToList();

            //// =====================================================
            //// ON TIME - BEFORE 10:00 AM
            //// =====================================================

            //var onTimeCount = firstCheckIns
            //    .Count(x =>
            //        x.InTime.TimeOfDay < new TimeSpan(10, 0, 0));

            //// =====================================================
            //// DELAYED - 10:00 AM ONWARDS
            //// =====================================================

            //var delayedCount = firstCheckIns
            //    .Count(x =>
            //        x.InTime.TimeOfDay >= new TimeSpan(10, 0, 0));

            //model.Workforce.OnTime = onTimeCount;

            //model.Workforce.Delayed = delayedCount;

            //model.Workforce.Present =
            //    onTimeCount + delayedCount;

            //// =====================================================
            //// ABSENT EMPLOYEES
            //// =====================================================

            //// Sunday and published holidays are not counted as absent.

            //var isSunday = today.DayOfWeek == DayOfWeek.Sunday;

            //var isHoliday = await _db.Holiday
            //    .AnyAsync(x =>
            //        x.HolidayDate >= today &&
            //        x.HolidayDate < tomorrow &&
            //        x.Status == "Published");

            //if (!isSunday && !isHoliday)
            //{
            //    model.Workforce.Absent =
            //        Math.Max(
            //            0,
            //            model.Workforce.TotalEmployees -
            //            model.Workforce.Present -
            //            model.Workforce.OnDuty -
            //            model.Workforce.OnLeave);
            //}
            //else
            //{
            //    model.Workforce.Absent = 0;
            //}

            //// =====================================================
            //// PRESENT PERCENTAGE
            //// =====================================================

            //model.Workforce.PresentPercentage =
            //    model.Workforce.TotalEmployees > 0
            //        ? Math.Round(
            //            (decimal)model.Workforce.Present /
            //            model.Workforce.TotalEmployees * 100, 0)
            //        : 0;

            //// =====================================================
            //// 2. CEO - APPROVALS PENDING
            //// =====================================================
            ///

            // =====================================================
            // 1. HR - WORKFORCE
            // =====================================================
            var workforce = await GetTodayWorkforceAsync(today);

            model.Workforce.TotalEmployees = workforce.Count;
            model.Workforce.OnTime = workforce.Count(x => x.Status == AttStatus.OnTime);
            model.Workforce.Delayed = workforce.Count(x => x.Status == AttStatus.Late);
            model.Workforce.Present = model.Workforce.OnTime + model.Workforce.Delayed;
            model.Workforce.OnDuty = workforce.Count(x => x.Status == AttStatus.OnDuty);
            model.Workforce.OnLeave = workforce.Count(x => x.Status == AttStatus.OnLeave);
            model.Workforce.Absent = workforce.Count(x => x.Status == AttStatus.Absent);

            model.Workforce.PresentPercentage = model.Workforce.TotalEmployees > 0
                ? Math.Round((decimal)model.Workforce.Present / model.Workforce.TotalEmployees * 100, 0)
                : 0;

            // Financial year (April to March)
            var currentFYStart = today.Month >= 4
                ? today.Year
                : today.Year - 1;

            var currentFY = $"{currentFYStart}-{(currentFYStart + 1) % 100:00}";

            //var selectedFY = string.IsNullOrWhiteSpace(financialYear)
            //    ? currentFY
            //    : financialYear;

            //model.FinancialYear = selectedFY;

            model.FinancialYears = new List<string>
            {
                currentFY,
                $"{currentFYStart - 1}-{currentFYStart % 100:00}",
                $"{currentFYStart - 2}-{(currentFYStart - 1) % 100:00}"
            };

            // Accept only a known financial year
            var selectedFY = (!string.IsNullOrWhiteSpace(financialYear) &&
                              model.FinancialYears.Contains(financialYear))
                ? financialYear
                : currentFY;

            model.FinancialYear = selectedFY;

            // PENDING (FY applied)
            var pending = await _db.CEOApprovals
                .AsNoTracking()
                .Where(x => x.FinancialYear == selectedFY)
                .ToListAsync();

            // SUMMARY CARDS
            model.ApprovalSummary.PendingLeave = pending.Count(x => x.RequestType == "Leave");
            model.ApprovalSummary.AwaitingApprovals = pending.Count(x => x.RequestType != "Leave");
            model.ApprovalSummary.HighPriority = pending.Count(x => x.Priority == "High");
            model.ApprovalSummary.Overdue = pending.Count(x => x.DueDate.HasValue && x.DueDate.Value.Date < today);

            // DECIDED (same FY, by decision date)
            //model.ApprovalSummary.Decided = await _db.CEODecisions
            model.ApprovalSummary.Decided = await _db.CEODecisions
                .AsNoTracking()
                .CountAsync(x => x.FinancialYear == selectedFY);

            // TABLE: overdue first, then priority, then due date
            model.PendingApprovals = pending
                .OrderByDescending(x => x.DueDate.HasValue && x.DueDate.Value.Date < today)
                .ThenBy(x => x.Priority == "High" ? 0 : x.Priority == "Medium" ? 1 : 2)
                .ThenBy(x => x.DueDate ?? DateTime.MaxValue)
                .Select(x => new CEOApprovalRequestVM
                {
                    Id = x.ApprovalId,
                    RequestId = x.RequestId,
                    RequestTitle = x.RequestTitle,
                    Department = x.Department,
                    SubmittedBy = x.SubmittedBy,
                    SubmittedDate = x.SubmittedDate,
                    DueDate = x.DueDate,
                    Priority = x.Priority,
                    Status = x.Status,
                    RequestType = x.RequestType,
                    SourceTable = x.SourceTable
                })
                .ToList();

            // DEPARTMENT FILTER
            model.ApprovalDepartments = model.PendingApprovals
                .Select(x => x.Department)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .OrderBy(x => x)
                .ToList();
            // =====================================================
            // 3. FINANCIAL KPIs (from KPI Master)
            // =====================================================

            var kpiNames = new[] { "Gross Revenue", "Surplus", "Govt Business", "Non Govt Business" };

            // The KPI section has its own FY dropdown (kpiFinancialYear), separate from the
            // approvals one (financialYear). Accept only the known financial years.
            var selectedKpiFY = (!string.IsNullOrWhiteSpace(kpiFinancialYear) &&
                                 model.FinancialYears.Contains(kpiFinancialYear))
                ? kpiFinancialYear
                : currentFY;

            model.KpiFinancialYear = selectedKpiFY;

            // Financial year window (April to March), e.g. "2026-27"
            var fyStartYear = int.Parse(selectedKpiFY.Substring(0, 4));
            var fyStart = new DateTime(fyStartYear, 4, 1);
            var fyEnd = fyStart.AddYears(1);                  // exclusive: 1 April next year

            // An entry belongs to the FY in which its From Date falls.
            // For each KPI the latest saved entry in that FY is shown.
            var kpiRows = await _db.KpiMasters
                .AsNoTracking()
                .Where(k => kpiNames.Contains(k.KpiName)
                         && k.FromDate >= fyStart
                         && k.FromDate < fyEnd)
                .ToListAsync();

            model.FinancialKpis = kpiNames
                .Select(name =>
                {
                    var row = kpiRows
                        .Where(k => k.KpiName == name)
                        .OrderByDescending(k => k.CreatedOn)
                        .ThenByDescending(k => k.Id)
                        .FirstOrDefault();

                    return new CEOKpiCardVM
                    {
                        Name = name,
                        Total = row?.TotalValue,
                        Received = row?.ReceivedValue,
                        FromDate = row?.FromDate,
                        ToDate = row?.ToDate
                    };
                })
                .ToList();
            return View(model);
        }
        [Authorize(Roles = "HR,CEO")]
        public async Task<IActionResult> HRDashboard()
        {
            var applicationUserId =
                User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(applicationUserId))
                return RedirectToAction("Login", "Account");

            var employee = await _db.Employee
                .FirstOrDefaultAsync(x =>
                    x.ApplicationUserId == applicationUserId);

            var today = DateTime.Today;

            var model = new HRDashboardVM
            {
                EmployeeName = employee?.FirstName ?? "HR",
                EmployeeCode = employee?.EmployeeCode
            };

            // =====================================================
            // EMPLOYEE BIRTHDAYS - TODAY + NEXT 6 DAYS
            // =====================================================

            var employees = await _db.Employee
                .Where(x => x.IsActive && x.DOB.HasValue)
                .ToListAsync();

            model.Birthdays = employees
                .Select(x =>
                {
                    var dob = x.DOB!.Value;

                    var birthday = new DateTime(
                        today.Year,
                        dob.Month,
                        dob.Day);

                    if (birthday < today)
                        birthday = birthday.AddYears(1);

                    return new
                    {
                        Employee = x,
                        Birthday = birthday
                    };
                })
                .Where(x =>
                    x.Birthday >= today &&
                    x.Birthday <= today.AddDays(6))
                .OrderBy(x => x.Birthday)
                .Select(x => new EmployeeBirthdayVM
                {
                    EmployeeName =
                        x.Employee.FirstName + " " +
                        x.Employee.LastName,

                    DOB = x.Employee.DOB!.Value,
                    PhotoPath = x.Employee.PhotoPath,
                    IsToday = x.Birthday.Date == today
                })
                .ToList();

            // =====================================================
            // TODAY'S ATTENDANCE - LIVE / MANUAL FALLBACK
            // =====================================================

            if (employee != null)
            {
                var attendance =
                    await _todayAttendanceLiveService
                        .GetTodayAttendanceAsync(employee.BiometricCode);

                DateTime? todayInTime = attendance.InTime;
                DateTime? todayOutTime = attendance.OutTime;

                if (!todayInTime.HasValue)
                {
                    var manualAttendance =
                        await _db.EmployeeAttendance
                            .Where(x =>
                                x.EmployeeId == employee.EmployeeId &&
                                x.AttendanceDate >= today &&
                                x.AttendanceDate < today.AddDays(1) &&
                                x.AttendanceSource == "Manual")
                            .OrderByDescending(x => x.AttendanceId)
                            .FirstOrDefaultAsync();

                    if (manualAttendance != null)
                    {
                        todayInTime = manualAttendance.InTime;
                        todayOutTime = manualAttendance.OutTime;
                    }
                }

                model.TodayInTime = todayInTime;
                model.TodayOutTime = todayOutTime;
            }

            // =====================================================
            // TOTAL EMPLOYEES
            // =====================================================

            model.TotalEmployees = await _db.Employee.CountAsync(e => e.IsActive);

            // =====================================================
            // PRESENT TODAY
            // =====================================================

            model.PresentToday =
                await _db.EmployeeAttendance
                    .CountAsync(x =>
                        x.AttendanceDate >= today &&
                        x.AttendanceDate < today.AddDays(1) &&
                        x.InTime != null);

            // =====================================================
            // EMPLOYEES ON LEAVE TODAY
            // =====================================================

            model.EmployeesOnLeave =
                await _db.LeaveRequest
                    .CountAsync(x =>
                        x.Status == "Approved" &&
                        x.FromDate <= today &&
                        x.ToDate >= today);

            // =====================================================
            // PENDING LEAVE REQUESTS
            // =====================================================

            model.PendingLeaveRequests =
                await _db.LeaveRequest
                    .CountAsync(x => x.Status == "Pending");

            // =====================================================
            // RECENT LEAVE REQUESTS
            // =====================================================

            model.RecentLeaveRequests =
                await _db.LeaveRequest
                    .Include(x => x.Employee)
                    .Include(x => x.LeaveType)
                    .OrderByDescending(x => x.CreatedDate)
                    .Take(5)
                    .Select(x => new RecentLeaveRequestVM
                    {
                        EmployeeName =
                            x.Employee != null
                                ? x.Employee.FirstName + " " +
                                  x.Employee.LastName
                                : "Unknown",

                        LeaveType =
                            x.LeaveType != null
                                ? x.LeaveType.LeaveTypeName
                                : "Unknown",

                        FromDate = x.FromDate,
                        ToDate = x.ToDate,
                        Status = x.Status
                    })
                    .ToListAsync();

            return View("Index", model);
        }



        [Authorize]
        public async Task<IActionResult> EmployeeDashboard()
        {
            // Prevent HR and CEO from accessing the employee dashboard.
            //if (User.IsInRole("CEO"))
            //    return RedirectToAction(nameof(CEODashboard));

            if (User.IsInRole("HR"))
                return RedirectToAction(nameof(HRDashboard));

            var applicationUserId =
                User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(applicationUserId))
                return RedirectToAction("Login", "Account");

            var employee = await _db.Employee
                .FirstOrDefaultAsync(x =>
                    x.ApplicationUserId == applicationUserId);

            if (employee == null)
                return NotFound();

            if (employee.ProfileStatus == "Pending")
                return RedirectToAction("EditProfile", "Employee");

            var today = DateTime.Today;

            // =====================================================
            // TODAY'S ATTENDANCE - LIVE / BIOMETRIC
            // =====================================================

            var attendance =
                await _todayAttendanceLiveService
                    .GetTodayAttendanceAsync(employee.BiometricCode);

            DateTime? todayInTime = attendance.InTime;
            DateTime? todayOutTime = attendance.OutTime;

            // =====================================================
            // FALLBACK - MANUAL ATTENDANCE
            // =====================================================

            if (!todayInTime.HasValue)
            {
                var manualAttendance =
                    await _db.EmployeeAttendance
                        .Where(x =>
                            x.EmployeeId == employee.EmployeeId &&
                            x.AttendanceDate >= today &&
                            x.AttendanceDate < today.AddDays(1) &&
                            x.AttendanceSource == "Manual")
                        .OrderByDescending(x => x.AttendanceId)
                        .FirstOrDefaultAsync();

                if (manualAttendance != null)
                {
                    todayInTime = manualAttendance.InTime;
                    todayOutTime = manualAttendance.OutTime;
                }
            }

            // =====================================================
            // DASHBOARD MODEL
            // =====================================================

            var model = new HRDashboardVM
            {
                EmployeeName = employee.FirstName,
                EmployeeCode = employee.EmployeeCode,
                TodayInTime = todayInTime,
                TodayOutTime = todayOutTime,
                DOB = employee.DOB
            };

            // =====================================================
            // EMPLOYEE BIRTHDAYS - TODAY
            // =====================================================

            model.Birthdays = await _db.Employee
                .Where(x =>
                    x.IsActive &&
                    x.DOB.HasValue &&
                    x.DOB.Value.Month == today.Month &&
                    x.DOB.Value.Day == today.Day)
                .Select(x => new EmployeeBirthdayVM
                {
                    EmployeeName =
                        x.FirstName + " " + x.LastName,

                    DOB = x.DOB!.Value,
                    PhotoPath = x.PhotoPath,
                    IsToday = true
                })
                .ToListAsync();

            // =====================================================
            // CURRENT MONTH ATTENDANCE SUMMARY
            // =====================================================

            var summary =
                await _attendanceSummaryService
                    .GetMonthlySummaryAsync(
                        employee.EmployeeId,
                        today.Year,
                        today.Month);

            model.PresentDaysThisMonth = summary.DaysWorked;

            model.AttendanceToRegularize =
                summary.MissingPunchCount;

            // =====================================================
            // WORKING DAYS - EXCLUDING SUNDAYS AND HOLIDAYS
            // =====================================================

            var firstDayOfMonth =
                new DateTime(today.Year, today.Month, 1);

            var holidays = await _db.Holiday
                .Where(x =>
                    x.HolidayDate >= firstDayOfMonth &&
                    x.HolidayDate <= today &&
                    x.Status == "Published")
                .Select(x => x.HolidayDate.Date)
                .ToListAsync();

            var holidaySet = holidays.ToHashSet();

            int workingDays = 0;

            for (var date = firstDayOfMonth;
                 date <= today;
                 date = date.AddDays(1))
            {
                if (date.DayOfWeek != DayOfWeek.Sunday &&
                    !holidaySet.Contains(date))
                {
                    workingDays++;
                }
            }

            model.WorkingDaysThisMonth = workingDays;

            // =====================================================
            // PENDING LEAVE REQUESTS
            // =====================================================

            model.PendingRequests =
                await _db.LeaveRequest
                    .CountAsync(x =>
                        x.EmployeeId == employee.EmployeeId &&
                        x.Status == "Pending");

            // =====================================================
            // LEAVE BALANCE
            // =====================================================

            model.LeaveBalance = 0;

            // =====================================================
            // RECENT EMPLOYEE REQUESTS
            // =====================================================

            model.RecentRequests =
                await _db.LeaveRequest
                    .Where(x =>
                        x.EmployeeId == employee.EmployeeId)
                    .OrderByDescending(x => x.CreatedDate)
                    .Take(5)
                    .Select(x => new EmployeeRecentRequestVM
                    {
                        RequestType = "Leave Request",
                        RequestDate = x.CreatedDate,
                        Status = x.Status
                    })
                    .ToListAsync();

            return View("EmployeeDashboard", model);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }


        private async Task<(DateTime? InTime, DateTime? OutTime)> GetTodayAttendance(
        string employeeCode)
        {
            DateTime? inTime = null;
            DateTime? outTime = null;

            //try
            {
                var connectionString =
                    _configuration.GetConnectionString("BiometricConnection");

                if (string.IsNullOrWhiteSpace(connectionString))
                {
                    _logger.LogError(
                        "BiometricConnection connection string is missing.");

                    return (null, null);
                }

                using var connection =
                    new SqlConnection(connectionString);

                await connection.OpenAsync();


                // Exclusive upper boundary
                var startDate = DateTime.Now;
                var tableName = $"DeviceLogs_{startDate.Month}_{startDate.Year}";
                var sql = $@"
                SELECT
                    MIN(LogDate) AS InTime,
                    CASE
                        WHEN MAX(LogDate) >= DATEADD(MINUTE, 30, MIN(LogDate))
                        THEN MAX(LogDate)
                        ELSE NULL
                    END AS OutTime
                FROM [{tableName}]
                WHERE UserId = @EmployeeCode
                  AND LogDate >= @Today
                  AND LogDate < DATEADD(DAY, 1, @Today);";

                using var command =
                    new SqlCommand(sql, connection);

                command.Parameters.Add(
                    "@EmployeeCode",
                    SqlDbType.VarChar,
                    50).Value = employeeCode;

                command.Parameters.Add(
                    "@Today",
                    SqlDbType.DateTime).Value = DateTime.Today;

                using var reader =
                    await command.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    if (reader["InTime"] != DBNull.Value)
                    {
                        inTime =
                            Convert.ToDateTime(reader["InTime"]);
                    }

                    if (reader["OutTime"] != DBNull.Value)
                    {
                        outTime =
                            Convert.ToDateTime(reader["OutTime"]);
                    }
                }

                return (inTime, outTime);
            }
            //catch (SqlException ex)
            //{
            //    _logger.LogError(
            //        ex,
            //        "Biometric database error while getting attendance for EmployeeCode: {EmployeeCode}",
            //        employeeCode);

            //    return (null, null);
            //}
            //catch (Exception ex)
            //{
            //    _logger.LogError(
            //        ex,
            //        "Unexpected error while getting biometric attendance for EmployeeCode: {EmployeeCode}",
            //        employeeCode);

            //    return (null, null);
            //}
        }

        [HttpGet]
        public async Task<IActionResult> TestBM()
        {
            try
            {
                var connectionString =
                    _configuration.GetConnectionString("BiometricConnection");

                // 1. Check connection string
                if (string.IsNullOrWhiteSpace(connectionString))
                {
                    return Json(new
                    {
                        success = false,
                        step = "Connection String",
                        message = "BiometricConnection connection string is missing."
                    });
                }

                // 2. Create SQL connection
                using var connection =
                    new SqlConnection(connectionString);

                // 3. Test connection
                await connection.OpenAsync();

                // 4. Check database/server information
                var serverName = connection.DataSource;
                var databaseName = connection.Database;

                // 5. Test the DeviceLogs table
                var sql = @"
            SELECT TOP 1
                UserId,
                LogDate
            FROM dbo.DeviceLogs_9_2026
            ORDER BY LogDate DESC;";

                using var command =
                    new SqlCommand(sql, connection);

                using var reader =
                    await command.ExecuteReaderAsync();

                object? lastUserId = null;
                DateTime? lastLogDate = null;

                if (await reader.ReadAsync())
                {
                    if (reader["UserId"] != DBNull.Value)
                        lastUserId = reader["UserId"];

                    if (reader["LogDate"] != DBNull.Value)
                        lastLogDate =
                            Convert.ToDateTime(reader["LogDate"]);
                }

                return Json(new
                {
                    success = true,
                    message = "Biometric database connection successful.",
                    server = serverName,
                    database = databaseName,
                    table = "dbo.DeviceLogs_x_2026",
                    lastUserId = lastUserId,
                    lastLogDate = lastLogDate
                });
            }
            catch (SqlException ex)
            {
                _logger.LogError(
                    ex,
                    "Error while testing biometric database connection.");

                return Json(new
                {
                    success = false,
                    step = "SQL Server",
                    message = ex.Message,
                    errorNumber = ex.Number,
                    state = ex.State,
                    classLevel = ex.Class
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unexpected error while testing biometric database connection.");

                return Json(new
                {
                    success = false,
                    step = "Application",
                    message = ex.Message,
                    error = ex.GetType().Name
                });
            }
        }



        public IActionResult GenerateCertificate()
        {
            // ------------------------------------------------
            // STATIC SAMPLE DATA
            // ------------------------------------------------

            string companyName = "One Point One Solutions Limited";
            string startDate = "14-December-2022";
            string endDate = "13-December-2027";

            // ------------------------------------------------
            // TEMPLATE PATH
            // ------------------------------------------------

            string templatePath = System.IO.Path.Combine(
                _environment.WebRootPath,
                "Templates",
                "ONE_POINT_ONE_SOLUTION_LTD.png"
            );

            if (!System.IO.File.Exists(templatePath))
            {
                return NotFound(
                    "Certificate template not found: " + templatePath
                );
            }

            // ------------------------------------------------
            // CREATE PDF IN MEMORY
            // ------------------------------------------------

            using (MemoryStream outputStream = new MemoryStream())
            {
                // A4 Landscape
                iText.Kernel.Geom.PageSize pageSize = iText.Kernel.Geom.PageSize.A4.Rotate();

                // ------------------------------------------------
                // PDF WRITER
                // ------------------------------------------------

                PdfWriter writer = new PdfWriter(outputStream);

                // ------------------------------------------------
                // PDF DOCUMENT
                // ------------------------------------------------

                PdfDocument pdf = new PdfDocument(writer);

                pdf.SetDefaultPageSize(pageSize);

                // ------------------------------------------------
                // LAYOUT DOCUMENT
                // ------------------------------------------------

                iText.Layout.Document document = new iText.Layout.Document(pdf);

                // Remove default margins
                document.SetMargins(0, 0, 0, 0);

                float pageWidth = pageSize.GetWidth();
                float pageHeight = pageSize.GetHeight();

                // ------------------------------------------------
                // BACKGROUND IMAGE
                // ------------------------------------------------

                ImageData imageData =
                    ImageDataFactory.Create(templatePath);

                Image background =
                    new Image(imageData);

                background
                    .ScaleAbsolute(
                        pageWidth,
                        pageHeight
                    )
                    .SetFixedPosition(
                        0,
                        0
                    );

                document.Add(background);

                // ------------------------------------------------
                // COMPANY NAME
                // ------------------------------------------------

                Paragraph company = new Paragraph(companyName)
                    .SetFontSize(16)
                    .SetFontColor(ColorConstants.BLACK)
                    .SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER)
                    .SetFixedPosition(
                        0,
                        310,
                        pageWidth
                    );

                document.Add(company);

                // ------------------------------------------------
                // DESCRIPTION
                // ------------------------------------------------

                Paragraph description = new Paragraph(
                    "Is an Platinum Industry member of Telecom Sector Skill Council for a period"
                )
                .SetFontSize(12)
                .SetFontColor(ColorConstants.DARK_GRAY)
                .SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER)
                .SetFixedPosition(
                    0,
                    290,
                    pageWidth
                );

                document.Add(description);

                // ------------------------------------------------
                // DATE
                // ------------------------------------------------

                string dateText =
                    $"{startDate}  to  {endDate}";

                Paragraph dates = new Paragraph(dateText)
                    .SetFontSize(14)
                    .SetFontColor(ColorConstants.BLACK)
                    .SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER)
                    .SetFixedPosition(
                        0,
                        270,
                        pageWidth
                    );

                document.Add(dates);

                // ------------------------------------------------
                // CLOSE DOCUMENT
                // ------------------------------------------------

                document.Close();

                byte[] pdfBytes = outputStream.ToArray();

                return File(
                    pdfBytes,
                    "application/pdf",
                    "Platinum_Industry_Membership_Certificate.pdf"
                );
            }
        }

        //[Authorize(Roles = "CEO")]
        [HttpGet]
        public async Task<IActionResult> ApprovalDetails(string source, long id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Account");

            var ceoId = await _approvalService.GetCeoEmployeeIdAsync(userId);

            if (!ceoId.HasValue)
                return Forbid();

            var header = await _approvalService.GetHeaderAsync(
                source, id, ceoId.Value);

            if (header == null)
                return NotFound();

            var model = await _approvalService.GetDetailsAsync(
                header, ceoId.Value);

            return View(model);
        }

        [Authorize(Roles = "CEO")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DecideApproval(DecideApprovalInput input)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Account");

            var ceoId = await _approvalService.GetCeoEmployeeIdAsync(userId);

            if (!ceoId.HasValue)
                return Forbid();

            var result = await _approvalService.DecideAsync(
                input, ceoId.Value);

            TempData[result.Ok ? "success" : "error"] = result.Message;

            if (result.Ok)
            {
                return RedirectToAction(nameof(CEODashboard));
            }

            return RedirectToAction(
                nameof(ApprovalDetails),
                new { source = input.Source, id = input.Id });
        }
        

        private async Task<List<WorkforceEmployeeVm>> GetTodayWorkforceAsync(DateTime today)
        {
            var tomorrow = today.AddDays(1);

            var employees = await _db.Employee
                .AsNoTracking()
                .Where(x => x.IsActive)
                .Select(x => new
                {
                    x.EmployeeId,
                    x.EmployeeCode,
                    Name = (x.FirstName + " " + (x.LastName ?? "")).Trim(),
                    Dept = x.Department.DepartmentName
                })
                .ToListAsync();

            var attendance = await _db.EmployeeAttendance
                .AsNoTracking()
                .Where(x => x.AttendanceDate >= today && x.AttendanceDate < tomorrow)
                .Select(x => new { x.EmployeeId, x.InTime, x.AttendanceSource })
                .ToListAsync();

            var leaveIds = (await _db.LeaveRequest
                .AsNoTracking()
                .Where(x => x.Status == "Approved" && x.FromDate <= today && x.ToDate >= today)
                .Select(x => x.EmployeeId)
                .Distinct()
                .ToListAsync()).ToHashSet();

            var odIds = attendance
                .Where(x => x.AttendanceSource == "OD")
                .Select(x => x.EmployeeId)
                .ToHashSet();

            var firstIn = attendance
                .Where(x => x.InTime.HasValue && x.AttendanceSource != "OD")
                .GroupBy(x => x.EmployeeId)
                .ToDictionary(g => g.Key, g => g.Min(x => x.InTime!.Value));

            var isOffDay = today.DayOfWeek == DayOfWeek.Sunday ||
                await _db.Holiday.AnyAsync(x =>
                    x.HolidayDate >= today && x.HolidayDate < tomorrow && x.Status == "Published");

            var list = new List<WorkforceEmployeeVm>(employees.Count);

            foreach (var e in employees)
            {
                var vm = new WorkforceEmployeeVm
                {
                    EmployeeId = e.EmployeeId,
                    Code = e.EmployeeCode,
                    Name = e.Name,
                    Department = e.Dept
                };

                if (leaveIds.Contains(e.EmployeeId))                       // leave wins
                {
                    vm.Status = AttStatus.OnLeave;
                    vm.Remark = "Approved leave";
                }
                else if (odIds.Contains(e.EmployeeId))                     // then OD
                {
                    vm.Status = AttStatus.OnDuty;
                    vm.Remark = "Working outside office";
                }
                else if (firstIn.TryGetValue(e.EmployeeId, out var inTime)) // then present
                {
                    vm.InTime = inTime.ToString("hh:mm tt");
                    if (inTime.TimeOfDay < LateCutOff)
                    {
                        vm.Status = AttStatus.OnTime;
                    }
                    else
                    {
                        vm.Status = AttStatus.Late;
                        vm.Remark = $"{(int)(inTime.TimeOfDay - LateCutOff).TotalMinutes} min late";
                    }
                }
                else                                                       // nothing marked
                {
                    vm.Status = isOffDay ? AttStatus.OffDay : AttStatus.Absent;
                    vm.Remark = isOffDay ? "Sunday / holiday" : "Not marked";
                }

                list.Add(vm);
            }

            return list;
        }

        [Authorize(Roles = "CEO")]
        [HttpGet]
        public async Task<IActionResult> WorkforceList(string category = "total")
        {
            var all = await GetTodayWorkforceAsync(DateTime.Today);

            IEnumerable<WorkforceEmployeeVm> list = category switch
            {
                "present" => all.Where(e => e.Status is AttStatus.OnTime or AttStatus.Late),
                "ontime" => all.Where(e => e.Status == AttStatus.OnTime),
                "late" => all.Where(e => e.Status == AttStatus.Late),
                "od" => all.Where(e => e.Status == AttStatus.OnDuty),
                "leave" => all.Where(e => e.Status == AttStatus.OnLeave),
                "absent" => all.Where(e => e.Status == AttStatus.Absent),
                _ => all
            };

            return PartialView("_WorkforceList",
                list.OrderBy(e => e.Status).ThenBy(e => e.Name).ToList());
        }
    }

}

