namespace TSSC.Unified.Services
{
    using Microsoft.AspNetCore.Identity;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Caching.Memory;
    using QUIZAPP;
    using QUIZAPP.Models;
    using TSSC.Unified.Areas.HRMS.Controllers;

    //public record MenuUser(
    //    int EmployeeId,
    //    bool IsHR,
    //    bool IsManager,
    //    bool IsFinanceEmployee,
    //    bool IsFinanceHead,
    //    bool IsCEO);
    public record MenuUser(
    int EmployeeId,
    bool IsHR,
    bool IsManager,
    bool IsFinanceEmployee,
    bool IsFinanceHead,
    bool IsCEO,
    string? ApplicationUserId);
    public record MenuCounts(
    int AllRegularizations,
    int AllODRequests,
    int HRLeaveRequests,
    int ManageLeaves,
    int Tasks,
    int PendingApprovals,
    int ReimbursementRequests,
    int ManagerRegularizations,
    int ManagerODRequests, int tpPendingApprovals)
    {
        public static readonly MenuCounts Empty = new(0, 0, 0, 0, 0, 0, 0, 0, 0, 0);

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
        UserManager<AppUser> _userManager;

        public MenuCountService(AppdbContext db, IMemoryCache cache, UserManager<AppUser> userManager)
        {
            _db = db;
            _cache = cache;
            _userManager = userManager;
        }

        private static string Key(int id) => $"menucounts:{id}";

        public void Invalidate(int employeeId) => _cache.Remove(Key(employeeId));

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
        int tpPendingApprovals = await GetTPPendingApprovalCountAsync(u);
        return new MenuCounts(
            hrRegCount,        // 1 HR > All Regularizations
            hrOdCount,         // 2 HR > All OD Requests
            hrLeaveCount,      // 3 HR > Leave Requests
            teamLeaveCount,    // 4 Team > Manage Leave Requests
            taskCount, approvals, reimb,
            teamRegCount,      // 8 Team > Regularization Requests (manager)
            teamOdCount, tpPendingApprovals);      // 9 Team > OD Requests (manager)
    })!;
        
        private async Task<int> GetTaskCountAsync(MenuUser u)
        {
            return await _db.Tasks
            .AsNoTracking()
            .CountAsync(t =>
                t.AssignedTo == u.EmployeeId &&
                t.IsActive &&
                t.Status == "Pending");
        }
        private async Task<int> GetReimbursementCountAsync(MenuUser u)
        {
            var claims = _db.Reimbursement.AsNoTracking();   // REIMBURSEMENT

            if (u.IsManager)
                return await claims.CountAsync(x =>
                    x.Status == "Pending Manager Approval" &&
                    _db.Employee.Any(e => e.EmployeeId == x.EmployeeId && e.ReportingManagerId == u.EmployeeId));

            if (u.IsFinanceEmployee)
                return await claims.CountAsync(x =>
                    x.IsActive &&
                    x.Status == "Manager Approved" &&
                    x.FinanceStatus == "Pending");
            if (u.IsHR) return await claims.CountAsync(x => x.FinanceStatus == "Finance Approved" && x.HRStatus == "Pending");
            
            if (u.IsFinanceHead) return await claims.CountAsync(x => x.FinanceStatus == "Finance Approved" &&
                    x.HRStatus == "HR Approved" && x.FinanceHeadStatus == "Pending");
           
            if (u.IsCEO) return await claims.CountAsync(x => x.FinanceStatus == "Finance Approved" &&
                    x.HRStatus == "HR Approved" &&
                    x.FinanceHeadStatus == "Finance Head Approved" && x.CEOStatus == "Pending");
            return 0;
        }
        private async Task<int> GetTPPendingApprovalCountAsync(MenuUser u)
        {
            if (string.IsNullOrWhiteSpace(u.ApplicationUserId))
                return 0;

            var appUser = await _userManager.FindByIdAsync(u.ApplicationUserId);

            if (appUser == null)
                return 0;

            var roles = await _userManager.GetRolesAsync(appUser);
            var roleNames = roles.ToList();

            var assignedStages = await _db.TPWorkflowApprovers
                .AsNoTracking()
                .Where(a => a.IsActive &&
                            a.StageCode != "FINANCE_CERTIFICATE" &&
                            (
                                (a.ApproverType == "Role" &&
                                 a.RoleName != null &&
                                 roleNames.Contains(a.RoleName))
                                ||
                                (a.ApproverType == "User" &&
                                 a.ApplicationUserId == u.ApplicationUserId)
                            ))
                .Select(a => a.StageCode)
                .Distinct()
                .ToListAsync();

            if (assignedStages.Count == 0)
                return 0;

            var registrations = _db.TPRegistrations
                .AsNoTracking()
                .Where(r => assignedStages.Contains(r.CurrentStage));

            // Count registrations awaiting approval at the assigned current stage.
            int count = 0;

            if (assignedStages.Contains("REGIONAL"))
                count += await registrations.CountAsync(r =>
                    r.CurrentStage == "REGIONAL" &&
                    r.Regional_Head_Status == "Pending");

            if (assignedStages.Contains("AFFILIATIONS"))
                count += await registrations.CountAsync(r =>
                    r.CurrentStage == "AFFILIATIONS" &&
                    r.Affiliations_Status == "Pending");

            if (assignedStages.Contains("FINANCE"))
                count += await registrations.CountAsync(r =>
                    r.CurrentStage == "FINANCE" &&
                    r.Finance_Status == "Pending");

            if (assignedStages.Contains("VERTICAL_HEAD"))
                count += await registrations.CountAsync(r =>
                    r.CurrentStage == "VERTICAL_HEAD" &&
                    r.Vertical_Head_Status == "Pending");

            if (assignedStages.Contains("SENIOR_REVIEWER_1"))
                count += await registrations.CountAsync(r =>
                    r.CurrentStage == "SENIOR_REVIEWER_1" &&
                    r.SeniorReviewer1_Status == "Pending");

            if (assignedStages.Contains("SENIOR_REVIEWER_2"))
                count += await registrations.CountAsync(r =>
                    r.CurrentStage == "SENIOR_REVIEWER_2" &&
                    r.SeniorReviewer2_Status == "Pending");

            if (assignedStages.Contains("CEO"))
                count += await registrations.CountAsync(r =>
                    r.CurrentStage == "CEO" &&
                    r.CEO_Status == "Pending");

            if (assignedStages.Contains("PAYMENT_PROCESS"))
                count += await registrations.CountAsync(r =>
                    r.CurrentStage == "PAYMENT_PROCESS" &&
                    r.PaymentProcess_Status == "Pending");

            if (assignedStages.Contains("CERTIFICATE_ISSUANCE"))
                count += await registrations.CountAsync(r =>
                    r.CurrentStage == "CERTIFICATE_ISSUANCE" &&
                    r.CertificateIssuance_Status == "Pending");

            return count;
        }
    }
}
