using ClosedXML.Excel;
using DocumentFormat.OpenXml.InkML;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QUIZAPP.Models;
using QUIZAPP.ViewModel;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Security.Claims;

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

        public async Task<IActionResult> Index()
        {
            var applicationUserId =
                User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var employee = await _db.Employee
                .FirstOrDefaultAsync(x =>
                    x.ApplicationUserId == applicationUserId);

            ViewBag.EmployeeName = employee?.FirstName ?? "Employee";

            if (User.IsInRole("HR"))
            {
                ViewBag.EmployeeName = employee?.FirstName ?? "HR Manager";
                return View("Index");
            }

            if (User.IsInRole("Employee"))
            {
                if (employee == null)
                    return NotFound();

                ViewBag.EmployeeCode = employee.EmployeeCode;

                var attendance = await GetTodayAttendance(
                    employee.EmployeeCode);

                ViewBag.TodayInTime = attendance.InTime;
                ViewBag.TodayOutTime = attendance.OutTime;

                return View("EmployeeDashboard");
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

        private async Task<(DateTime? InTime, DateTime? OutTime)> GetTodayAttendance(
    string employeeCode)
        {
            DateTime? inTime = null;
            DateTime? outTime = null;

            var connectionString =
                _configuration.GetConnectionString("BiometricConnection");

            using var connection = new SqlConnection(connectionString);

            await connection.OpenAsync();

            var sql = @"
        SELECT
            MIN(LogDate) AS InTime,
            CASE
                WHEN COUNT(*) > 1 THEN MAX(LogDate)
                ELSE NULL
            END AS OutTime
        FROM etimetracklite1.dbo.DeviceLogs_8_2026
        WHERE UserId = @EmployeeCode
          AND CAST(LogDate AS DATE) = @Today";

            using var command = new SqlCommand(sql, connection);

            command.Parameters.AddWithValue(
                "@EmployeeCode",
                employeeCode);

            command.Parameters.AddWithValue(
                "@Today",
                DateTime.Today);

            using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                if (reader["InTime"] != DBNull.Value)
                    inTime = Convert.ToDateTime(reader["InTime"]);

                if (reader["OutTime"] != DBNull.Value)
                    outTime = Convert.ToDateTime(reader["OutTime"]);
            }

            return (inTime, outTime);
        }
    }
}

