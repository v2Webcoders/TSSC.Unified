using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QUIZAPP;
using TSSC.Unified.Models;
using TSSC.Unified.ViewModel;
using iText.Commons.Internal.Runtime;
using Microsoft.AspNetCore.Identity;
using QUIZAPP.Models;
using QUIZAPP.Services;

namespace TSSC.Unified.Areas.CandidatePortal.Controllers
{
    [Area("CandidatePortal")]
    [Authorize(Roles = "Candidate")]
    public class BatchController : Controller
    {
        private readonly AppdbContext _context;
        private readonly EmailService _emailService;
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _environment;

        public BatchController(
            AppdbContext context,
            EmailService emailService,
            UserManager<AppUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IConfiguration configuration,
            IWebHostEnvironment environment)
        {
            _context = context;
            _emailService = emailService;
            _userManager = userManager;
            _roleManager = roleManager;
            _configuration = configuration;
            _environment = environment;
        }


        [HttpGet]
        [Authorize(Roles = "Candidate")]
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);

            if (string.IsNullOrEmpty(userId))
                return Forbid();

            // Find the logged-in candidate's enrolled batches.
            var batches = await _context.BatchCandidates
                .AsNoTracking()
                .Where(bc => bc.Candidate.ApplicationUserId == userId)
                .Select(bc => bc.Batch)
                .Where(b => b != null && b.Status == "Approved")
                .Distinct()
                .OrderByDescending(b => b.CreatedOn)
                .Select(x => new BatchIndexVM
                {
                    Id = x.Id,
                    BatchId = x.BatchId,
                    StartDate = x.StartDate,
                    EndDate = x.EndDate,
                    Status = x.Status,
                    CreatedOn = x.CreatedOn,

                    ApprovalRemarks = x.ApprovalRemarks,
                    ApprovedOn = x.ApprovedOn,

                    CandidateCount = x.Candidates.Count(),

                    ProjectName = _context.Project
                        .Where(p => p.Id == x.ProjectId)
                        .Select(p => p.ProjectName)
                        .FirstOrDefault(),

                    SchemeName = _context.Scheme
                        .Where(s => s.SchemeId == x.SchemeId)
                        .Select(s => s.SchemeName)
                        .FirstOrDefault(),

                    CenterName = _context.TrainingCenter
                        .Where(c => c.Id == x.TrainingCenterId)
                        .Select(c => c.TrainingCenterName)
                        .FirstOrDefault(),

                    JobRoleName = _context.JobRoles
                        .Where(j => j.Id == x.JobRoleId)
                        .Select(j => j.JobRoleTitle)
                        .FirstOrDefault(),

                    TrainerName = _context.TrainerRegistration
                        .Where(t => t.Id == x.TrainerRegistrationId)
                        .Select(t => t.CandidateName)
                        .FirstOrDefault(),

                    
                })
                .ToListAsync();

            return View(batches);
        }











    }
}
