using Microsoft.EntityFrameworkCore;
using QUIZAPP;
using TSSC.Unified.Models;

namespace TSSC.Unified.Services.Leave
{
    public class LeaveService : ILeaveService
    {
        private readonly AppdbContext _context;

        public LeaveService(AppdbContext context)
        {
            _context = context;
        }

        private async Task ProcessLateLeaveDeductionAsync(
        int employeeId,
        int leaveTypeId,
        DateTime month)
        {
            var startDate = new DateTime(month.Year, month.Month, 1);
            var endDate = startDate.AddMonths(1);

            // 1. Count late marks
            var lateCount = await _context.EmployeeAttendance
                .CountAsync(x =>
                    x.EmployeeId == employeeId &&
                    x.AttendanceDate >= startDate &&
                    x.AttendanceDate < endDate &&
                    x.AttendanceStatus == "Late");

            // 2. Every 3 late marks = 1 leave
            var requiredDeduction = lateCount / 3;

            if (requiredDeduction <= 0)
                return;

            // 3. Find leave adjustments already created
            var alreadyDeducted = await _context.LeaveAdjustment
                .Where(x =>
                    x.EmployeeId == employeeId &&
                    x.LeaveTypeId == leaveTypeId &&
                    x.AdjustmentType == "LateDeduction" &&
                    x.AdjustedOn >= startDate &&
                    x.AdjustedOn < endDate)
                .SumAsync(x => x.Days);

            // 4. Calculate only NEW deduction
            var newDeduction = requiredDeduction - alreadyDeducted;

            if (newDeduction <= 0)
                return;

            // 5. Get leave balance
            var balance = await _context.EmployeeLeaveBalance
                .FirstOrDefaultAsync(x =>
                    x.EmployeeId == employeeId &&
                    x.LeaveTypeId == leaveTypeId);

            if (balance == null)
                return;

            // 6. Create LeaveAdjustment
            var adjustment = new LeaveAdjustment
            {
                EmployeeId = employeeId,
                LeaveTypeId = leaveTypeId,
                AdjustmentType = "LateDeduction",
                Days = newDeduction,
                Reason = $"Late attendance deduction - {startDate:MMMM yyyy}",
                AdjustedBy = 0,
                AdjustedOn = DateTime.Now
            };

            _context.LeaveAdjustment.Add(adjustment);

            // 7. Update UsedLeaves
            balance.UsedLeaves += newDeduction;
            balance.LastUpdated = DateTime.Now;

            await _context.SaveChangesAsync();
        }

        Task ILeaveService.ProcessLateLeaveDeductionAsync(int employeeId, int leaveTypeId, DateTime month)
        {
            return ProcessLateLeaveDeductionAsync(employeeId, leaveTypeId, month);
        }
    }
}
