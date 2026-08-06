using DocumentFormat.OpenXml.InkML;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QUIZAPP;
using QUIZAPP.Models;
using TSSC.Unified.Models;
using TSSC.Unified.ViewModel;

namespace TSSC.Unified.Areas.HRMS.Controllers
{
    [Area("HRMS")]
    [Authorize(Roles = "HR,Employee")]
    public class LetterController :Controller
    {
        private readonly AppdbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly IWebHostEnvironment _environment;

        public LetterController(AppdbContext context, UserManager<AppUser> userManager, IWebHostEnvironment environment)
        {
            _context = context;
            _userManager = userManager;
            _environment = environment;
        }
        public IActionResult LetterReceive()
        {
            LetterRegisterVM model = new LetterRegisterVM();

            model.EmployeeList =_context.Employee
                .Where(x => x.IsActive)
                .Select(x => new SelectListItem
                {
                    Text = x.FirstName + " " + x.LastName,
                    Value = x.EmployeeId.ToString()
                }).ToList();

            return View(model);
        }
        [HttpPost]
        public IActionResult LetterReceive(LetterRegisterVM model)
        {
            if (ModelState.IsValid)
            {
                if (model.LetterAttachmentFile != null)
                {
                    string folder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads/letters");

                    if (!Directory.Exists(folder))
                    {
                        Directory.CreateDirectory(folder);
                    }

                    string fileName = Guid.NewGuid() + Path.GetExtension(model.LetterAttachmentFile.FileName);

                    string filePath = Path.Combine(folder, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        model.LetterAttachmentFile.CopyTo(stream);
                    }

                    model.LetterAttachment = "/uploads/letters/" + fileName;
                }
                LetterRegister obj = new LetterRegister();

                obj.LetterType = "Received";

                obj.ItemName = model.ItemName;
                obj.SenderName = model.SenderName;
                obj.Mode = model.Mode;
                obj.DocketNo = model.DocketNo;
                obj.ReceivedBy = model.ReceivedBy;
                obj.DelegatedBy = model.DelegatedBy;
                obj.DelegatedTo = model.DelegatedTo;
                obj.Handover = model.Handover;
                obj.HandoverDate = model.HandoverDate;
                obj.Remarks = model.Remarks;
                obj.LetterAttachment = model.LetterAttachment;

                obj.CreatedOn = DateTime.Now;
                obj.IsActive = true;

                _context.LetterRegister.Add(obj);
                _context.SaveChanges();

                return RedirectToAction("LetterReceive");
            }

            return View(model);
        }
        public IActionResult LetterSend()
        {
            LetterRegisterVM model = new LetterRegisterVM();

            model.EmployeeList = _context.Employee
                .Where(x => x.IsActive)
                .Select(x => new SelectListItem
                {
                    Text = x.FirstName + " " + x.LastName,
                    Value = x.EmployeeId.ToString()
                }).ToList();

            return View(model);
        }
        [HttpPost]
        public IActionResult LetterSend(LetterRegisterVM model)
        {
            if (ModelState.IsValid)
            {
                if (model.LetterAttachmentFile != null)
                {
                    string folder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads/letters");

                    if (!Directory.Exists(folder))
                    {
                        Directory.CreateDirectory(folder);
                    }

                    string fileName = Guid.NewGuid() + Path.GetExtension(model.LetterAttachmentFile.FileName);

                    string filePath = Path.Combine(folder, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        model.LetterAttachmentFile.CopyTo(stream);
                    }

                    model.LetterAttachment = "/uploads/letters/" + fileName;
                }
                LetterRegister obj = new LetterRegister();

                obj.LetterType = "Send";

                obj.ItemName = model.ItemName;
                obj.ReceiverName = model.ReceiverName;
                obj.Mode = model.Mode;
                obj.DocketNo = model.DocketNo;
                obj.SentBy = model.SentBy;
                obj.Handover = model.Handover;
                obj.HandoverDate = model.HandoverDate;
                obj.Remarks = model.Remarks;
                obj.LetterAttachment = model.LetterAttachment;

                obj.CreatedOn = DateTime.Now;
                obj.IsActive = true;

                _context.LetterRegister.Add(obj);
                _context.SaveChanges();

                return RedirectToAction("LetterSend");
            }

            return View(model);
        }
    }
}
