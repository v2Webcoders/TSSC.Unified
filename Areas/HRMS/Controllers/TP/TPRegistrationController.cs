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
using System.IO.Compression;
using ClosedXML.Excel;

namespace TSSC.Unified.Areas.HRMS.Controllers.TP
{
    [Area("HRMS")]
    public class TPRegistrationController : Controller
    {
        private readonly AppdbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly IWebHostEnvironment _environment;
        public TPRegistrationController(
        AppdbContext context,
        UserManager<AppUser> userManager,
        IWebHostEnvironment environment)
        {
            _context = context;
            _userManager = userManager;
            _environment = environment;
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

        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //[Authorize]
        //public async Task<IActionResult> BulkDownload(List<int> selectedIds)
        //{
        //    if (selectedIds == null || selectedIds.Count == 0)
        //        return BadRequest("Please select at least one registration.");

        //    selectedIds = selectedIds
        //        .Where(id => id > 0)
        //        .Distinct()
        //        .ToList();

        //    if (selectedIds.Count == 0 || selectedIds.Count > 100)
        //        return BadRequest("Select between 1 and 100 registrations.");

        //    // Get selected registrations and organization names.
        //    var registrations = await _context.TPRegistrations
        //        .Where(r => selectedIds.Contains(r.Id))
        //        .Select(r => new
        //        {
        //            r.Id,
        //            r.OrganizationName
        //        })
        //        .ToDictionaryAsync(r => r.Id);

        //    // Ensure every requested registration exists.
        //    if (registrations.Count != selectedIds.Count)
        //        return BadRequest("One or more registrations are invalid.");

        //    // TODO: Restrict registrations to those the current user is
        //    // authorized to download before proceeding.

        //    // Get documents belonging to the selected registrations.
        //    var documents = await _context.TPRegistrationDocuments
        //        .Where(d => selectedIds.Contains(d.TPRegistrationId))
        //        .OrderBy(d => d.TPRegistrationId)
        //        .ThenBy(d => d.DocumentType)
        //        .ToListAsync();

        //    if (documents.Count == 0)
        //        return NotFound("No documents were found for the selected registrations.");

        //    // Assumed storage path:
        //    // wwwroot/uploads/tp-registrations/{TPRegistrationId}/{FileName}
        //    if (string.IsNullOrWhiteSpace(_environment.WebRootPath))
        //        return StatusCode(500, "The web root directory is not configured.");

        //    var uploadRoot = Path.Combine(
        //        _environment.WebRootPath,
        //        "uploads",
        //        "tp-registrations");

        //    if (!Directory.Exists(uploadRoot))
        //        return NotFound("The document storage directory was not found.");

        //    using var memoryStream = new MemoryStream();

        //    var addedFiles = 0;

        //    using (var archive = new ZipArchive(
        //        memoryStream,
        //        ZipArchiveMode.Create,
        //        leaveOpen: true))
        //    {
        //        foreach (var document in documents)
        //        {
        //            if (!registrations.TryGetValue(
        //                    document.TPRegistrationId,
        //                    out var registration))
        //            {
        //                continue;
        //            }

        //            var fileName = Path.GetFileName(document.FileName ?? string.Empty);

        //            if (string.IsNullOrWhiteSpace(fileName))
        //                continue;

        //            var registrationFolder = Path.GetFullPath(
        //                Path.Combine(
        //                    uploadRoot,
        //                    document.TPRegistrationId.ToString()));

        //            var fullPath = Path.GetFullPath(
        //                Path.Combine(registrationFolder, fileName));

        //            // Ensure the resolved file stays inside its registration folder.
        //            var expectedPrefix = registrationFolder
        //                .TrimEnd(Path.DirectorySeparatorChar)
        //                + Path.DirectorySeparatorChar;

        //            if (!fullPath.StartsWith(
        //                    expectedPrefix,
        //                    StringComparison.OrdinalIgnoreCase))
        //            {
        //                continue;
        //            }

        //            if (!System.IO.File.Exists(fullPath))
        //                continue;

        //            // Use the organization name as the ZIP folder.
        //            var organizationName = string.IsNullOrWhiteSpace(
        //                registration.OrganizationName)
        //                    ? $"Registration_{document.TPRegistrationId}"
        //                    : registration.OrganizationName.Trim();

        //            foreach (var invalidChar in Path.GetInvalidFileNameChars())
        //            {
        //                organizationName = organizationName.Replace(invalidChar, '_');
        //            }

        //            //// Include document ID to prevent duplicate ZIP entry names.
        //            //var entryName =
        //            //    $"{organizationName}/{document.Id}_{fileName}";

        //            //archive.CreateEntryFromFile(
        //            //    fullPath,
        //            //    entryName,
        //            //    CompressionLevel.Optimal);

        //            //addedFiles++;
        //            // Convert DocumentType into a readable filename
        //            var documentType = string.IsNullOrWhiteSpace(document.DocumentType)
        //                ? "Document"
        //                : document.DocumentType.Trim();

        //            // Replace spaces and invalid filename characters
        //            foreach (var invalidChar in Path.GetInvalidFileNameChars())
        //            {
        //                documentType = documentType.Replace(invalidChar, '_');
        //            }

        //            documentType = string.Join(
        //                "_",
        //                documentType.Split(
        //                    new[] { ' ', '-', '_' },
        //                    StringSplitOptions.RemoveEmptyEntries));

        //            // Preserve the original file extension
        //            var extension = Path.GetExtension(fileName);

        //            // Create a meaningful filename
        //            var originalFileName = Path.GetFileNameWithoutExtension(fileName);
        //            var renamedFile = $"{documentType}_{originalFileName}{extension}";

        //            // ZIP path: OrganizationName/DocumentType_Id.extension
        //            var entryName = $"{organizationName}/{renamedFile}";

        //            archive.CreateEntryFromFile(
        //                fullPath,
        //                entryName,
        //                CompressionLevel.Optimal);

        //            addedFiles++;
        //        }
        //    }

        //    if (addedFiles == 0)
        //        return NotFound("No physical documents were found for the selected registrations.");

        //    return File(
        //        memoryStream.ToArray(),
        //        "application/zip",
        //        $"TPRegistrations_{DateTime.Now:yyyyMMdd_HHmmss}.zip");
        //}

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> BulkDownload(List<int> selectedIds)
        {
            if (selectedIds == null || selectedIds.Count == 0)
                return BadRequest("Please select at least one registration.");

            selectedIds = selectedIds
                .Where(id => id > 0)
                .Distinct()
                .ToList();

            if (selectedIds.Count == 0 || selectedIds.Count > 100)
                return BadRequest("Select between 1 and 100 registrations.");

            // 1. Get selected registrations and their Excel details.
            //var registrations = await _context.TPRegistrations
            //    .Where(r => selectedIds.Contains(r.Id))
            //    .Select(r => new
            //    {
            //        r.Id,
            //        r.OrganizationName,
            //        r.OrganizationType,
            //        r.RegistrationNumber,
            //        r.AuthorizedSignatoryName,
            //        r.Designation,
            //        r.Email,
            //        r.MobileNumber,
            //        r.CreatedOn,
            //        r.OverallApprovalStatus,
            //        r.CurrentStage
            //    })
            //    .ToDictionaryAsync(r => r.Id);
            var registrations = await _context.TPRegistrations
    .Where(r => selectedIds.Contains(r.Id))
    .ToDictionaryAsync(r => r.Id);
            if (registrations.Count != selectedIds.Count)
                return BadRequest("One or more registrations are invalid.");

            // IMPORTANT: Apply your Affiliations user's authorization
            // rules to the selected registrations before downloading.

            // 2. Create Excel workbook.
            //        using var workbook = new XLWorkbook();

            //        var worksheet = workbook.Worksheets.Add("Registrations");

            //        string[] headers =
            //        {
            //    "Registration ID",
            //    "Organization Name",
            //    "Organization Type",
            //    "Registration Number",
            //    "Applicant / Signatory",
            //    "Designation",
            //    "Email",
            //    "Mobile Number",
            //    "Submitted On",
            //    "Overall Approval Status",
            //    "Current Stage"
            //};

            //        for (int col = 0; col < headers.Length; col++)
            //        {
            //            worksheet.Cell(1, col + 1).Value = headers[col];
            //        }

            //        var headerRange = worksheet.Range(
            //            1, 1, 1, headers.Length);

            //        headerRange.Style.Font.Bold = true;
            //        headerRange.Style.Fill.BackgroundColor =
            //            XLColor.LightBlue;

            //        int row = 2;

            //        foreach (var registration in registrations.Values
            //                     .OrderBy(r => r.OrganizationName))
            //        {
            //            worksheet.Cell(row, 1).Value = registration.Id;
            //            worksheet.Cell(row, 2).Value = registration.OrganizationName ?? "";
            //            worksheet.Cell(row, 3).Value = registration.OrganizationType ?? "";
            //            worksheet.Cell(row, 4).Value = registration.RegistrationNumber ?? "";
            //            worksheet.Cell(row, 5).Value = registration.AuthorizedSignatoryName ?? "";
            //            worksheet.Cell(row, 6).Value = registration.Designation ?? "";
            //            worksheet.Cell(row, 7).Value = registration.Email ?? "";
            //            worksheet.Cell(row, 8).Value = registration.MobileNumber ?? "";

            //            worksheet.Cell(row, 9).Value = registration.CreatedOn;
            //            worksheet.Cell(row, 9).Style.DateFormat.Format = "dd MMM yyyy";

            //            worksheet.Cell(row, 10).Value =
            //                registration.OverallApprovalStatus ?? "";

            //            worksheet.Cell(row, 11).Value =
            //                registration.CurrentStage ?? "";

            //            row++;
            //        }

            //        worksheet.SheetView.FreezeRows(1);

            //        worksheet.Range(
            //            1, 1, row - 1, headers.Length).SetAutoFilter();

            //        worksheet.Columns().AdjustToContents();

            //        using var excelStream = new MemoryStream();

            //        workbook.SaveAs(excelStream);
            //        excelStream.Position = 0;
            // Create Excel workbook
            using var workbook = new XLWorkbook();

            var worksheet = workbook.Worksheets.Add("Registrations");

            string[] baseHeaders =
            {
    "Registration ID",
    "Organization Name",
    "Organization Type",
    "Registration Number",
    "Applicant / Signatory",
    "Designation",
    "Email",
    "Mobile Number",
    "Submitted On",
    "Overall Approval Status",
    "Current Stage"
};

            var approvalStages = new[]
            {
    "Regional Head",
    "Affiliations",
    "Finance",
    "Vertical Head",
    "Senior Reviewer 1",
    "Senior Reviewer 2",
    "CEO",
    "Payment Process",
    "Certificate Issuance"
};

            // Base headers
            int col = 1;

            foreach (var header in baseHeaders)
            {
                worksheet.Cell(1, col++).Value = header;
            }

            // Four columns for each approval stage
            foreach (var stage in approvalStages)
            {
                worksheet.Cell(1, col++).Value = $"{stage} Status";
                worksheet.Cell(1, col++).Value = $"{stage} Date";
                worksheet.Cell(1, col++).Value = $"{stage} Remarks";
                worksheet.Cell(1, col++).Value = $"{stage} Approved By";
            }

            // Populate rows
            int row = 2;

            foreach (var registration in registrations.Values
                         .OrderBy(r => r.OrganizationName))
            {
                col = 1;

                worksheet.Cell(row, col++).Value = registration.Id;
                worksheet.Cell(row, col++).Value = registration.OrganizationName ?? "";
                worksheet.Cell(row, col++).Value = registration.OrganizationType ?? "";
                worksheet.Cell(row, col++).Value = registration.RegistrationNumber ?? "";
                worksheet.Cell(row, col++).Value = registration.AuthorizedSignatoryName ?? "";
                worksheet.Cell(row, col++).Value = registration.Designation ?? "";
                worksheet.Cell(row, col++).Value = registration.Email ?? "";
                worksheet.Cell(row, col++).Value = registration.MobileNumber ?? "";

                if (registration.CreatedOn != default)
                {
                    worksheet.Cell(row, col).Value = registration.CreatedOn;
                    worksheet.Cell(row, col).Style.DateFormat.Format = "dd MMM yyyy";
                }
                col++;

                worksheet.Cell(row, col++).Value =
                    registration.OverallApprovalStatus ?? "";

                worksheet.Cell(row, col++).Value =
                    registration.CurrentStage ?? "";

                // All approval details from your database
                var approvals = new (string? Status, DateTime? Date,
                                     string? Remarks, string? ApprovedBy)[]
                {
        (
            registration.Regional_Head_Status,
            registration.Regional_Head_Date,
            registration.Regional_Head_Remarks,
            registration.Regional_Head_ApprovedBy
        ),
        (
            registration.Affiliations_Status,
            registration.Affiliations_Date,
            registration.Affiliations_Remarks,
            registration.Affiliations_ApprovedBy
        ),
        (
            registration.Finance_Status,
            registration.Finance_Date,
            registration.Finance_Remarks,
            registration.Finance_ApprovedBy
        ),
        (
            registration.Vertical_Head_Status,
            registration.Vertical_Head_Date,
            registration.Vertical_Head_Remarks,
            registration.Vertical_Head_ApprovedBy
        ),
        (
            registration.SeniorReviewer1_Status,
            registration.SeniorReviewer1_Date,
            registration.SeniorReviewer1_Remarks,
            registration.SeniorReviewer1_ApprovedBy
        ),
        (
            registration.SeniorReviewer2_Status,
            registration.SeniorReviewer2_Date,
            registration.SeniorReviewer2_Remarks,
            registration.SeniorReviewer2_ApprovedBy
        ),
        (
            registration.CEO_Status,
            registration.CEO_Date,
            registration.CEO_Remarks,
            registration.CEO_ApprovedBy
        ),
        (
            registration.PaymentProcess_Status,
            registration.PaymentProcess_Date,
            registration.PaymentProcess_Remarks,
            registration.PaymentProcess_ApprovedBy
        ),
        (
            registration.CertificateIssuance_Status,
            registration.CertificateIssuance_Date,
            registration.CertificateIssuance_Remarks,
            registration.CertificateIssuance_ApprovedBy
        )
                };

                foreach (var approval in approvals)
                {
                    worksheet.Cell(row, col++).Value = approval.Status ?? "";

                    if (approval.Date.HasValue)
                    {
                        worksheet.Cell(row, col).Value = approval.Date.Value;
                        worksheet.Cell(row, col).Style.DateFormat.Format =
                            "dd MMM yyyy";
                    }
                    col++;

                    worksheet.Cell(row, col++).Value = approval.Remarks ?? "";
                    worksheet.Cell(row, col++).Value = approval.ApprovedBy ?? "";
                }

                row++;
            }

            // Format the Excel sheet
            var headerRange = worksheet.Range(
                1, 1, 1, baseHeaders.Length + approvalStages.Length * 4);

            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightBlue;
            headerRange.Style.Alignment.WrapText = true;

            worksheet.SheetView.FreezeRows(1);

            worksheet.Range(
                1, 1, row - 1, baseHeaders.Length + approvalStages.Length * 4
            ).SetAutoFilter();

            // Set reasonable widths so remarks don't make columns excessively wide.
            worksheet.Columns().AdjustToContents(1, 30);

            foreach (var column in worksheet.Columns())
            {
                if (column.Width > 35)
                    column.Width = 35;
            }

            // Generate Excel in memory
            using var excelStream = new MemoryStream();

            workbook.SaveAs(excelStream);
            excelStream.Position = 0;
            // 3. Get document metadata for the selected registrations.
            var documents = await _context.TPRegistrationDocuments
                .Where(d => selectedIds.Contains(d.TPRegistrationId))
                .OrderBy(d => d.TPRegistrationId)
                .ThenBy(d => d.DocumentType)
                .ToListAsync();

            if (string.IsNullOrWhiteSpace(_environment.WebRootPath))
                return StatusCode(500, "The web root directory is not configured.");

            // Assumed storage:
            // wwwroot/uploads/tp-registrations/{RegistrationId}/{FileName}
            var uploadRoot = Path.Combine(
                _environment.WebRootPath,
                "uploads",
                "tp-registrations");

            using var zipStream = new MemoryStream();

            using (var archive = new ZipArchive(
                zipStream,
                ZipArchiveMode.Create,
                leaveOpen: true))
            {
                // 4. Add the Excel registration list to the ZIP.
                var excelEntry = archive.CreateEntry(
                    "TP_Registrations.xlsx",
                    CompressionLevel.Optimal);

                using (var entryStream = excelEntry.Open())
                {
                    excelStream.CopyTo(entryStream);
                }

                // 5. Add each selected registration's documents.
                int addedFiles = 0;

                foreach (var document in documents)
                {
                    if (!registrations.TryGetValue(
                            document.TPRegistrationId,
                            out var registration))
                    {
                        continue;
                    }

                    var fileName = Path.GetFileName(
                        document.FileName ?? "");

                    if (string.IsNullOrWhiteSpace(fileName))
                        continue;

                    var registrationFolder = Path.GetFullPath(
                        Path.Combine(
                            uploadRoot,
                            document.TPRegistrationId.ToString()));

                    var fullPath = Path.GetFullPath(
                        Path.Combine(registrationFolder, fileName));

                    var expectedPrefix = registrationFolder
                        .TrimEnd(Path.DirectorySeparatorChar)
                        + Path.DirectorySeparatorChar;

                    if (!fullPath.StartsWith(
                            expectedPrefix,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    if (!System.IO.File.Exists(fullPath))
                        continue;

                    // Organization name as the folder name.
                    var organizationName = string.IsNullOrWhiteSpace(
                        registration.OrganizationName)
                            ? $"Registration_{document.TPRegistrationId}"
                            : registration.OrganizationName.Trim();

                    foreach (var invalidChar in Path.GetInvalidFileNameChars())
                    {
                        organizationName =
                            organizationName.Replace(invalidChar, '_');
                    }

                    // Friendly document-type prefix + original filename.
                    var documentType = string.IsNullOrWhiteSpace(
                        document.DocumentType)
                            ? "Document"
                            : document.DocumentType.Trim();

                    foreach (var invalidChar in Path.GetInvalidFileNameChars())
                    {
                        documentType = documentType.Replace(invalidChar, '_');
                    }

                    documentType = string.Join(
                        "_",
                        documentType.Split(
                            new[] { ' ', '-', '_' },
                            StringSplitOptions.RemoveEmptyEntries));

                    var extension = Path.GetExtension(fileName);

                    var originalFileName =
                        Path.GetFileNameWithoutExtension(fileName);

                    var renamedFile =
                        $"{documentType}_{originalFileName}{extension}";

                    var entryName =
                        $"{organizationName}/{renamedFile}";

                    archive.CreateEntryFromFile(
                        fullPath,
                        entryName,
                        CompressionLevel.Optimal);

                    addedFiles++;
                }
            }

            // The Excel file is still useful even if some or all
            // selected registrations have no accessible physical documents.
            zipStream.Position = 0;

            return File(
                zipStream.ToArray(),
                "application/zip",
                $"TPRegistrations_{DateTime.Now:yyyyMMdd_HHmmss}.zip");
        }

    }
}
