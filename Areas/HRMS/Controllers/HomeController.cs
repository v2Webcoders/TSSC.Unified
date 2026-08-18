using ClosedXML.Excel;
using DocumentFormat.OpenXml.InkML;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QUIZAPP.Models;
using QUIZAPP.ViewModel;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Security.Claims;
using TSSC.Unified.ViewModel;

namespace QUIZAPP.Areas.HRMS.Controllers
{
    [Area("HRMS")]
    [Authorize(Roles = "HR, Employee")]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly AppdbContext _db;
        private readonly IConfiguration _configuration;
        public HomeController(ILogger<HomeController> logger, AppdbContext db, IConfiguration configuration)
        {
            _logger = logger;
            _db = db;
            _configuration = configuration;
        }

        //public async Task<IActionResult> Index()
        //{
        //    var applicationUserId =
        //        User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        //    var employee = await _db.Employee
        //        .FirstOrDefaultAsync(x =>
        //            x.ApplicationUserId == applicationUserId);

        //    ViewBag.EmployeeName = employee?.FirstName ?? "Employee";
        //    ViewBag.EmployeeCode = employee?.EmployeeCode;
        //    if (User.IsInRole("HR"))
        //    {
        //        var attendance = await GetTodayAttendance(
        //            employee.EmployeeCode);

        //        ViewBag.TodayInTime = attendance.InTime;
        //        ViewBag.TodayOutTime = attendance.OutTime;
        //        return View("Index");
        //    }

        //    if (User.IsInRole("Employee"))
        //    {
        //        if (employee == null)
        //            return NotFound();

        //        var attendance = await GetTodayAttendance(
        //            employee.EmployeeCode);

        //        ViewBag.TodayInTime = attendance.InTime;
        //        ViewBag.TodayOutTime = attendance.OutTime;

        //        return View("EmployeeDashboard");
        //    }

        //    return View();
        //}

        public async Task<IActionResult> Index()
        {
            var applicationUserId =
                User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(applicationUserId))
                return RedirectToAction("Login", "Account");

            // =====================================================
            // LOGGED-IN EMPLOYEE
            // =====================================================

            var employee =
                await _db.Employee
                    .FirstOrDefaultAsync(x =>
                        x.ApplicationUserId == applicationUserId);


            // =====================================================
            // HR DASHBOARD
            // =====================================================
            
            if (User.IsInRole("HR"))
            {
                var today = DateTime.Today;


                var model = new HRDashboardVM
                {
                    EmployeeName =
               employee?.FirstName ?? "HR",

                    EmployeeCode =
               employee?.EmployeeCode
                };

                // =================================================
                // TODAY'S ATTENDANCE FOR LOGGED-IN HR
                // =================================================

                if (employee != null)
                {
                    var attendance =
                        await GetTodayAttendance(
                            employee.EmployeeCode);

                    model.TodayInTime =
                        attendance.InTime;

                    model.TodayOutTime =
                        attendance.OutTime;
                }


                // =================================================
                // TOTAL EMPLOYEES
                // =================================================

                model.TotalEmployees =
                    await _db.Employee.CountAsync();


                // =================================================
                // PRESENT TODAY
                // =================================================

                model.PresentToday =
                    await _db.EmployeeAttendance
                        .CountAsync(x =>
                            x.AttendanceDate == today &&
                            x.InTime != null);


                // =================================================
                // EMPLOYEES ON LEAVE TODAY
                // =================================================

                model.EmployeesOnLeave =
                    await _db.LeaveRequest
                        .CountAsync(x =>
                            x.Status == "Approved" &&
                            x.FromDate <= today &&
                            x.ToDate >= today);


                // =================================================
                // PENDING LEAVE REQUESTS
                // =================================================

                model.PendingLeaveRequests =
                    await _db.LeaveRequest
                        .CountAsync(x =>
                            x.Status == "Pending");


                // =================================================
                // RECENT LEAVE REQUESTS
                // =================================================

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
                                    ? x.Employee.FirstName +
                                      " " +
                                      x.Employee.LastName
                                    : "Unknown",

                            LeaveType =
                                x.LeaveType != null
                                    ? x.LeaveType.LeaveTypeName
                                    : "Unknown",

                            FromDate =
                                x.FromDate,

                            ToDate =
                                x.ToDate,

                            Status =
                                x.Status
                        })
                        .ToListAsync();


                return View("Index", model);
            }


            // =====================================================
            // EMPLOYEE DASHBOARD
            // =====================================================

            if (User.IsInRole("Employee"))
            {
                if (employee == null)
                    return NotFound();

                var today = DateTime.Today;

                var attendance =
                    await GetTodayAttendance(
                        employee.EmployeeCode);


                var model = new HRDashboardVM
                {
                    EmployeeName =
                        employee.FirstName,

                    EmployeeCode =
                        employee.EmployeeCode,

                    TodayInTime =
                        attendance.InTime,

                    TodayOutTime =
                        attendance.OutTime
                };


                // =================================================
                // CURRENT MONTH
                // =================================================

                var firstDayOfMonth =
                    new DateTime(
                        today.Year,
                        today.Month,
                        1);

                var lastDayOfMonth =
                    firstDayOfMonth.AddMonths(1).AddDays(-1);


                // =================================================
                // PRESENT DAYS THIS MONTH
                // =================================================

                model.PresentDaysThisMonth =
                    await _db.EmployeeAttendance
                        .CountAsync(x =>
                            x.EmployeeId == employee.EmployeeId &&
                            x.AttendanceDate >= firstDayOfMonth &&
                            x.AttendanceDate <= lastDayOfMonth &&
                            x.InTime != null);


                // =================================================
                // WORKING DAYS THIS MONTH
                // =================================================

                var workingDays = 0;

                for (
                    var date = firstDayOfMonth;
                    date <= today;
                    date = date.AddDays(1))
                {
                    if (date.DayOfWeek != DayOfWeek.Sunday)
                    {
                        workingDays++;
                    }
                }

                model.WorkingDaysThisMonth =
                    workingDays;


                // =================================================
                // PENDING LEAVE REQUESTS
                // =================================================

                model.PendingRequests =
                    await _db.LeaveRequest
                        .CountAsync(x =>
                            x.EmployeeId == employee.EmployeeId &&
                            x.Status == "Pending");


                // =================================================
                // LEAVE BALANCE
                // =================================================

                // Replace this with your actual LeaveBalance table
                // once you provide its structure.

                model.LeaveBalance = 0;


                // =================================================
                // ATTENDANCE TO REGULARIZE
                // =================================================

                model.AttendanceToRegularize =
                    await _db.EmployeeAttendance
                        .CountAsync(x =>
                            x.EmployeeId == employee.EmployeeId &&
                            x.AttendanceDate >= firstDayOfMonth &&
                            x.AttendanceDate <= today &&
                            x.InTime == null);


                // =================================================
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

                            RequestDate =
                                x.CreatedDate,

                            Status =
                                x.Status
                        })
                        .ToListAsync();


                return View(
                    "EmployeeDashboard",
                    model);
            }
            return View();
        }
        public IActionResult EmployeeDashboard()
        {
            return View();
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

        //private async Task<(DateTime? InTime, DateTime? OutTime)> GetTodayAttendance(
        //string employeeCode)
        //{
        //    DateTime? inTime = null;
        //    DateTime? outTime = null;

        //    var connectionString =
        //        _configuration.GetConnectionString("BiometricConnection");

        //    using var connection = new SqlConnection(connectionString);

        //    await connection.OpenAsync();

        //    var sql = @"
        //SELECT
        //    MIN(LogDate) AS InTime,
        //    CASE
        //        WHEN COUNT(*) > 1 THEN MAX(LogDate)
        //        ELSE NULL
        //    END AS OutTime
        //FROM etimetracklite1.dbo.DeviceLogs_8_2026
        //WHERE UserId = @EmployeeCode
        //  AND CAST(LogDate AS DATE) = @Today";

        //    using var command = new SqlCommand(sql, connection);

        //    command.Parameters.AddWithValue(
        //        "@EmployeeCode",
        //        employeeCode);

        //    command.Parameters.AddWithValue(
        //        "@Today",
        //        DateTime.Today);

        //    using var reader = await command.ExecuteReaderAsync();

        //    if (await reader.ReadAsync())
        //    {
        //        if (reader["InTime"] != DBNull.Value)
        //            inTime = Convert.ToDateTime(reader["InTime"]);

        //        if (reader["OutTime"] != DBNull.Value)
        //            outTime = Convert.ToDateTime(reader["OutTime"]);
        //    }

        //    return (inTime, outTime);
        //}
        private async Task<(DateTime? InTime, DateTime? OutTime)> GetTodayAttendance(
        string employeeCode)
        {
            DateTime? inTime = null;
            DateTime? outTime = null;

            try
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

                var sql = @"
            SELECT
                MIN(LogDate) AS InTime,

                CASE
                    WHEN MAX(LogDate) >= DATEADD(
                        MINUTE,
                        30,
                        MIN(LogDate)
                    )
                    THEN MAX(LogDate)

                    ELSE NULL
                END AS OutTime

            FROM etimetracklite1.dbo.DeviceLogs_8_2026

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
            catch (SqlException ex)
            {
                _logger.LogError(
                    ex,
                    "Biometric database error while getting attendance for EmployeeCode: {EmployeeCode}",
                    employeeCode);

                return (null, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unexpected error while getting biometric attendance for EmployeeCode: {EmployeeCode}",
                    employeeCode);

                return (null, null);
            }
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
            FROM etimetracklite1.dbo.DeviceLogs_8_2026
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
                    table = "etimetracklite1.dbo.DeviceLogs_8_2026",
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
    }
}

