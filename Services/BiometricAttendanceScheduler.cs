using DocumentFormat.OpenXml.InkML;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using QUIZAPP;
using TSSC.Unified.Services.Leave;

namespace TSSC.Unified.Services
{
    public class BiometricAttendanceScheduler : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<BiometricAttendanceScheduler> _logger;

        public BiometricAttendanceScheduler(
            IServiceScopeFactory scopeFactory,
            ILogger<BiometricAttendanceScheduler> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var now = DateTime.Now;

                    // =============================================
                    // DAILY SYNC SCHEDULE
                    // =============================================

                    var scheduledTimes = new[]
                    {
                        now.Date.AddHours(10),                  // 10:00 AM
                        now.Date.AddHours(12).AddMinutes(30),   // 12:30 PM
                        now.Date.AddHours(15).AddMinutes(30),   // 3:30 PM
                        now.Date.AddHours(23)                   // 11:00 PM
                    };

                    //FOR TESTING
                    //var scheduledTimes = new[]
                    //{
                    //    DateTime.Now.AddMinutes(2)
                    //};

                    // =============================================
                    // GET NEXT SCHEDULED TIME
                    // =============================================

                    var nextRun = scheduledTimes
                        .FirstOrDefault(x => x > now);

                    // If all today's schedules have passed,
                    // schedule the first sync tomorrow at 10 AM.

                    if (nextRun == default)
                    {
                        nextRun = now.Date.AddDays(1).AddHours(10);
                    }

                    var delay = nextRun - now;

                    _logger.LogInformation(
                        "Next biometric attendance sync scheduled at {NextRun}",
                        nextRun);

                    // =============================================
                    // WAIT UNTIL NEXT SCHEDULED TIME
                    // =============================================

                    await Task.Delay(
                        delay,
                        stoppingToken);

                    if (stoppingToken.IsCancellationRequested)
                        break;

                    // =============================================
                    // RUN BIOMETRIC SYNC
                    // =============================================

                    await RunBiometricSync(stoppingToken);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Error in biometric attendance scheduler.");

                    // Retry after 5 minutes
                    await Task.Delay(
                        TimeSpan.FromMinutes(5),
                        stoppingToken);
                }
            }
        }

        //protected override async Task ExecuteAsync(
        //    CancellationToken stoppingToken)
        //{
        //    while (!stoppingToken.IsCancellationRequested)
        //    {
        //        try
        //        {
        //            var now = DateTime.Now;

        //            // =============================================
        //            // CALCULATE NEXT 12:00 PM
        //            // =============================================

        //            var next12PM = now.Date.AddHours(12);

        //            if (now >= next12PM)
        //            {
        //                next12PM = next12PM.AddDays(1);
        //            }


        //            // =============================================
        //            // CALCULATE NEXT 12:00 AM (MIDNIGHT)
        //            // =============================================

        //            var nextMidnight = now.Date.AddDays(1);


        //            // =============================================
        //            // GET THE NEXT SCHEDULED TIME
        //            // =============================================

        //            var nextRun =
        //                next12PM < nextMidnight
        //                    ? next12PM
        //                    : nextMidnight;

        //            // =============================================
        //            // FOR TESTING
        //            // =============================================

        //            //var nextRun = DateTime.Now.AddMinutes(2);

        //            var delay = nextRun - now;

        //            _logger.LogInformation(
        //                "Next biometric attendance sync scheduled at {NextRun}",
        //                nextRun);

        //            // =============================================
        //            // WAIT UNTIL 12:00 PM
        //            // =============================================

        //            await Task.Delay(
        //                delay,
        //                stoppingToken);

        //            if (stoppingToken.IsCancellationRequested)
        //                break;

        //            // =============================================
        //            // RUN SYNC
        //            // =============================================

        //            await RunBiometricSync(stoppingToken);
        //        }
        //        catch (TaskCanceledException)
        //        {
        //            break;
        //        }
        //        catch (Exception ex)
        //        {
        //            _logger.LogError(
        //                ex,
        //                "Error in biometric attendance scheduler.");

        //            // Retry after 5 minutes
        //            await Task.Delay(
        //                TimeSpan.FromMinutes(5),
        //                stoppingToken);
        //        }
        //    }
        //}

        private async Task RunBiometricSync(
            CancellationToken cancellationToken)
        {
            using var scope =
                _scopeFactory.CreateScope();

            var syncService =
                scope.ServiceProvider
                    .GetRequiredService<IAttendanceSyncService>();
            var leaveService =
                scope.ServiceProvider
                    .GetRequiredService<ILeaveService>();
            var context =
    scope.ServiceProvider
        .GetRequiredService<AppdbContext>();
            var today = DateTime.Today;

            var fromDate = today.AddDays(-2);
            var toDate = today;

            var result =
                await syncService
                    .SyncBiometricAttendanceAsync(
                        fromDate,
                        toDate);

            _logger.LogInformation(
                "Daily biometric sync completed. " +
                "Records: {Records}, Created: {Created}, " +
                "Updated: {Updated}, MissingPunch: {MissingPunch}, " +
                "Unmapped: {Unmapped}, Duplicates: {Duplicates}",
                result.TotalBiometricRecords,
                result.AttendanceCreated,
                result.AttendanceUpdated,
                result.MissingPunch,
                result.UnmappedEmployees,
                result.DuplicatePunches);


            // =============================================
            // LATE DEDUCTION: ENTIRE CURRENT MONTH
            // =============================================

            var monthStart = new DateTime(
                today.Year,
                today.Month,
                1);

            var monthEnd = monthStart.AddMonths(1);

            var employeeIds =
                await context.EmployeeAttendance
                    .Where(x =>
                        x.AttendanceDate >= monthStart &&
                        x.AttendanceDate < monthEnd &&
                        x.AttendanceStatus == "Late")
                    .Select(x => x.EmployeeId)
                    .Distinct()
                    .ToListAsync(cancellationToken);


            // =============================================
            // CASUAL LEAVE
            // =============================================

            var casualLeaveTypeId = 4;

            if (casualLeaveTypeId == 0)
            {
                _logger.LogWarning(
                    "Casual Leave type was not found.");

                return;
            }


            // =============================================
            // PROCESS LATE DEDUCTION
            // =============================================

            foreach (var employeeId in employeeIds)
            {
                await leaveService
                    .ProcessLateLeaveDeductionAsync(
                        employeeId,
                        casualLeaveTypeId,
                        today);
            }

            _logger.LogInformation(
                "Late leave deduction completed. " +
                "Employees processed: {Count}",
                employeeIds.Count);
        }
    }
}

