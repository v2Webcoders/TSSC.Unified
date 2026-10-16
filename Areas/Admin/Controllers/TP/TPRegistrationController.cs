using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QUIZAPP;
using QUIZAPP.Models;
using TSSC.Unified.Models;
using TSSC.Unified.ViewModel;

namespace TSSC.Unified.Areas.Admin.Controllers.TP
{
    [Area("Admin")]
    public class TPRegistrationController : Controller
    {
        private readonly AppdbContext _context;
        private readonly UserManager<AppUser> _userManager;
        public TPRegistrationController(AppdbContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index(bool approvedOnly = false)
        {
            IQueryable<TPRegistration> query = _context.TPRegistrations;

            // =====================================================
            // APPROVED & ONBOARDED
            // =====================================================
            if (approvedOnly)
            {
                query = query.Where(x =>
                    x.Status == "Approved" &&
                    x.CEO_Status == "Approved");
            }

            // =====================================================
            // ALL REGISTRATIONS
            // =====================================================
            // No filter when approvedOnly = false

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

        Status=x.Status,

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

            ViewBag.ApprovedOnly = approvedOnly;

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


            ViewBag.UserRole = roles.FirstOrDefault();

            return View(registration);
        }

        
    }
}
