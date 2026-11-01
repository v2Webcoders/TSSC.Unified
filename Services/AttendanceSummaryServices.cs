using Microsoft.EntityFrameworkCore;
using QUIZAPP;
using TSSC.Unified.Models;
using TSSC.Unified.ViewModel;

namespace TSSC.Unified.Services
{
    public class AttendanceSummaryService : IAttendanceSummaryService
    {
        private readonly AppdbContext _context;

        public AttendanceSummaryService(AppdbContext context)
        {
            _context = context;
        }
        //public static class AttendanceRules
        //{
        //    public static readonly TimeSpan LateFrom = new(10, 0, 0);
        //    public static readonly TimeSpan HalfDayFrom = new(11, 0, 0);
        //    public static readonly TimeSpan MinFullDayHours = TimeSpan.FromHours(4.5);   // NEW, use your policy

        //    public readonly record struct DayFlags(
        //        bool IsPunch, bool IsHalfDay, bool IsLate, bool IsMissing);

        //    public static DayFlags Classify(EmployeeAttendance? record, DateTime date, DateTime today)
        //    {
        //        if (record == null || !record.InTime.HasValue)
        //            return new DayFlags(false, false, false, false);

        //        var key = new string((record.AttendanceStatus ?? "")
        //            .Where(char.IsLetter).ToArray());

        //        var inTime = record.InTime.Value.TimeOfDay;

        //        // NEW: left early (worked fewer than the minimum hours)
        //        bool leftEarly =
        //            record.OutTime.HasValue &&
        //            (record.OutTime.Value - record.InTime.Value) < MinFullDayHours;

        //        bool isHalfDay =
        //            key.Contains("HalfDay", StringComparison.OrdinalIgnoreCase) ||
        //            inTime >= HalfDayFrom ||
        //            leftEarly;                                                            // NEW

        //        bool isLate = !isHalfDay && (
        //            key.Contains("Late", StringComparison.OrdinalIgnoreCase) ||
        //            (inTime >= LateFrom && inTime < HalfDayFrom));

        //        bool isMissing =
        //            key.Contains("Missing", StringComparison.OrdinalIgnoreCase) ||
        //            (!record.OutTime.HasValue && date.Date < today.Date);

        //        return new DayFlags(true, isHalfDay, isLate, isMissing);
        //    }
        //}

        public static class AttendanceRules
        {
            public static readonly TimeSpan LateFrom = new(10, 0, 0);
            public static readonly TimeSpan HalfDayFrom = new(11, 0, 0);
            public static readonly TimeSpan MinFullDayHours = TimeSpan.FromHours(4.5);  // use your policy

            public readonly record struct DayFlags(
                bool IsPunch, bool IsHalfDay, bool IsLate, bool IsMissing);

            // CORE: works with any model, just pass its values
            public static DayFlags Classify(
                string? status,
                DateTime? inTime,
                DateTime? outTime,
                DateTime date,
                DateTime today)
            {
                if (!inTime.HasValue)
                    return new DayFlags(false, false, false, false);

                // Letters only: "Half Day - Check Out Missing" -> "HalfDayCheckOutMissing"
                var key = new string((status ?? "").Where(char.IsLetter).ToArray());
                var t = inTime.Value.TimeOfDay;

                //bool leftEarly =
                //    outTime.HasValue &&
                //    (outTime.Value - inTime.Value) < MinFullDayHours;
                bool leftEarly = false;

                bool isHalfDay =
                    key.Contains("HalfDay", StringComparison.OrdinalIgnoreCase) ||
                    t >= HalfDayFrom ||
                    leftEarly;

                bool isLate = !isHalfDay && (
                    key.Contains("Late", StringComparison.OrdinalIgnoreCase) ||
                    (t >= LateFrom && t < HalfDayFrom));

                bool isMissing =
                    key.Contains("Missing", StringComparison.OrdinalIgnoreCase) ||
                    (!outTime.HasValue && date.Date < today.Date);

                return new DayFlags(true, isHalfDay, isLate, isMissing);
            }

            // OVERLOAD: for the EmployeeAttendance entity (My Attendance, service)
            public static DayFlags Classify(EmployeeAttendance? record, DateTime date, DateTime today)
                => record == null
                    ? new DayFlags(false, false, false, false)
                    : Classify(record.AttendanceStatus, record.InTime, record.OutTime, date, today);
        }
        public async Task<AttendanceSummaryVM> GetMonthlySummaryAsync(
    int employeeId,
    int year,
    int month)
        {
            var startDate = new DateTime(year, month, 1);
            var endDate = startDate.AddMonths(1);
            var today = DateTime.Today;

            decimal leaveWorkedDays = 0m;

            var summary = new AttendanceSummaryVM
            {
                EmployeeId = employeeId,
                Year = year,
                Month = month
            };

            // 1. Attendance records
            var attendance = await _context.EmployeeAttendance
                .Where(x =>
                    x.EmployeeId == employeeId &&
                    x.AttendanceDate >= startDate &&
                    x.AttendanceDate < endDate)
                .OrderBy(x => x.AttendanceDate)
                .ToListAsync();

            var attendanceByDate = attendance
                .GroupBy(x => x.AttendanceDate.Date)
                .ToDictionary(g => g.Key, g => g.First());

            // 2. Approved leave
            var leaveRequests = await _context.LeaveRequest
                .Where(x =>
                    x.EmployeeId == employeeId &&
                    x.Status == "Approved" &&
                    x.FromDate < endDate &&
                    x.ToDate >= startDate)
                .ToListAsync();

            // 3. Holidays
            var holidayDates = (await _context.Holiday
                .Where(x => x.HolidayDate >= startDate && x.HolidayDate < endDate)
                .Select(x => x.HolidayDate.Date)
                .Distinct()
                .ToListAsync())
                .ToHashSet();

            // 4. Daily classification
            for (var date = startDate; date < endDate; date = date.AddDays(1))
            {
                var currentDate = date.Date;

                attendanceByDate.TryGetValue(currentDate, out var record);
                var c = AttendanceRules.Classify(record, currentDate, today);

                // Priority 1: Holiday
                if (holidayDates.Contains(currentDate))
                {
                    summary.HolidayCount++;
                    continue;
                }

                // Priority 2: Approved leave (the worked part of the day still counts)
                var leave = leaveRequests.FirstOrDefault(x =>
                    x.FromDate.Date <= currentDate &&
                    x.ToDate.Date >= currentDate);

                if (leave != null)
                {
                    var leaveKey = Convert.ToString(leave.LeaveDuration)?.Trim();

                    bool isFirstHalf = string.Equals(leaveKey, "FirstHalf", StringComparison.OrdinalIgnoreCase);
                    bool isSecondHalf = string.Equals(leaveKey, "SecondHalf", StringComparison.OrdinalIgnoreCase);
                    bool isHalfLeave = isFirstHalf || isSecondHalf;

                    summary.LeaveCount += isHalfLeave ? 0.5m : 1m;

                    if (c.IsPunch)
                    {
                        leaveWorkedDays += 0.5m;

                        if (c.IsLate && !isFirstHalf) summary.LateCount++;
                        if (c.IsMissing) summary.MissingPunchCount++;
                    }

                    continue;
                }

                // Priority 3: Attendance
                if (c.IsPunch)
                {
                    if (c.IsHalfDay) summary.HalfDayCount++;
                    else summary.PresentCount++;   // Late is a flag on a present day

                    if (c.IsLate) summary.LateCount++;
                    if (c.IsMissing) summary.MissingPunchCount++;

                    continue;
                }

                // Priority 4: Sunday
                if (currentDate.DayOfWeek == DayOfWeek.Sunday)
                    continue;

                // Priority 5: Saturday WFH
                if (currentDate.DayOfWeek == DayOfWeek.Saturday)
                {
                    if (currentDate <= today)
                        summary.WFHCount++;

                    continue;
                }

                // Priority 6: Past working day with no attendance
                if (currentDate < today)
                    summary.AbsentCount++;
            }

            // 5. Late leave deduction (first 3 free, then 0.5 per late)
            summary.LateLeaveDeduction =
                summary.LateCount >= 4
                    ? (summary.LateCount - 3) * 0.5m
                    : 0m;

            // 6. Days worked (full days + 0.5 per half day + worked half on leave days)
            summary.DaysWorked =
                summary.PresentCount
                + (summary.HalfDayCount * 0.5m)
                + leaveWorkedDays;

            return summary;
        }
        //public async Task<AttendanceSummaryVM> GetMonthlySummaryAsync(
        //    int employeeId,
        //    int year,
        //    int month)
        //{
        //    var startDate = new DateTime(year, month, 1);
        //    var endDate = startDate.AddMonths(1);
        //    var today = DateTime.Today;

        //    var summary = new AttendanceSummaryVM
        //    {
        //        EmployeeId = employeeId,
        //        Year = year,
        //        Month = month
        //    };

        //    // 1. Attendance records
        //    var attendance = await _context.EmployeeAttendance
        //        .Where(x =>
        //            x.EmployeeId == employeeId &&
        //            x.AttendanceDate >= startDate &&
        //            x.AttendanceDate < endDate)
        //        .OrderBy(x => x.AttendanceDate)
        //        .ToListAsync();

        //    var attendanceByDate = attendance
        //        .GroupBy(x => x.AttendanceDate.Date)
        //        .ToDictionary(g => g.Key, g => g.First());

        //    // 2. Approved leave records
        //    var leaveRequests = await _context.LeaveRequest
        //        .Where(x =>
        //            x.EmployeeId == employeeId &&
        //            x.Status == "Approved" &&
        //            x.FromDate < endDate &&
        //            x.ToDate >= startDate)
        //        .ToListAsync();

        //    // 3. Holidays
        //    var holidays = await _context.Holiday
        //        .Where(x =>
        //            x.HolidayDate >= startDate &&
        //            x.HolidayDate < endDate)
        //        .Select(x => x.HolidayDate.Date)
        //        .Distinct()
        //        .ToListAsync();

        //    var holidayDates = holidays.ToHashSet();

        //    // 4. Calculate daily summary
        //    for (var date = startDate;
        //         date < endDate;
        //         date = date.AddDays(1))
        //    {
        //        var currentDate = date.Date;

        //        // Priority 1: Holiday
        //        if (holidayDates.Contains(currentDate))
        //        {
        //            summary.HolidayCount++;
        //            continue;
        //        }

        //        // Priority 2: Approved Leave
        //        bool isOnLeave = leaveRequests.Any(x =>
        //            x.FromDate.Date <= currentDate &&
        //            x.ToDate.Date >= currentDate);

        //        if (isOnLeave)
        //        {
        //            summary.LeaveCount++;
        //            continue;
        //        }

        //        // Priority 3: Attendance
        //        if (attendanceByDate.TryGetValue(currentDate, out var record))
        //        {
        //            string status = NormalizeStatus(record.AttendanceStatus);

        //            bool isLate =
        //                status == "Late" ||
        //                (record.InTime.HasValue &&
        //                 record.InTime.Value.TimeOfDay >= new TimeSpan(10, 0, 0) &&
        //                 record.InTime.Value.TimeOfDay < new TimeSpan(11, 0, 0));

        //            if (status == "Present" || status == "Late")
        //            {
        //                summary.PresentCount++;
        //            }

        //            if (isLate)
        //            {
        //                summary.LateCount++;
        //            }

        //            if (status == "HalfDay")
        //            {
        //                summary.HalfDayCount++;
        //            }

        //            //if (status.Contains("CheckoutMissing"))
        //            //{
        //            //    summary.MissingPunchCount++;
        //            //}
        //            if (record.InTime.HasValue && !record.OutTime.HasValue)
        //            {
        //                summary.MissingPunchCount++;
        //            }
        //            continue;
        //        }

        //        // Priority 4: Sunday
        //        if (currentDate.DayOfWeek == DayOfWeek.Sunday)
        //        {
        //            continue;
        //        }

        //        // Priority 5: Saturday WFH
        //        if (currentDate.DayOfWeek == DayOfWeek.Saturday)
        //        {
        //            if (currentDate <= today)
        //            {
        //                summary.WFHCount++;
        //            }

        //            continue;
        //        }

        //        // Priority 6: Past working day without attendance
        //        if (currentDate < today)
        //        {
        //            summary.AbsentCount++;
        //        }
        //    }

        //    // 5. Late leave deduction
        //    summary.LateLeaveDeduction =
        //        summary.LateCount >= 4
        //            ? (summary.LateCount - 3) * 0.5m
        //            : 0m;

        //    // 6. Days worked
        //    summary.DaysWorked =
        //        summary.PresentCount + (summary.HalfDayCount * 0.5m);

        //    return summary;
        //}


        private static string NormalizeStatus(string? status)
        {
            return (status ?? string.Empty)
                .Replace(" ", "")
                .Replace("-", "")
                .Replace("_", "")
                .Trim();
        }
    }
}
