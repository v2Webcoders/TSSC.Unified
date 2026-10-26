using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QUIZAPP;
using QUIZAPP.Models;
using TSSC.Unified.Models;
using TSSC.Unified.ViewModel;

namespace TSSC.Unified.Areas.CandidatePortal.Controllers
{
    [Area("CandidatePortal")]
    public class HomeController : Controller
    {
        private readonly AppdbContext _context;
        private readonly UserManager<AppUser> _userManager;

        public HomeController(
            AppdbContext context,
            UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet]
        [Authorize(Roles = "Candidate")]
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);

            if (string.IsNullOrEmpty(userId))
                return Forbid();

            var batches = await (
    from bc in _context.BatchCandidates.AsNoTracking()
    join b in _context.Batches.AsNoTracking()
        on bc.BatchRecordId equals b.Id

    join s in _context.Scheme.AsNoTracking()
        on b.SchemeId equals s.SchemeId into schemeGroup
    from s in schemeGroup.DefaultIfEmpty()

    join p in _context.Project.AsNoTracking()
        on b.ProjectId equals p.Id into projectGroup
    from p in projectGroup.DefaultIfEmpty()

    join c in _context.TrainingCenter.AsNoTracking()
        on b.TrainingCenterId equals c.Id into centerGroup
    from c in centerGroup.DefaultIfEmpty()

    join j in _context.JobRoles.AsNoTracking()
        on b.JobRoleId equals j.Id into jobRoleGroup
    from j in jobRoleGroup.DefaultIfEmpty()

    join t in _context.TrainerRegistration.AsNoTracking()
        on b.TrainerRegistrationId equals t.Id into trainerGroup
    from t in trainerGroup.DefaultIfEmpty()

    where bc.Candidate != null
          && bc.Candidate.ApplicationUserId == userId
          && b.Status == "Approved"
    select new BatchIndexVM
    {
        Id = b.Id,
        BatchId = b.BatchId,
        SchemeName = s != null ? s.SchemeName : "-",
        ProjectName = p != null ? p.ProjectName : "-",
        CenterName = c != null ? c.OrganizationName : "-",
        JobRoleName = j != null ? j.JobRoleTitle : "-",
        TrainerName = t != null ? t.CandidateName : "-",
        StartDate = b.StartDate,
        EndDate = b.EndDate,
        Status = b.Status,
        CreatedOn = b.CreatedOn
    })
    .Distinct()
    .ToListAsync();

            // Order after Distinct, because Distinct can discard an earlier OrderBy
            batches = batches.OrderByDescending(x => x.CreatedOn).ToList();

            var model = new CandidateDashboardVM
            {
                EnrolledBatches = batches.Count,
                // Replace "Completed" with your actual completion status if different.
                CompletedBatches = batches.Count(b =>
                    string.Equals(
                        b.Status,
                        "Completed",
                        StringComparison.OrdinalIgnoreCase)),
                RecentBatches = batches.Take(5).ToList()
            };

            return View(model);
        }
    }
}
