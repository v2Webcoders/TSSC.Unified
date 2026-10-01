namespace TSSC.Unified.Services.Leave
{
    public interface ILeaveService
    {
        Task ProcessLateLeaveDeductionAsync(
            int employeeId,
            int leaveTypeId,
            DateTime month);
    }
}
