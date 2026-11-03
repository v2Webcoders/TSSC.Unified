
using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using TSSC.Unified.ViewModel;

namespace TSSC.Unified.Areas.HRMS.Controllers
{
    [Area("HRMS")]
    [Authorize(Roles = "HR")]
    public class ReportsController : Controller
    {
        private readonly IConfiguration _configuration;

        public ReportsController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet]
        public IActionResult Attendance()
        {
            return View(new AttendanceReportFilterVM());
        }

        [HttpPost]
        public async Task<IActionResult> ExportAttendanceExcel(
            AttendanceReportFilterVM model)
        {
            if (model.Month < 1 || model.Month > 12 ||
                model.Year < 1900 || model.Year > 9998)
            {
                ModelState.AddModelError(
                    string.Empty, "Please select a valid month and year.");

                return View("Attendance", model);
            }

            var connectionString =
                _configuration.GetConnectionString("DefaultConnectionString");

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    "The DefaultConnection connection string is missing.");
            }

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Attendance Report");

            string[] headers =
            {
                "Employee Code",
                "Employee Name",
                "Attendance Date",
                "In Time",
                "Out Time",
                "Daily Status",
                "Recorded Attendance Status",
                "Attendance Source",
                "Attendance Remarks",
                "Holiday Name",
                "Holiday Type",
                "Leave Request ID",
                "Leave Type ID",
                "Leave Request Status",
                "Requested Leave Days",
                "Leave Reason"
            };

            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cell(1, i + 1).Value = headers[i];
            }

            var header = worksheet.Range(1, 1, 1, headers.Length);
            header.Style.Font.Bold = true;
            header.Style.Font.FontColor = XLColor.White;
            header.Style.Fill.BackgroundColor =
                XLColor.FromHtml("#1B2A4A");
            header.Style.Alignment.WrapText = true;

            int row = 2;

            await using (var connection =
                new SqlConnection(connectionString))
            await using (var command = new SqlCommand(
                "dbo.GetEmployeeAttendanceReport", connection))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.CommandTimeout = 120;

                command.Parameters.Add("@Year", SqlDbType.Int).Value =
                    model.Year;

                command.Parameters.Add("@Month", SqlDbType.Int).Value =
                    model.Month;

                await connection.OpenAsync();

                await using var reader =
                    await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    worksheet.Cell(row, 1).Value =
                        GetText(reader, "EmployeeCode");

                    worksheet.Cell(row, 2).Value =
                        GetText(reader, "EmployeeName");

                    var dateValue = reader["AttendanceDate"];

                    if (dateValue != DBNull.Value)
                    {
                        worksheet.Cell(row, 3).Value =
                            Convert.ToDateTime(dateValue);

                        worksheet.Cell(row, 3).Style.DateFormat.Format =
                            "dd-MMM-yyyy";
                    }

                    worksheet.Cell(row, 4).Value =
                        GetTime(reader, "InTime");

                    worksheet.Cell(row, 5).Value =
                        GetTime(reader, "OutTime");

                    worksheet.Cell(row, 6).Value =
                        GetText(reader, "DailyStatus");

                    worksheet.Cell(row, 7).Value =
                        GetText(reader, "RecordedAttendanceStatus");

                    worksheet.Cell(row, 8).Value =
                        GetText(reader, "AttendanceSource");

                    worksheet.Cell(row, 9).Value =
                        GetText(reader, "AttendanceRemarks");

                    worksheet.Cell(row, 10).Value =
                        GetText(reader, "HolidayName");

                    worksheet.Cell(row, 11).Value =
                        GetText(reader, "HolidayType");

                    worksheet.Cell(row, 12).Value =
                        GetText(reader, "LeaveRequestId");

                    worksheet.Cell(row, 13).Value =
                        GetText(reader, "LeaveTypeId");

                    worksheet.Cell(row, 14).Value =
                        GetText(reader, "LeaveRequestStatus");

                    worksheet.Cell(row, 15).Value =
                        GetText(reader, "RequestedLeaveDays");

                    worksheet.Cell(row, 16).Value =
                        GetText(reader, "LeaveReason");

                    row++;
                }
            }

            worksheet.SheetView.FreezeRows(1);
            worksheet.RangeUsed()?.SetAutoFilter();
            worksheet.Columns().AdjustToContents();

            // Keep long remarks and leave reasons readable.
            worksheet.Column(9).Width = 35;
            worksheet.Column(16).Width = 35;
            worksheet.Column(9).Style.Alignment.WrapText = true;
            worksheet.Column(16).Style.Alignment.WrapText = true;

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);

            var fileName =
                $"Attendance_Report_{model.Year}_{model.Month:00}.xlsx";

            return File(
                stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }

        private static string GetText(
            SqlDataReader reader, string column)
        {
            if (reader[column] == DBNull.Value)
                return string.Empty;

            return Convert.ToString(reader[column]) ?? string.Empty;
        }

        private static string GetTime(
            SqlDataReader reader, string column)
        {
            if (reader[column] == DBNull.Value)
                return string.Empty;

            var value = reader[column];

            if (value is TimeSpan timeSpan)
                return timeSpan.ToString(@"hh\:mm");

            if (value is DateTime dateTime)
                return dateTime.ToString("HH:mm");

            return Convert.ToString(value) ?? string.Empty;
        }
    }
}

