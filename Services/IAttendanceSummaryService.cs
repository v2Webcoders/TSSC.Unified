using TSSC.Unified.ViewModel;

namespace TSSC.Unified.Services
{
    public interface IAttendanceSummaryService
    {
        Task<AttendanceSummaryVM> GetMonthlySummaryAsync(
            int employeeId,
            int year,
            int month);
    }
}
