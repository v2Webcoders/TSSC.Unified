namespace TSSC.Unified.Services
{
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Caching.Memory;
    using QUIZAPP;

    public record MenuUser(
        int EmployeeId,
        bool IsHR,
        bool IsManager,
        bool IsFinanceEmployee,
        bool IsFinanceHead,
        bool IsCEO);

    public record MenuCounts(
    int AllRegularizations,
    int AllODRequests,
    int HRLeaveRequests,
    int ManageLeaves,
    int Tasks,
    int PendingApprovals,
    int ReimbursementRequests,
    int ManagerRegularizations,
    int ManagerODRequests)
    {
        public static readonly MenuCounts Empty = new(0, 0, 0, 0, 0, 0, 0, 0, 0);

        // HR > Attendance Management (parent dot)
        public int AttendanceTotal => AllRegularizations + AllODRequests;

        // Team > Attendance Approvals (parent dot, non-HR managers)
        public int ApprovalsAttendanceTotal => ManagerRegularizations + ManagerODRequests;
    }

    public interface IMenuCountService
    {
        Task<MenuCounts> GetAsync(MenuUser u);
        void Invalidate(int employeeId);
    }

    public class MenuCountService : IMenuCountService
    {
        private readonly AppdbContext _db;
        private readonly IMemoryCache _cache;

        public MenuCountService(AppdbContext db, IMemoryCache cache)
        {
            _db = db;
            _cache = cache;
        }

        private static string Key(int id) => $"menucounts:{id}";

        public void Invalidate(int employeeId) => _cache.Remove(Key(employeeId));

        //public Task<MenuCounts> GetAsync(MenuUser u) =>
        //    _cache.GetOrCreateAsync(Key(u.EmployeeId), async entry =>
        //    {
        //        entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30);
        //        var id = u.EmployeeId;

        //        // ---------- Leave / Regularization / OD ----------
        //        var leaves = _db.LeaveRequest.AsNoTracking().Where(x => x.Status == "Pending");
        //        var regs = _db.AttendanceRegularization.AsNoTracking().Where(x => x.Status == "Pending");
        //        var ods = _db.ODRequest.AsNoTracking().Where(x => x.Status == "Pending");

        //        if (!u.IsHR)
        //        {
        //            var reportees = _db.Employee
        //                .Where(x => x.ReportingManagerId == id)
        //                .Select(x => x.EmployeeId);

        //            leaves = leaves.Where(x => reportees.Contains(x.EmployeeId));
        //            regs = regs.Where(x => reportees.Contains(x.EmployeeId));
        //            ods = ods.Where(x => reportees.Contains(x.EmployeeId));
        //        }

        //        int leaveCount = (u.IsHR || u.IsManager) ? await leaves.CountAsync() : 0;

        //        // HR-only badges
        //        // AFTER (HR and managers)
        //        int regCount = (u.IsHR || u.IsManager) ? await regs.CountAsync() : 0;
        //        int odCount = (u.IsHR || u.IsManager) ? await ods.CountAsync() : 0;

        //        // ---------- Tasks ----------
        //        int taskCount = await GetTaskCountAsync(u);

        //        // ---------- Internal Approvals: Pending Approvals ----------
        //        int approvals = await _db.Reimbursements.AsNoTracking()
        //            .Where(x => x.IsActive)
        //            .CountAsync(x =>
        //                (x.Approver2Id == id && x.Approver2Status == "Pending") ||
        //                (x.Approver3Id == id && x.Approver2Status == "Approved" && x.Approver3Status == "Pending") ||
        //                (x.Approver4Id == id && x.Approver2Status == "Approved" && x.Approver3Status == "Approved" && x.Approver4Status == "Pending") ||
        //                (x.Approver5Id == id && x.Approver2Status == "Approved" && x.Approver3Status == "Approved" && x.Approver4Status == "Approved" && x.Approver5Status == "Pending") ||
        //                (x.Approver6Id == id && x.Approver2Status == "Approved" && x.Approver3Status == "Approved" && x.Approver4Status == "Approved" && x.Approver5Status == "Approved" && x.Approver6Status == "Pending"));

        //        // ---------- Reimbursement Requests (one stage per user, same priority as menu) ----------
        //        int reimb = await GetReimbursementCountAsync(u);

        //        return new MenuCounts(
        //            u.IsHR ? regCount : 0,     // 1 HR > All Regularizations
        //            u.IsHR ? odCount : 0,     // 2 HR > All OD Requests
        //            u.IsHR ? leaveCount : 0,   // 3 HR > Leave Requests
        //            leaveCount,                // 4 Team > Manage Leave Requests
        //            taskCount, approvals, reimb,
        //            u.IsHR ? 0 : regCount,     // 8 Team > Regularization Requests (manager)
        //            u.IsHR ? 0 : odCount);     // 9 Team > OD Requests (manager)
        //        })!;

        // ---------------------------------------------------------

        public Task<MenuCounts> GetAsync(MenuUser u) =>
    _cache.GetOrCreateAsync(Key(u.EmployeeId), async entry =>
    {
        entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30);
        var id = u.EmployeeId;

        // ---------- Leave / Regularization / OD ----------
        var allLeaves = _db.LeaveRequest.AsNoTracking().Where(x => x.Status == "Pending");
        var allRegs = _db.AttendanceRegularization.AsNoTracking().Where(x => x.Status == "Pending");
        var allOds = _db.ODRequest.AsNoTracking().Where(x => x.Status == "Pending");

        // HR scope: everyone
        int hrLeaveCount = u.IsHR ? await allLeaves.CountAsync() : 0;
        int hrRegCount = u.IsHR ? await allRegs.CountAsync() : 0;
        int hrOdCount = u.IsHR ? await allOds.CountAsync() : 0;

        // Manager scope: direct reportees only
        int teamLeaveCount = 0, teamRegCount = 0, teamOdCount = 0;
        if (u.IsManager)
        {
            var reportees = _db.Employee
                .Where(x => x.ReportingManagerId == id)
                .Select(x => x.EmployeeId);

            teamLeaveCount = await allLeaves.Where(x => reportees.Contains(x.EmployeeId)).CountAsync();
            teamRegCount = await allRegs.Where(x => reportees.Contains(x.EmployeeId)).CountAsync();
            teamOdCount = await allOds.Where(x => reportees.Contains(x.EmployeeId)).CountAsync();
        }

        // ---------- Tasks ----------
        int taskCount = await GetTaskCountAsync(u);

        // ---------- Internal Approvals: Pending Approvals ----------
        int approvals = await _db.Reimbursements.AsNoTracking()
            .Where(x => x.IsActive)
            .CountAsync(x =>
                (x.Approver2Id == id && x.Approver2Status == "Pending") ||
                (x.Approver3Id == id && x.Approver2Status == "Approved" && x.Approver3Status == "Pending") ||
                (x.Approver4Id == id && x.Approver2Status == "Approved" && x.Approver3Status == "Approved" && x.Approver4Status == "Pending") ||
                (x.Approver5Id == id && x.Approver2Status == "Approved" && x.Approver3Status == "Approved" && x.Approver4Status == "Approved" && x.Approver5Status == "Pending") ||
                (x.Approver6Id == id && x.Approver2Status == "Approved" && x.Approver3Status == "Approved" && x.Approver4Status == "Approved" && x.Approver5Status == "Approved" && x.Approver6Status == "Pending"));

        // ---------- Reimbursement Requests (one stage per user, same priority as menu) ----------
        int reimb = await GetReimbursementCountAsync(u);

        return new MenuCounts(
            hrRegCount,        // 1 HR > All Regularizations
            hrOdCount,         // 2 HR > All OD Requests
            hrLeaveCount,      // 3 HR > Leave Requests
            teamLeaveCount,    // 4 Team > Manage Leave Requests
            taskCount, approvals, reimb,
            teamRegCount,      // 8 Team > Regularization Requests (manager)
            teamOdCount);      // 9 Team > OD Requests (manager)
    })!;
        private async Task<int> GetTaskCountAsync(MenuUser u)
        {
            if (!(u.IsManager || u.IsHR || u.IsCEO || u.IsFinanceEmployee || u.IsFinanceHead))
                return 0;

            // ASSUMPTION: replace entity/columns with yours.
            // Counts tasks I assigned that are Completed and waiting to be closed.
            return await _db.Tasks.AsNoTracking()
                .CountAsync(t => t.AssignedBy == u.EmployeeId && t.Status == "Completed");
        }

        private async Task<int> GetReimbursementCountAsync(MenuUser u)
        {
            var claims = _db.Reimbursement.AsNoTracking();   // REIMBURSEMENT

            if (u.IsManager)
                return await claims.CountAsync(x =>
                    x.Status == "Pending Manager Approval" &&
                    _db.Employee.Any(e => e.EmployeeId == x.EmployeeId &&
                                          e.ReportingManagerId == u.EmployeeId));

            if (u.IsFinanceEmployee) return await claims.CountAsync(x => x.FinanceStatus == "Pending");
            if (u.IsHR) return await claims.CountAsync(x => x.HRStatus == "Pending");
            if (u.IsFinanceHead) return await claims.CountAsync(x => x.FinanceHeadStatus == "Pending");
            if (u.IsCEO) return await claims.CountAsync(x => x.CEOStatus == "Pending");
            return 0;
        }
    }
}
