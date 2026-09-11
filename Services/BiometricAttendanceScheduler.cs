using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

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


        protected override async Task ExecuteAsync(
            CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var now = DateTime.Now;

                    // =============================================
                    // NEXT 12:00 PM
                    // =============================================

                    var nextRun = now.Date.AddHours(12);

                    if (now >= nextRun)
                    {
                        nextRun = nextRun.AddDays(1);
                    }

                    //FOR TESTING USE THIS LINE
                    //var nextRun = DateTime.Now.AddMinutes(1);

                    var delay = nextRun - now;

                    _logger.LogInformation(
                        "Next biometric attendance sync scheduled at {NextRun}",
                        nextRun);

                    // =============================================
                    // WAIT UNTIL 12:00 PM
                    // =============================================

                    await Task.Delay(
                        delay,
                        stoppingToken);

                    if (stoppingToken.IsCancellationRequested)
                        break;

                    // =============================================
                    // RUN SYNC
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

        private async Task RunBiometricSync(
            CancellationToken cancellationToken)
        {
            using var scope =
                _scopeFactory.CreateScope();

            var syncService =
                scope.ServiceProvider
                    .GetRequiredService<IAttendanceSyncService>();

            //var today = DateTime.Today;

            //var result =
            //    await syncService
            //        .SyncBiometricAttendanceAsync(
            //            today,
            //            today);
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
        }
    }
}

