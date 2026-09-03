using System.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using QUIZAPP;
using TSSC.Unified.Models;
using TSSC.Unified.ViewModel;

namespace TSSC.Unified.Services
{
    public class AttendanceSyncService : IAttendanceSyncService
    {
        private readonly AppdbContext _hrmsContext;
        private readonly IConfiguration _configuration;

        public AttendanceSyncService(
            AppdbContext hrmsContext,
            IConfiguration configuration)
        {
            _hrmsContext = hrmsContext;
            _configuration = configuration;
        }

        public async Task<AttendanceSyncResultVM>
            SyncBiometricAttendanceAsync(
                DateTime fromDate,
                DateTime toDate)
        {
            var result = new AttendanceSyncResultVM();

            // =====================================================
            // BIOMETRIC CONNECTION
            // =====================================================

            var connectionString =
                _configuration.GetConnectionString(
                    "BiometricConnection");

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new Exception(
                    "BiometricConnection is not configured.");
            }


            // =====================================================
            // DATE RANGE
            // =====================================================

            var startDate = fromDate.Date;

            // Exclusive upper boundary
            var endDate = toDate.Date.AddDays(1);


            // =====================================================
            // OPTIMIZED SQL QUERY
            // =====================================================

            //    const string sql = @"
            //    SELECT
            //        UserId,
            //        CAST(LogDate AS DATE) AS AttendanceDate,
            //        MIN(LogDate) AS InTime,
            //        MAX(LogDate) AS OutTime,
            //        COUNT(*) AS PunchCount
            //    FROM DeviceLogs_8_2026
            //    WHERE LogDate >= @FromDate
            //      AND LogDate < @ToDate
            //    GROUP BY
            //        UserId,
            //        CAST(LogDate AS DATE)
            //    ORDER BY
            //        AttendanceDate,
            //        UserId;
            //";


            // =====================================================
            // BIOMETRIC TABLE NAME
            // Example: DeviceLogs_8_2026
            // =====================================================

            var tableName =
                $"DeviceLogs_{startDate.Month}_{startDate.Year}";


            // =====================================================
            // SQL
            // =====================================================

                var sql = $@"
                SELECT
                    UserId,
                    CAST(LogDate AS DATE) AS AttendanceDate,
                    MIN(LogDate) AS InTime,
                    MAX(LogDate) AS OutTime,
                    COUNT(*) AS PunchCount
                FROM [{tableName}]
                WHERE LogDate >= @FromDate
                  AND LogDate < @ToDate
                GROUP BY
                    UserId,
                    CAST(LogDate AS DATE)
                ORDER BY
                    AttendanceDate,
                    UserId;
            ";

            // =====================================================
            // READ FROM BIOMETRIC DATABASE
            // =====================================================

            await using var connection =
                new SqlConnection(connectionString);

            await connection.OpenAsync();

            await using var command =
                new SqlCommand(sql, connection);

            command.Parameters.Add(
                new SqlParameter("@FromDate", startDate));

            command.Parameters.Add(
                new SqlParameter("@ToDate", endDate));


            await using var reader =
                await command.ExecuteReaderAsync();


            // =====================================================
            // PROCESS ONE EMPLOYEE/DAY AT A TIME
            // =====================================================

            while (await reader.ReadAsync())
            {
                result.TotalBiometricRecords++;

                // =================================================
                // BIOMETRIC USER ID -> EMPLOYEE CODE
                // =================================================

                string employeeCode =
                    reader["UserId"]?.ToString()?.Trim();

                if (string.IsNullOrWhiteSpace(employeeCode))
                {
                    result.UnmappedEmployees++;
                    continue;
                }


                // =================================================
                // ATTENDANCE DATE
                // =================================================

                DateTime attendanceDate =
                    reader.GetDateTime(
                        reader.GetOrdinal("AttendanceDate"));


                // =================================================
                // FIRST PUNCH
                // =================================================

                DateTime inTime =
                    reader.GetDateTime(
                        reader.GetOrdinal("InTime"));


                // =================================================
                // LAST PUNCH
                // =================================================

                DateTime outTime =
                    reader.GetDateTime(
                        reader.GetOrdinal("OutTime"));


                // =================================================
                // PUNCH COUNT
                // =================================================

                int punchCount =
                    Convert.ToInt32(reader["PunchCount"]);


                // =================================================
                // FIND EMPLOYEE
                // =================================================

                var employee =
                    await _hrmsContext.Employee
                        .FirstOrDefaultAsync(x =>
                            x.EmployeeCode != null &&
                            x.EmployeeCode.Trim() == employeeCode);

                if (employee == null)
                {
                    result.UnmappedEmployees++;

                    Console.WriteLine(
                        $"Unmapped biometric UserId: [{employeeCode}]");

                    continue;
                }

                result.EmployeesProcessed++;


                // =================================================
                // DETERMINE MISSING PUNCH
                // =================================================

                bool missingPunch = punchCount == 1;

                if (missingPunch)
                {
                    result.MissingPunch++;
                }


                // =================================================
                // EXTRA / DUPLICATE PUNCHES
                // =================================================

                if (punchCount > 2)
                {
                    result.DuplicatePunches += punchCount - 2;
                }


                // =================================================
                // FIND EXISTING ATTENDANCE
                // =================================================

                var attendance =
                    await _hrmsContext.EmployeeAttendance
                        .FirstOrDefaultAsync(x =>
                            x.EmployeeId == employee.EmployeeId &&
                            x.AttendanceDate == attendanceDate);


                // =================================================
                // CREATE NEW ATTENDANCE
                // =================================================

                if (attendance == null)
                {
                    attendance = new EmployeeAttendance
                    {
                        EmployeeId =
                            employee.EmployeeId,

                        EmployeeCode =
                            employee.EmployeeCode,

                        AttendanceDate =
                            attendanceDate,

                        InTime =
                            inTime,

                        OutTime =
                            missingPunch
                                ? null
                                : outTime,

                        AttendanceStatus =
                        missingPunch
                            ? "Present - Check Out Missing"
                            : "Present",

                                            Remarks =
                        missingPunch
                            ? "Employee checked in but has not checked out."
                            : "Attendance synchronized from biometric device.",

                        AttendanceSource =
                            "Biometric",

                        

                        CreatedDate =
                            DateTime.Now
                    };

                    _hrmsContext.EmployeeAttendance.Add(attendance);

                    result.AttendanceCreated++;
                }


                // =================================================
                // UPDATE EXISTING ATTENDANCE
                // =================================================

                else
                {
                    attendance.EmployeeCode =
                        employee.EmployeeCode;

                    attendance.InTime =
                        inTime;

                    attendance.OutTime =
                        missingPunch
                            ? null
                            : outTime;

                    attendance.AttendanceStatus =
                        missingPunch
                            ? "Present - Check Out Missing"
                            : "Present";

                    attendance.Remarks =
                        missingPunch
                            ? "Employee checked in but has not checked out."
                            : "Attendance synchronized from biometric device.";

                    attendance.AttendanceSource =
                        "Biometric";
                    result.AttendanceUpdated++;
                }
            }


            // =====================================================
            // SAVE TO HRMS DATABASE
            // =====================================================

            await _hrmsContext.SaveChangesAsync();

            return result;
        }

        public Task<AttendanceSyncResultVM> SyncBiometricAttendanceAsync()
        {
            throw new NotImplementedException();
        }
    }

}
