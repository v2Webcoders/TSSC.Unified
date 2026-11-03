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

        //public async Task<AttendanceSyncResultVM>
        //    SyncBiometricAttendanceAsync(
        //        DateTime fromDate,
        //        DateTime toDate)
        //{
        //    var result = new AttendanceSyncResultVM();

        //    // =====================================================
        //    // BIOMETRIC CONNECTION
        //    // =====================================================

        //    var connectionString =
        //        _configuration.GetConnectionString(
        //            "BiometricConnection");

        //    if (string.IsNullOrWhiteSpace(connectionString))
        //    {
        //        throw new Exception(
        //            "BiometricConnection is not configured.");
        //    }


        //    // =====================================================
        //    // DATE RANGE
        //    // =====================================================

        //    var startDate = fromDate.Date;

        //    // Exclusive upper boundary
        //    var endDate = toDate.Date.AddDays(1);


        //    // =====================================================
        //    // OPTIMIZED SQL QUERY
        //    // =====================================================

        //    //    const string sql = @"
        //    //    SELECT
        //    //        UserId,
        //    //        CAST(LogDate AS DATE) AS AttendanceDate,
        //    //        MIN(LogDate) AS InTime,
        //    //        MAX(LogDate) AS OutTime,
        //    //        COUNT(*) AS PunchCount
        //    //    FROM DeviceLogs_8_2026
        //    //    WHERE LogDate >= @FromDate
        //    //      AND LogDate < @ToDate
        //    //    GROUP BY
        //    //        UserId,
        //    //        CAST(LogDate AS DATE)
        //    //    ORDER BY
        //    //        AttendanceDate,
        //    //        UserId;
        //    //";


        //    // =====================================================
        //    // BIOMETRIC TABLE NAME
        //    // Example: DeviceLogs_8_2026
        //    // =====================================================

        //    var tableName =
        //        $"DeviceLogs_{startDate.Month}_{startDate.Year}";


        //    // =====================================================
        //    // SQL
        //    // =====================================================

        //    var sql = $@"
        //        SELECT
        //            UserId,
        //            CAST(LogDate AS DATE) AS AttendanceDate,
        //            MIN(LogDate) AS InTime,
        //            MAX(LogDate) AS OutTime,
        //            COUNT(*) AS PunchCount
        //        FROM [{tableName}]
        //        WHERE LogDate >= @FromDate
        //          AND LogDate < @ToDate
        //        GROUP BY
        //            UserId,
        //            CAST(LogDate AS DATE)
        //        ORDER BY
        //            AttendanceDate,
        //            UserId;
        //    ";

        //    // =====================================================
        //    // READ FROM BIOMETRIC DATABASE
        //    // =====================================================

        //    await using var connection =
        //        new SqlConnection(connectionString);

        //    await connection.OpenAsync();

        //    await using var command =
        //        new SqlCommand(sql, connection);

        //    command.Parameters.Add(
        //        new SqlParameter("@FromDate", startDate));

        //    command.Parameters.Add(
        //        new SqlParameter("@ToDate", endDate));


        //    await using var reader =
        //        await command.ExecuteReaderAsync();


        //    // =====================================================
        //    // PROCESS ONE EMPLOYEE/DAY AT A TIME
        //    // =====================================================

        //    while (await reader.ReadAsync())
        //    {
        //        result.TotalBiometricRecords++;

        //        // =================================================
        //        // BIOMETRIC USER ID -> EMPLOYEE CODE
        //        // =================================================

        //        string employeeCode =
        //            reader["UserId"]?.ToString()?.Trim();

        //        if (string.IsNullOrWhiteSpace(employeeCode))
        //        {
        //            result.UnmappedEmployees++;
        //            continue;
        //        }


        //        // =================================================
        //        // ATTENDANCE DATE
        //        // =================================================

        //        DateTime attendanceDate =
        //            reader.GetDateTime(
        //                reader.GetOrdinal("AttendanceDate"));


        //        // =================================================
        //        // FIRST PUNCH
        //        // =================================================

        //        DateTime inTime =
        //            reader.GetDateTime(
        //                reader.GetOrdinal("InTime"));


        //        // =================================================
        //        // LAST PUNCH
        //        // =================================================

        //        DateTime outTime =
        //            reader.GetDateTime(
        //                reader.GetOrdinal("OutTime"));


        //        // =================================================
        //        // PUNCH COUNT
        //        // =================================================

        //        int punchCount =
        //            Convert.ToInt32(reader["PunchCount"]);

        //        // =================================================
        //        // DETERMINE ATTENDANCE STATUS
        //        // Based on FIRST BIOMETRIC PUNCH
        //        // =================================================

        //        string baseAttendanceStatus;

        //        if (inTime.TimeOfDay < new TimeSpan(10, 0, 0))
        //        {
        //            // Before 10:00 AM
        //            baseAttendanceStatus = "Present";
        //        }
        //        else if (inTime.TimeOfDay < new TimeSpan(11, 0, 0))
        //        {
        //            // 10:00 AM - 10:59 AM
        //            baseAttendanceStatus = "Late";
        //        }
        //        else
        //        {
        //            // 11:00 AM onwards
        //            baseAttendanceStatus = "Half Day";
        //        }




        //        // =================================================
        //        // FIND EMPLOYEE
        //        // =================================================

        //        var employee =
        //            await _hrmsContext.Employee
        //                .FirstOrDefaultAsync(x =>
        //                    x.BiometricCode != null &&
        //                    x.BiometricCode.Trim() == employeeCode);

        //        if (employee == null)
        //        {
        //            result.UnmappedEmployees++;

        //            Console.WriteLine(
        //                $"Unmapped biometric UserId: [{employeeCode}]");

        //            continue;
        //        }

        //        result.EmployeesProcessed++;


        //        // =================================================
        //        // DETERMINE MISSING PUNCH
        //        // =================================================

        //        bool missingPunch = punchCount == 1;

        //        if (missingPunch)
        //        {
        //            result.MissingPunch++;
        //        }


        //        // =================================================
        //        // EXTRA / DUPLICATE PUNCHES
        //        // =================================================

        //        if (punchCount > 2)
        //        {
        //            result.DuplicatePunches += punchCount - 2;
        //        }

        //        string attendanceStatus =
        //        missingPunch
        //       ? $"{baseAttendanceStatus} - Check Out Missing"
        //       : baseAttendanceStatus;

        //        string remarks =
        //        missingPunch
        //       ? $"Employee checked in at {inTime:hh:mm tt} " +
        //         "but has not checked out."
        //       : $"Attendance synchronized from biometric device. " +
        //         $"Check-in: {inTime:hh:mm tt}";


        //        // =================================================
        //        // FIND EXISTING ATTENDANCE
        //        // =================================================

        //        var attendance =
        //            await _hrmsContext.EmployeeAttendance
        //                .FirstOrDefaultAsync(x =>
        //                    x.EmployeeId == employee.EmployeeId &&
        //                    x.AttendanceDate == attendanceDate);


        //        // =================================================
        //        // CREATE NEW ATTENDANCE
        //        // =================================================

        //        if (attendance == null)
        //        {
        //            attendance = new EmployeeAttendance
        //            {
        //                EmployeeId =
        //                    employee.EmployeeId,

        //                EmployeeCode =
        //                    employee.EmployeeCode,

        //                AttendanceDate =
        //                    attendanceDate,

        //                InTime =
        //                    inTime,

        //                OutTime =
        //                    missingPunch
        //                        ? null
        //                        : outTime,

        //                //AttendanceStatus =
        //                //missingPunch
        //                //    ? "Present - Check Out Missing"
        //                //    : "Present",

        //                //                    Remarks =
        //                //missingPunch
        //                //    ? "Employee checked in but has not checked out."
        //                //    : "Attendance synchronized from biometric device.",
        //                AttendanceStatus =
        //                attendanceStatus,

        //                Remarks =
        //                remarks,
        //                AttendanceSource =
        //                    "Biometric",



        //                CreatedDate =
        //                    DateTime.Now
        //            };

        //            _hrmsContext.EmployeeAttendance.Add(attendance);

        //            result.AttendanceCreated++;
        //        }


        //        // =================================================
        //        // UPDATE EXISTING ATTENDANCE
        //        // =================================================

        //        else
        //        {
        //            // Do not overwrite Manual, Regularization, or OD attendance
        //            if (attendance.AttendanceSource == "Manual" ||
        //                attendance.AttendanceSource == "Regularization" ||
        //                attendance.AttendanceSource == "OD")
        //            {
        //                continue;
        //            }
        //            attendance.EmployeeCode =
        //                employee.EmployeeCode;

        //            attendance.InTime =
        //                inTime;

        //            attendance.OutTime =
        //                missingPunch
        //                    ? null
        //                    : outTime;
        //            attendance.AttendanceStatus = attendanceStatus;
        //            attendance.Remarks =
        //                missingPunch
        //                    ? "Employee checked in but has not checked out."
        //                    : "Attendance synchronized from biometric device.";

        //            attendance.AttendanceSource =
        //                "Biometric";
        //            result.AttendanceUpdated++;
        //        }
        //    }


        //    // =====================================================
        //    // SAVE TO HRMS DATABASE
        //    // =====================================================

        //    await _hrmsContext.SaveChangesAsync();

        //    return result;
        //}


        public async Task<AttendanceSyncResultVM> SyncBiometricAttendanceAsync(
        DateTime fromDate,
        DateTime toDate)
        {
            var result = new AttendanceSyncResultVM();

            var connectionString = _configuration.GetConnectionString("BiometricConnection");
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new Exception("BiometricConnection is not configured.");

            var startDate = fromDate.Date;
            var endDate = toDate.Date.AddDays(1);      // exclusive

            // =====================================================
            // PRELOAD (instead of 2 queries per punch row)
            // =====================================================
            var employees = await _hrmsContext.Employee
                .AsNoTracking()
                .Where(x => x.BiometricCode != null)
                .Select(x => new { x.EmployeeId, x.EmployeeCode, x.BiometricCode })
                .ToListAsync();

            var employeeByBioCode = employees
                .GroupBy(x => x.BiometricCode!.Trim())
                .ToDictionary(g => g.Key, g => g.First());

            var existingList = await _hrmsContext.EmployeeAttendance
                .Where(x => x.AttendanceDate >= startDate && x.AttendanceDate < endDate)
                .ToListAsync();

            var existingByKey = existingList
                .GroupBy(x => (x.EmployeeId, x.AttendanceDate.Date))
                .ToDictionary(g => g.Key, g => g.OrderByDescending(a => a.CreatedDate).First());

            // =====================================================
            // ONE QUERY PER MONTH TABLE IN THE RANGE
            // =====================================================
            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            var segmentStart = startDate;

            while (segmentStart < endDate)
            {
                var monthEnd = new DateTime(segmentStart.Year, segmentStart.Month, 1).AddMonths(1);
                var segmentEnd = monthEnd < endDate ? monthEnd : endDate;

                var tableName = $"DeviceLogs_{segmentStart.Month}_{segmentStart.Year}";

                var sql = $@"
            SELECT
                UserId,
                CAST(LogDate AS DATE) AS AttendanceDate,
                MIN(LogDate) AS InTime,
                MAX(LogDate) AS OutTime,
                COUNT(*)     AS PunchCount
            FROM [{tableName}]
            WHERE LogDate >= @FromDate
              AND LogDate <  @ToDate
            GROUP BY UserId, CAST(LogDate AS DATE)
            ORDER BY AttendanceDate, UserId;";

                var rows = new List<(string UserId, DateTime Date, DateTime In, DateTime Out, int Count)>();

                try
                {
                    await using var command = new SqlCommand(sql, connection);
                    command.Parameters.Add(new SqlParameter("@FromDate", segmentStart));
                    command.Parameters.Add(new SqlParameter("@ToDate", segmentEnd));

                    await using var reader = await command.ExecuteReaderAsync();

                    while (await reader.ReadAsync())
                    {
                        rows.Add((
                            reader["UserId"]?.ToString()?.Trim() ?? "",
                            reader.GetDateTime(reader.GetOrdinal("AttendanceDate")),
                            reader.GetDateTime(reader.GetOrdinal("InTime")),
                            reader.GetDateTime(reader.GetOrdinal("OutTime")),
                            Convert.ToInt32(reader["PunchCount"])));
                    }
                }
                catch (SqlException ex) when (ex.Number == 208)
                {
                    // Monthly table doesn't exist yet (e.g. early on the 1st): nothing to sync for this segment
                    //_logger.LogWarning("Biometric table [{Table}] not found, skipping {From:d} to {To:d}",
                        //tableName, segmentStart, segmentEnd.AddDays(-1));

                    segmentStart = segmentEnd;
                    continue;
                }

               // _logger.LogInformation("Biometric table [{Table}]: {Count} employee-day rows", tableName, rows.Count);

                // =================================================
                // PROCESS ROWS
                // =================================================
                foreach (var row in rows)
                {
                    result.TotalBiometricRecords++;

                    if (string.IsNullOrWhiteSpace(row.UserId))
                    {
                        result.UnmappedEmployees++;
                        continue;
                    }

                    if (!employeeByBioCode.TryGetValue(row.UserId, out var employee))
                    {
                        result.UnmappedEmployees++;
                       // _logger.LogWarning("Unmapped biometric UserId: [{UserId}]", row.UserId);
                        continue;
                    }

                    result.EmployeesProcessed++;

                    var inTime = row.In;
                    var outTime = row.Out;

                    // Status from the first punch
                    string baseStatus =
                        inTime.TimeOfDay < new TimeSpan(10, 0, 0) ? "Present" :
                        inTime.TimeOfDay < new TimeSpan(11, 0, 0) ? "Late" :
                                                                    "Half Day";

                    bool missingPunch = row.Count == 1;
                    if (missingPunch) result.MissingPunch++;
                    if (row.Count > 2) result.DuplicatePunches += row.Count - 2;

                    string attendanceStatus = missingPunch ? $"{baseStatus} - Check Out Missing" : baseStatus;

                    string remarks = missingPunch
                        ? $"Employee checked in at {inTime:hh:mm tt} but has not checked out."
                        : $"Attendance synchronized from biometric device. Check-in: {inTime:hh:mm tt}";

                    var key = (employee.EmployeeId, row.Date.Date);

                    if (!existingByKey.TryGetValue(key, out var attendance))
                    {
                        attendance = new EmployeeAttendance
                        {
                            EmployeeId = employee.EmployeeId,
                            EmployeeCode = employee.EmployeeCode,
                            AttendanceDate = row.Date.Date,
                            InTime = inTime,
                            OutTime = missingPunch ? null : outTime,
                            AttendanceStatus = attendanceStatus,
                            Remarks = remarks,
                            AttendanceSource = "Biometric",
                            CreatedDate = DateTime.Now
                        };

                        _hrmsContext.EmployeeAttendance.Add(attendance);
                        existingByKey[key] = attendance;
                        result.AttendanceCreated++;
                    }
                    else
                    {
                        // Don't overwrite Manual, Regularization or OD attendance
                        if (attendance.AttendanceSource == "Manual" ||
                            attendance.AttendanceSource == "Regularization" ||
                            attendance.AttendanceSource == "OD")
                            continue;

                        var newOut = missingPunch ? (DateTime?)null : outTime;

                        bool changed =
                            attendance.InTime != inTime ||
                            attendance.OutTime != newOut ||
                            attendance.AttendanceStatus != attendanceStatus ||
                            attendance.AttendanceSource != "Biometric";

                        if (!changed) continue;     // nothing new: don't count it as updated

                        attendance.EmployeeCode = employee.EmployeeCode;
                        attendance.InTime = inTime;
                        attendance.OutTime = newOut;
                        attendance.AttendanceStatus = attendanceStatus;
                        attendance.Remarks = remarks;
                        attendance.AttendanceSource = "Biometric";

                        result.AttendanceUpdated++;
                    }
                }

                segmentStart = segmentEnd;
            }

            await _hrmsContext.SaveChangesAsync();

            return result;
        }

        public Task<AttendanceSyncResultVM> SyncBiometricAttendanceAsync()
        {
            throw new NotImplementedException();
        }
    }

}
