using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QUIZAPP.Areas.Admin.ViewModels;

namespace QUIZAPP.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class UserLogController : Controller
    {
        private readonly AppdbContext _db;

        public UserLogController(AppdbContext db)
        {
            _db = db;
        }

        public IActionResult Index()
        {
            ViewBag.Title = "User Login History";
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> GetLoginHistory()
        {
            var data = await _db.UserLoginHistory
                .OrderByDescending(x => x.ActionTime)
                .Select(x => new
                {
                    x.Id,
                    x.UserId,
                    x.UserType,
                    x.Action,
                    ActionTime = x.ActionTime.ToString("yyyy-MM-dd HH:mm:ss"),
                    x.IPAddress,
                    x.BrowserInfo
                }).ToListAsync();

            return Json(new { data });
        }
    }
}
