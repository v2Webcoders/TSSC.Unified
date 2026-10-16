using AspNetCore.ReportingServices.ReportProcessing.ReportObjectModel;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QUIZAPP;
using QUIZAPP.Models;
using TSSC.Unified.Models;
using TSSC.Unified.ViewModel;

namespace TSSC.Unified.Areas.HRMS.Controllers.TP
{
    [Area("HRMS")]
    public class TPRegistrationController : Controller
    {
        private readonly AppdbContext _context;
        private readonly UserManager<AppUser> _userManager;
        public TPRegistrationController(AppdbContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        //public async Task<IActionResult> Index(string tab = "pending")
        //{
        //    var user = await _userManager.GetUserAsync(User);

        //    if (user == null)
        //        return Unauthorized();

        //    tab = (tab ?? "pending").ToLowerInvariant();

        //    if (tab != "pending" &&
        //        tab != "approved" &&
        //        tab != "rejected")
        //    {
        //        tab = "pending";
        //    }

        //    var userId = user.Id;

        //    Get the stages assigned to the current user's roles.
        //    var roles = await _userManager.GetRolesAsync(user);

        //    var approvers = await _context.TPWorkflowApprovers
        //        .Where(x => x.IsActive)
        //        .ToListAsync();

        //    Only include stages assigned to this user or one of their roles.
        //    var assignedStageCodes = approvers
        //        .Where(x =>
        //            (x.ApproverType == "User" &&
        //             x.ApplicationUserId == userId)
        //            ||
        //            (x.ApproverType == "Role" &&
        //             x.RoleName != null &&
        //             roles.Contains(x.RoleName)))
        //        .Select(x => x.StageCode)
        //        .Distinct()
        //        .ToList();

        //    if (!assignedStageCodes.Any())
        //        return Forbid();

        //    IQueryable<TPRegistration> query =
        //        _context.TPRegistrations;

        //    if (tab == "pending")
        //    {
        //        Show registrations currently awaiting one of this user's stages.
        //        query = query.Where(x =>
        //            assignedStageCodes.Contains(x.CurrentStage) &&
        //            x.OverallApprovalStatus != "Approved" &&
        //            x.OverallApprovalStatus != "Rejected");
        //    }
        //    else
        //    {
        //        For completed decisions, filter by the stages assigned to this user.
        //         A registration appears if the user was assigned to a stage
        //         whose recorded outcome matches the selected tab.
        //        var matchingStageCodes = approvers
        //            .Where(x =>
        //                assignedStageCodes.Contains(x.StageCode))
        //            .Select(x => x.StageCode)
        //            .Distinct()
        //            .ToList();

        //        if (tab == "approved")
        //        {
        //            query = query.Where(x =>
        //                matchingStageCodes.Contains("REGIONAL") &&
        //                    x.Regional_Head_Status == "Approved"
        //                ||
        //                matchingStageCodes.Contains("AFFILIATIONS") &&
        //                    x.Affiliations_Status == "Approved"
        //                ||
        //                matchingStageCodes.Contains("FINANCE") &&
        //                    x.Finance_Status == "Approved"
        //                ||
        //                matchingStageCodes.Contains("VERTICAL_HEAD") &&
        //                    x.Vertical_Head_Status == "Approved"
        //                ||
        //                matchingStageCodes.Contains("SENIOR_REVIEWER_1") &&
        //                    x.SeniorReviewer1_Status == "Approved"
        //                ||
        //                matchingStageCodes.Contains("SENIOR_REVIEWER_2") &&
        //                    x.SeniorReviewer2_Status == "Approved"
        //                ||
        //                matchingStageCodes.Contains("CEO") &&
        //                    x.CEO_Status == "Approved"
        //                ||
        //                matchingStageCodes.Contains("PAYMENT_PROCESS") &&
        //                    x.PaymentProcess_Status == "Approved"
        //                ||
        //                matchingStageCodes.Contains("FINANCE_CERTIFICATE") &&
        //                    x.FinanceCertificate_Status == "Approved"
        //                ||
        //                matchingStageCodes.Contains("CERTIFICATE_ISSUANCE") &&
        //                    x.CertificateIssuance_Status == "Approved");
        //        }
        //        else // rejected
        //        {
        //            query = query.Where(x =>
        //                matchingStageCodes.Contains("REGIONAL") &&
        //                    x.Regional_Head_Status == "Rejected"
        //                ||
        //                matchingStageCodes.Contains("AFFILIATIONS") &&
        //                    x.Affiliations_Status == "Rejected"
        //                ||
        //                matchingStageCodes.Contains("FINANCE") &&
        //                    x.Finance_Status == "Rejected"
        //                ||
        //                matchingStageCodes.Contains("VERTICAL_HEAD") &&
        //                    x.Vertical_Head_Status == "Rejected"
        //                ||
        //                matchingStageCodes.Contains("SENIOR_REVIEWER_1") &&
        //                    x.SeniorReviewer1_Status == "Rejected"
        //                ||
        //                matchingStageCodes.Contains("SENIOR_REVIEWER_2") &&
        //                    x.SeniorReviewer2_Status == "Rejected"
        //                ||
        //                matchingStageCodes.Contains("CEO") &&
        //                    x.CEO_Status == "Rejected"
        //                ||
        //                matchingStageCodes.Contains("PAYMENT_PROCESS") &&
        //                    x.PaymentProcess_Status == "Rejected"
        //                ||
        //                matchingStageCodes.Contains("FINANCE_CERTIFICATE") &&
        //                    x.FinanceCertificate_Status == "Rejected"
        //                ||
        //                matchingStageCodes.Contains("CERTIFICATE_ISSUANCE") &&
        //                    x.CertificateIssuance_Status == "Rejected");
        //        }
        //    }

        //    var registrations = await query
        //        .OrderByDescending(x => x.CreatedOn)
        //        .ToListAsync();

        //    ViewBag.CurrentTab = tab;

        //    return View(query);
        //}

        public async Task<IActionResult> Index(string tab = "pending")
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            tab = (tab ?? "pending").ToLowerInvariant();
            if (tab != "pending" && tab != "approved" && tab != "rejected")
                tab = "pending";

            var userId = user.Id;
            var roles = await _userManager.GetRolesAsync(user);

            var approvers = await _context.TPWorkflowApprovers
                .Where(x => x.IsActive)
                .ToListAsync();

            var assignedStageCodes = approvers
                .Where(x =>
                    (x.ApproverType == "User" && x.ApplicationUserId == userId) ||
                    (x.ApproverType == "Role" && x.RoleName != null && roles.Contains(x.RoleName)))
                .Select(x => x.StageCode)
                .Distinct()
                .ToList();

            if (!assignedStageCodes.Any())
                return Forbid();

            IQueryable<TPRegistration> query = _context.TPRegistrations;

            if (tab == "pending")
            {
                bool hasRegional = assignedStageCodes.Contains("REGIONAL");
                bool hasAffil = assignedStageCodes.Contains("AFFILIATIONS");
                bool hasFinance = assignedStageCodes.Contains("FINANCE");
                bool hasVertical = assignedStageCodes.Contains("VERTICAL_HEAD");
                bool hasSenior1 = assignedStageCodes.Contains("SENIOR_REVIEWER_1");
                bool hasSenior2 = assignedStageCodes.Contains("SENIOR_REVIEWER_2");
                bool hasCeo = assignedStageCodes.Contains("CEO");
                bool hasPayment = assignedStageCodes.Contains("PAYMENT_PROCESS");
                bool hasFinCert = assignedStageCodes.Contains("FINANCE_CERTIFICATE");
                bool hasCertIssue = assignedStageCodes.Contains("CERTIFICATE_ISSUANCE");

                query = query.Where(x =>
                    x.OverallApprovalStatus != "Approved" &&
                    x.OverallApprovalStatus != "Rejected" &&
                    (
                        (hasRegional && x.CurrentStage == "REGIONAL") ||
                        (hasAffil && x.CurrentStage == "AFFILIATIONS") ||
                        (hasFinance && x.CurrentStage == "FINANCE") ||
                        (hasVertical && x.CurrentStage == "VERTICAL_HEAD") ||
                        (hasSenior1 && x.CurrentStage == "SENIOR_REVIEWER_1") ||
                        (hasSenior2 && x.CurrentStage == "SENIOR_REVIEWER_2") ||
                        (hasCeo && x.CurrentStage == "CEO") ||
                        (hasPayment && x.CurrentStage == "PAYMENT_PROCESS") ||
                        (hasFinCert && x.CurrentStage == "FINANCE_CERTIFICATE") ||
                        (hasCertIssue && x.CurrentStage == "CERTIFICATE_ISSUANCE")
                    ));
            }
            else
            {
                var status = tab == "approved" ? "Approved" : "Rejected";

                bool hasRegional = assignedStageCodes.Contains("REGIONAL");
                bool hasAffil = assignedStageCodes.Contains("AFFILIATIONS");
                bool hasFinance = assignedStageCodes.Contains("FINANCE");
                bool hasVertical = assignedStageCodes.Contains("VERTICAL_HEAD");
                bool hasSenior1 = assignedStageCodes.Contains("SENIOR_REVIEWER_1");
                bool hasSenior2 = assignedStageCodes.Contains("SENIOR_REVIEWER_2");
                bool hasCeo = assignedStageCodes.Contains("CEO");
                bool hasPayment = assignedStageCodes.Contains("PAYMENT_PROCESS");
                bool hasFinCert = assignedStageCodes.Contains("FINANCE_CERTIFICATE");
                bool hasCertIssue = assignedStageCodes.Contains("CERTIFICATE_ISSUANCE");

                query = query.Where(x =>
                    (hasRegional && x.Regional_Head_Status == status) ||
                    (hasAffil && x.Affiliations_Status == status) ||
                    (hasFinance && x.Finance_Status == status) ||
                    (hasVertical && x.Vertical_Head_Status == status) ||
                    (hasSenior1 && x.SeniorReviewer1_Status == status) ||
                    (hasSenior2 && x.SeniorReviewer2_Status == status) ||
                    (hasCeo && x.CEO_Status == status) ||
                    (hasPayment && x.PaymentProcess_Status == status) ||
                    (hasFinCert && x.FinanceCertificate_Status == status) ||
                    (hasCertIssue && x.CertificateIssuance_Status == status));
            }

            //var registrations = await query
            //    .OrderByDescending(x => x.CreatedOn)
            //    .ToListAsync();

            //ViewBag.CurrentTab = tab;
            //return View(registrations);

            var registrations = await query
    .AsNoTracking()
    .OrderByDescending(x => x.CreatedOn)
    .Select(x => new TPRegistrationListVM
    {
        Id = x.Id,

        OrganizationName = x.OrganizationName,
        OrganizationType = x.OrganizationType,
        RegistrationNumber = x.RegistrationNumber,

        AuthorizedSignatoryName = x.AuthorizedSignatoryName,
        Designation = x.Designation,

        Email = x.Email,
        MobileNumber = x.MobileNumber,
        CreatedOn = x.CreatedOn,

        Status= x.Status,   

        Regional_Head_Status = x.Regional_Head_Status,
        Regional_Head_Date = x.Regional_Head_Date,

        Affiliations_Status = x.Affiliations_Status,
        Affiliations_Date = x.Affiliations_Date,

        Finance_Status = x.Finance_Status,
        Finance_Date = x.Finance_Date,

        Vertical_Head_Status = x.Vertical_Head_Status,
        Vertical_Head_Date = x.Vertical_Head_Date,

        SeniorReviewer1_Status = x.SeniorReviewer1_Status,
        SeniorReviewer1_Date = x.SeniorReviewer1_Date,

        SeniorReviewer2_Status = x.SeniorReviewer2_Status,
        SeniorReviewer2_Date = x.SeniorReviewer2_Date,

        CEO_Status = x.CEO_Status,
        CEO_Date = x.CEO_Date,

        PaymentProcess_Status = x.PaymentProcess_Status,
        PaymentProcess_Date = x.PaymentProcess_Date,

        FinanceCertificate_Status = x.FinanceCertificate_Status,
        FinanceCertificate_Date = x.FinanceCertificate_Date,

        CertificateIssuance_Status = x.CertificateIssuance_Status,
        CertificateIssuance_Date = x.CertificateIssuance_Date,

        CurrentStage = x.CurrentStage
    })
    .ToListAsync();

            ViewBag.CurrentTab = tab;
            return View(registrations);
        }
        public async Task<IActionResult> Details(int id)
        {
            var registration = await _context.TPRegistrations
                .Include(x => x.Documents)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (registration == null)
                return NotFound();

            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return Unauthorized();

            var roles = await _userManager.GetRolesAsync(user);

            var employee = await _context.Employee
                .FirstOrDefaultAsync(x => x.ApplicationUserId == user.Id);

            if (employee == null)
                return Forbid();

            ViewBag.UserRole = roles.FirstOrDefault();

            ViewBag.EmployeeName =
                $"{employee.FirstName} {employee.LastName}".Trim();

            ViewBag.EmployeeCode = employee.EmployeeCode;

            ViewBag.ApprovedBy =
                $"{employee.FirstName} {employee.LastName}".Trim()
                + $" ({employee.EmployeeCode})";

            return View(registration);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Decide(
    int id,
    string decision,
    string? remarks)
        {
            if (decision is not ("Approved" or "Rejected"))
                return BadRequest();

            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return Unauthorized();

            var registration = await _context.TPRegistrations
                .FirstOrDefaultAsync(x => x.Id == id);

            if (registration == null)
                return NotFound();

            // Do not allow decisions after rejection or completion.
            if (registration.OverallApprovalStatus == "Rejected" ||
                registration.CurrentStage == "COMPLETED")
            {
                return Forbid();
            }

            var stage = registration.CurrentStage;

            var validStages = new[]
            {
        "REGIONAL",
        "AFFILIATIONS",
        "FINANCE",
        "VERTICAL_HEAD",
        "SENIOR_REVIEWER_1",
        "SENIOR_REVIEWER_2",
        "CEO",
        "PAYMENT_PROCESS",
        "CERTIFICATE_ISSUANCE"
    };

            if (string.IsNullOrWhiteSpace(stage) ||
                !validStages.Contains(stage))
            {
                return Forbid();
            }

            var roles = await _userManager.GetRolesAsync(user);

            // =====================================================
            // CHECK WHETHER USER IS ASSIGNED TO CURRENT STAGE
            // =====================================================

            var stageApprovers = await _context.TPWorkflowApprovers
                .Where(x => x.StageCode == stage && x.IsActive)
                .ToListAsync();

            var isAssigned = stageApprovers.Any(a =>
                (
                    string.Equals(
                        a.ApproverType,
                        "User",
                        StringComparison.OrdinalIgnoreCase)
                    &&
                    (
                        a.ApplicationUserId == user.Id ||
                        a.ApplicationUserId == user.UserName ||
                        a.ApplicationUserId == user.Email
                    )
                )
                ||
                (
                    string.Equals(
                        a.ApproverType,
                        "Role",
                        StringComparison.OrdinalIgnoreCase)
                    &&
                    !string.IsNullOrWhiteSpace(a.RoleName)
                    &&
                    roles.Contains(a.RoleName)
                )
            );

            if (!isAssigned)
                return Forbid();

            // =====================================================
            // GET EMPLOYEE DETAILS
            // =====================================================

            var employee = await _context.Employee
                .FirstOrDefaultAsync(x => x.ApplicationUserId == user.Id);

            if (employee == null)
                return Forbid();

            var employeeName =
                $"{employee.FirstName} {employee.LastName}".Trim();

            var approvedBy =
                $"{employeeName} ({employee.EmployeeCode})";

            var now = DateTime.UtcNow;

            // =====================================================
            // UPDATE CURRENT STAGE
            // =====================================================

            string? stageStatus = stage switch
            {
                "REGIONAL" => registration.Regional_Head_Status,
                "AFFILIATIONS" => registration.Affiliations_Status,
                "FINANCE" => registration.Finance_Status,
                "VERTICAL_HEAD" => registration.Vertical_Head_Status,
                "SENIOR_REVIEWER_1" => registration.SeniorReviewer1_Status,
                "SENIOR_REVIEWER_2" => registration.SeniorReviewer2_Status,
                "CEO" => registration.CEO_Status,
                "PAYMENT_PROCESS" => registration.PaymentProcess_Status,
                "CERTIFICATE_ISSUANCE" => registration.CertificateIssuance_Status,
                _ => null
            };

            // Only Pending stages can be processed.
            if (!string.Equals(
                stageStatus,
                "Pending",
                StringComparison.OrdinalIgnoreCase))
            {
                return Forbid();
            }

            switch (stage)
            {
                case "REGIONAL":
                    registration.Regional_Head_Status = decision;
                    registration.Regional_Head_Remarks = remarks;
                    registration.Regional_Head_ApprovedBy = approvedBy;
                    registration.Regional_Head_Date = now;
                    break;

                case "AFFILIATIONS":
                    registration.Affiliations_Status = decision;
                    registration.Affiliations_Remarks = remarks;
                    registration.Affiliations_ApprovedBy = approvedBy;
                    registration.Affiliations_Date = now;
                    break;

                case "FINANCE":
                    registration.Finance_Status = decision;
                    registration.Finance_Remarks = remarks;
                    registration.Finance_ApprovedBy = approvedBy;
                    registration.Finance_Date = now;
                    break;

                case "VERTICAL_HEAD":
                    registration.Vertical_Head_Status = decision;
                    registration.Vertical_Head_Remarks = remarks;
                    registration.Vertical_Head_ApprovedBy = approvedBy;
                    registration.Vertical_Head_Date = now;
                    break;

                case "SENIOR_REVIEWER_1":
                    registration.SeniorReviewer1_Status = decision;
                    registration.SeniorReviewer1_Remarks = remarks;
                    registration.SeniorReviewer1_ApprovedBy = approvedBy;
                    registration.SeniorReviewer1_Date = now;
                    break;

                case "SENIOR_REVIEWER_2":
                    registration.SeniorReviewer2_Status = decision;
                    registration.SeniorReviewer2_Remarks = remarks;
                    registration.SeniorReviewer2_ApprovedBy = approvedBy;
                    registration.SeniorReviewer2_Date = now;
                    break;

                case "CEO":
                    registration.CEO_Status = decision;
                    registration.CEO_Remarks = remarks;
                    registration.CEO_ApprovedBy = approvedBy;
                    registration.CEO_Date = now;
                    break;

                case "PAYMENT_PROCESS":

                    // Payment proof must exist before Finance can approve.
                    if (decision == "Approved" &&
                        (registration.PaymentProofStatus != "Pending" ||
                         string.IsNullOrWhiteSpace(registration.PaymentProofFileName)))
                    {
                        return BadRequest(
                            "Payment proof must be submitted before payment can be approved.");
                    }

                    registration.PaymentProcess_Status = decision;
                    registration.PaymentProcess_Remarks = remarks;
                    registration.PaymentProcess_ApprovedBy = approvedBy;
                    registration.PaymentProcess_Date = now;

                    if (decision == "Approved")
                    {
                        registration.PaymentProofStatus = "Verified";
                        // Mark certificate issuance as approved.
                        registration.CertificateIssuance_Status = "Approved";
                        registration.CertificateIssuance_Remarks =
                            "Certificate issuance approved after payment verification.";
                        registration.CertificateIssuance_ApprovedBy = approvedBy;
                        registration.CertificateIssuance_Date = now;

                        // Complete the workflow.
                        registration.CurrentStage = "COMPLETED";
                        registration.OverallApprovalStatus = "Approved";
                        registration.Status = "Approved";
                        registration.OverallApprovalStatusUpdatedOn = DateTime.UtcNow;
                    }
                    else
                    {
                        // Reject the proof without rejecting the entire application.
                        registration.PaymentProofStatus = "Rejected";
                    }

                    break;
                case "CERTIFICATE_ISSUANCE":
                    registration.CertificateIssuance_Status = decision;
                    registration.CertificateIssuance_Remarks = remarks;
                    registration.CertificateIssuance_ApprovedBy = approvedBy;
                    registration.CertificateIssuance_Date = now;
                    break;
            }

            if (decision == "Rejected" && stage == "PAYMENT_PROCESS")
            {
                registration.CurrentStage = "PAYMENT_PROCESS";
            }
            else if (decision == "Rejected")
            {
                registration.OverallApprovalStatus = "Rejected";
                registration.OverallApprovalStatusUpdatedOn = now;
                registration.Status = "Rejected";
            }
            else
            {
                if (stage != "PAYMENT_PROCESS")
                {
                    registration.CurrentStage = stage switch
                    {
                        "REGIONAL" => "AFFILIATIONS",
                        "AFFILIATIONS" => "FINANCE",
                        "FINANCE" => "VERTICAL_HEAD",
                        "VERTICAL_HEAD" => "SENIOR_REVIEWER_1",
                        "SENIOR_REVIEWER_1" => "SENIOR_REVIEWER_2",
                        "SENIOR_REVIEWER_2" => "CEO",
                        "CEO" => "PAYMENT_PROCESS",
                        "CERTIFICATE_ISSUANCE" => "COMPLETED",
                        _ => stage
                    };

                    if (registration.CurrentStage == "COMPLETED")
                    {
                        registration.OverallApprovalStatus = "Approved";
                        registration.OverallApprovalStatusUpdatedOn = now;
                        registration.Status = "Approved";
                    }
                    else
                    {
                        registration.OverallApprovalStatus = "In_Progress";
                        registration.OverallApprovalStatusUpdatedOn = now;
                        registration.Status = "Under Review";
                    }
                }
            }

            registration.UpdatedOn = now;

            await _context.SaveChangesAsync();

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id });
        }
    }
}
