using Microsoft.AspNetCore.Mvc;
using QUIZAPP.Areas.Admin.ViewModels;
using X.PagedList;
using X.PagedList.Extensions;

namespace QUIZAPP.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class DealerController : Controller
    {
        private readonly AppdbContext _db;

        public DealerController(AppdbContext db)
        {
            _db = db;
        }



        public IActionResult Index()
        {
            //int pageSize = 20;
            //int pageNumber = page ?? 1;

            var query = _db.Dealer
                           .OrderBy(d => d.Name)   // must order before paging
                           .Select(d => new DealerMasterVM
                           {
                               Id = d.Id,
                               DealerCode = d.DealerCode,
                               DealerName = d.Name,
                               Pincode = d.Pincode,
                               DealerStatus = d.DealerStatus,
                               DType = d.DType
                           }).ToList();

            //var pagedList = query.ToPagedList(pageNumber, pageSize);  // EF generates SQL OFFSET/FETCH
            ViewBag.Count = query.Count().ToString();
            return View(query);
        }

    }

}
