using TSSC.Unified.ViewModel;

namespace TSSC.Unified.Services
{
    public interface IAttendanceSyncService
    {
        Task<AttendanceSyncResultVM> SyncBiometricAttendanceAsync(
            DateTime fromDate,
            DateTime toDate);
    }
}
