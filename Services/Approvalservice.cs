using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Globalization;
using Microsoft.EntityFrameworkCore;
using QUIZAPP;

namespace TSSC.Unified.Services
{
    public enum ApprovalDecision { Approved = 1, Rejected = 2, Returned = 3 }

    public class ApprovalHeader
    {
        public long ApprovalId { get; set; }
        public string RequestId { get; set; } = "";
        public string RequestType { get; set; } = "";
        public string RequestTitle { get; set; } = "";
        public string Department { get; set; } = "";
        public string SubmittedBy { get; set; } = "";
        public DateTime SubmittedDate { get; set; }
        public DateTime? DueDate { get; set; }
        public string Priority { get; set; } = "";
        public string Status { get; set; } = "";
        public string CurrentStage { get; set; } = "";
        public string SourceTable { get; set; } = "";
        public int? CEOEmployeeId { get; set; }
        public int? ApprovalStep { get; set; }
    }

    public record DetailItem(string Label, string Value);
    public record TrailItem(string Step, string Status, bool IsYou);

    public class ApprovalDetailsVm
    {
        public ApprovalHeader Header { get; set; } = new();
        public List<DetailItem> Details { get; set; } = new();
        public List<TrailItem> Trail { get; set; } = new();
    }

    public class DecideApprovalInput
    {
        [Required] public string Source { get; set; } = "";
        [Required] public long Id { get; set; }
        [Required] public ApprovalDecision Decision { get; set; }
        [StringLength(2000)] public string? Remarks { get; set; }
    }

    public class ApprovalService
    {
       // private readonly IDbConnection _db;
       // public ApprovalService(IDbConnection db) => _db = db;
        private readonly AppdbContext _context;

        public ApprovalService(AppdbContext context)
        {
            _context = context;
        }

        // Returns null when the logged-in user is not an active CEO employee.
        public async Task<int?> GetCeoEmployeeIdAsync(string userId)
        {
            return await _context.Employee
                .Where(e => e.ApplicationUserId == userId
                            && e.IsActive)
                .Where(e => _context.UserRoles.Any(ur =>
                    ur.UserId == userId &&
                    _context.Roles.Any(r =>
                        r.Id == ur.RoleId &&
                        r.Name == "CEO")))
                .Select(e => (int?)e.EmployeeId)
                .FirstOrDefaultAsync();
        }

        // Only rows still waiting for this CEO come back (the view is pending-only).
        public async Task<ApprovalHeader?> GetHeaderAsync(
     string source, long id, int ceoId)
        {
            var approval = await _context.CEOApprovals
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.SourceTable == source &&
                    x.ApprovalId == id &&
                    (x.CEOEmployeeId == ceoId ||
                     x.CEOEmployeeId == null));

            if (approval == null)
                return null;

            return new ApprovalHeader
            {
                ApprovalId = approval.ApprovalId,
                RequestId = approval.RequestId,
                RequestType = approval.RequestType,
                RequestTitle = approval.RequestTitle,
                Department = approval.Department,
                SubmittedBy = approval.SubmittedBy,
                SubmittedDate = approval.SubmittedDate,
                DueDate = approval.DueDate,
                Priority = approval.Priority,
                Status = approval.Status,
                CurrentStage = approval.CurrentStage ?? "",
                SourceTable = approval.SourceTable,
                CEOEmployeeId = approval.CEOEmployeeId,
                ApprovalStep = approval.ApprovalStep
            };
        }

        public async Task<ApprovalDetailsVm> GetDetailsAsync(ApprovalHeader h, int ceoId)
        {
            var vm = new ApprovalDetailsVm { Header = h };

            switch (h.SourceTable)
            {
                case "Reimbursements":
                    {
                        var r = await _context.Reimbursements
                            .AsNoTracking()
                            .FirstOrDefaultAsync(x =>
                                x.ReimbursementId == h.ApprovalId);

                        if (r == null)
                            break;

                        vm.Details.Add(new("Category", r.ExpenseCategory ?? "-"));

                        vm.Details.Add(new(
                            "Amount",
                            "₹" + Convert.ToDecimal(r.ExpenseAmount)
                                .ToString("N0", new CultureInfo("en-IN"))
                        ));

                        var approvers = new[]
                        {
                (r.Approver1Id, r.Approver1Status),
                (r.Approver2Id, r.Approver2Status),
                (r.Approver3Id, r.Approver3Status),
                (r.Approver4Id, r.Approver4Status),
                (r.Approver5Id, r.Approver5Status),
                (r.Approver6Id, r.Approver6Status)
            };

                        for (int i = 0; i < approvers.Length; i++)
                        {
                            var approver = approvers[i];

                            if (approver.Item1 == null)
                                continue;

                            vm.Trail.Add(new(
                                $"Approver {i + 1}",
                                approver.Item2 ?? "Pending",
                                Convert.ToInt32(approver.Item1) == ceoId
                            ));
                        }

                        break;
                    }

                case "TrainingCenter":
                    {
                        var t = await _context.TrainingCenter
                            .AsNoTracking()
                            .FirstOrDefaultAsync(x => x.Id == h.ApprovalId);

                        if (t == null)
                            break;

                        vm.Details.Add(new(
                            "Training Center",
                            t.TrainingCenterName ?? "-"
                        ));

                        vm.Details.Add(new(
                            "Organization",
                            t.OrganizationName ?? "-"
                        ));

                        vm.Trail.Add(new(
                            "Business Head",
                            t.BusinessHeadStatus ?? "Pending",
                            false
                        ));

                        //vm.Trail.Add(new(
                        //    "CEO",
                        //    t.CEOStatus ?? "Pending",
                        //    true
                        //));

                        break;
                    }

                case "TPRegistrations":
                    {
                        var tp = await _context.TPRegistrations
                            .AsNoTracking()
                            .FirstOrDefaultAsync(x => x.Id == h.ApprovalId);

                        if (tp == null)
                            break;

                        vm.Details.Add(new(
                            "Organization",
                            tp.OrganizationName ?? "-"
                        ));

                        vm.Trail.Add(new(
                            "CEO",
                            tp.CEO_Status ?? "Pending",
                            true
                        ));

                        break;
                    }

                case "LeaveRequest":
                    {
                        var leave = await _context.LeaveRequest
                            .AsNoTracking()
                            .FirstOrDefaultAsync(x =>
                                x.LeaveRequestId == h.ApprovalId);

                        if (leave == null)
                            break;
                        var leaveTypeName = await _context.LeaveType
                        .Where(x => x.LeaveTypeId == leave.LeaveTypeId)
                        .Select(x => x.LeaveTypeName)
                        .FirstOrDefaultAsync();

                        vm.Details.Add(new("Leave Type", leaveTypeName ?? "-"));
                        vm.Details.Add(new("From Date", leave.FromDate.ToString("dd MMM yyyy")));
                        vm.Details.Add(new("To Date", leave.ToDate.ToString("dd MMM yyyy")));
                        vm.Details.Add(new("Total Days", leave.TotalDays.ToString()));
                        vm.Details.Add(new("Reason", leave.Reason ?? "-"));

                        vm.Trail.Add(new(
                            "CEO",
                            leave.Status ?? "Pending",
                            true
                        ));

                        break;
                    }
            }

            return vm;
        }

        public async Task<(bool Ok, string Message)> DecideAsync(
    DecideApprovalInput input, int ceoId)
        {
            var remarks = input.Remarks?.Trim();

            if (input.Decision != ApprovalDecision.Approved &&
                string.IsNullOrWhiteSpace(remarks))
            {
                return (false, "Remarks are required to reject or return a request.");
            }

            // Verify that the request is still waiting for this CEO.
            var h = await GetHeaderAsync(input.Source, input.Id, ceoId);

            if (h == null)
                return (false, "This request is no longer waiting for your decision.");

            var status = input.Decision.ToString();

            await using var tx = await _context.Database.BeginTransactionAsync();

            try
            {
                var rows = 0;

                switch (h.SourceTable)
                {
                    case "LeaveRequest":
                        {
                            var leave = await _context.LeaveRequest
                                .FirstOrDefaultAsync(x =>
                                    x.LeaveRequestId == h.ApprovalId &&
                                    x.ApproverId == ceoId &&
                                    x.Status == "Pending");

                            if (leave != null)
                            {
                                leave.Status = status;
                                leave.ApprovedDate = DateTime.Now;
                                leave.ApprovalRemarks = remarks;

                                rows = 1;
                            }

                            break;
                        }

                    case "TPRegistrations":
                        {
                            var tp = await _context.TPRegistrations
                                .FirstOrDefaultAsync(x =>
                                    x.Id == h.ApprovalId &&
                                    x.CurrentStage == "CEO" &&
                                    (x.CEO_Status == null ||
                                     x.CEO_Status == "Pending"));

                            if (tp != null)
                            {
                                tp.CEO_Status = status;
                                tp.CEO_Date = DateTime.Now;
                                tp.CEO_Remarks = remarks;

                                rows = 1;
                            }

                            break;
                        }

                    //case "TrainingCenter":
                    //    {
                    //        var trainingCenter = await _context.TrainingCenter
                    //            .FirstOrDefaultAsync(x =>
                    //                x.Id == h.ApprovalId &&
                    //                x.CEOEmployeeId == ceoId &&
                    //                x.BusinessHeadStatus == "Approved" &&
                    //                (x.CEOStatus == null ||
                    //                 x.CEOStatus == "" ||
                    //                 x.CEOStatus == "Pending"));

                    //        if (trainingCenter != null)
                    //        {
                    //            trainingCenter.CEOStatus = status;
                    //            trainingCenter.CEODateApproveDate = DateTime.Now;
                    //            trainingCenter.CEORemarks = remarks;

                    //            rows = 1;
                    //        }

                    //        break;
                    //    }

                    case "Reimbursements":
                        {
                            rows = await DecideReimbursementAsync(
                                h, ceoId, status);

                            break;
                        }
                }

                if (rows == 0)
                {
                    await tx.RollbackAsync();

                    return (false,
                        "This request was already decided or is no longer waiting for you.");
                }

                await _context.SaveChangesAsync();

                // Insert audit log using EF Core parameterized SQL.
            //    await _context.Database.ExecuteSqlInterpolatedAsync($@"
            //INSERT INTO dbo.ApprovalActionLog
            //(
            //    SourceTable,
            //    ApprovalId,
            //    RequestId,
            //    Decision,
            //    Remarks,
            //    ActorEmployeeId,
            //    ActionDate
            //)
            //VALUES
            //(
            //    {h.SourceTable},
            //    {h.ApprovalId},
            //    {h.RequestId},
            //    {status},
            //    {remarks},
            //    {ceoId},
            //    SYSDATETIME()
            //)");

                await tx.CommitAsync();

                return (true,
                    $"{h.RequestId} marked as {status.ToLowerInvariant()}.");
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }
        private async Task<int> DecideReimbursementAsync(
    ApprovalHeader h,
    int ceoId,
    string status)
        {
            var n = h.ApprovalStep ?? 0;

            if (n < 1 || n > 6)
                return 0;

            var r = await _context.Reimbursements
                .FirstOrDefaultAsync(x =>
                    x.ReimbursementId == h.ApprovalId &&
                    x.IsActive &&
                    x.FinalStatus == "Pending");

            if (r == null)
                return 0;

            var approverIds = new[]
            {
        r.Approver1Id,
        r.Approver2Id,
        r.Approver3Id,
        r.Approver4Id,
        r.Approver5Id,
        r.Approver6Id
    };

            var approverStatuses = new[]
            {
        r.Approver1Status,
        r.Approver2Status,
        r.Approver3Status,
        r.Approver4Status,
        r.Approver5Status,
        r.Approver6Status
    };

            if (approverIds[n - 1] != ceoId)
                return 0;

            if (!string.IsNullOrEmpty(approverStatuses[n - 1]) &&
                approverStatuses[n - 1] != "Pending")
                return 0;

            switch (n)
            {
                case 1: r.Approver1Status = status; break;
                case 2: r.Approver2Status = status; break;
                case 3: r.Approver3Status = status; break;
                case 4: r.Approver4Status = status; break;
                case 5: r.Approver5Status = status; break;
                case 6: r.Approver6Status = status; break;
            }

            if (status == "Approved")
            {
                var allApproved = true;

                for (int i = 0; i < 6; i++)
                {
                    if (approverIds[i] != null &&
                        approverStatuses[i] != "Approved" &&
                        i != n - 1)
                    {
                        allApproved = false;
                        break;
                    }
                }

                if (allApproved)
                    r.FinalStatus = "Approved";
            }
            else
            {
                r.FinalStatus = status;
            }

            return 1;
        }
    }

}
