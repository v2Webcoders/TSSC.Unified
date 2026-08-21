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
    [Authorize]
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
     
        public IActionResult LetterReceive(int? id)
        {
            LetterRegisterVM model = new LetterRegisterVM();

            model.EmployeeList = _context.Employee
                .Where(x => x.IsActive)
                .Select(x => new SelectListItem
                {
                    Text = x.FirstName + " " + x.LastName,
                    Value = x.EmployeeId.ToString()
                }).ToList();

            if (id != null)
            {
                var data = _context.LetterRegister.FirstOrDefault(x => x.LetterId == id);

                if (data != null)
                {
                    model.LetterId = data.LetterId;
                    model.ItemName = data.ItemName;
                    model.SenderName = data.SenderName;
                    model.Mode = data.Mode;
                    model.DocketNo = data.DocketNo;
                    model.ReceivedBy = data.ReceivedBy;
                    model.DelegatedBy = data.DelegatedBy;
                    model.DelegatedTo = data.DelegatedTo;
                    model.Handover = data.Handover;
                    model.HandoverDate = data.HandoverDate;
                    model.Remarks = data.Remarks;
                    model.LetterAttachment = data.LetterAttachment;
                }
            }

            return View(model);
        }
        [HttpPost]
        public IActionResult LetterReceive(LetterRegisterVM model)
        {
            if (!ModelState.IsValid)
            {
                model.EmployeeList = _context.Employee
                    .Where(x => x.IsActive)
                    .Select(x => new SelectListItem
                    {
                        Text = x.FirstName + " " + x.LastName,
                        Value = x.EmployeeId.ToString()
                    }).ToList();

                return View(model);
            }

            LetterRegister obj;

            // Edit
            if (model.LetterId > 0)
            {
                obj = _context.LetterRegister.FirstOrDefault(x => x.LetterId == model.LetterId);

                if (obj == null)
                    return NotFound();
            }
            // Add
            else
            {
                obj = new LetterRegister();
                obj.CreatedOn = DateTime.Now;
                obj.IsActive = true;
                obj.LetterType = "Received";
            }

            // Upload Attachment
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

                obj.LetterAttachment = "/uploads/letters/" + fileName;
            }

            // Common Fields
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

            if (model.LetterId == 0)
            {
                _context.LetterRegister.Add(obj);
            }
            else
            {
                _context.LetterRegister.Update(obj);
            }

            _context.SaveChanges();

            return RedirectToAction("LetterReceiveList");
        }
        public IActionResult LetterSend(int? id)
        {
            LetterRegisterVM model = new LetterRegisterVM();

            model.EmployeeList = _context.Employee
                .Where(x => x.IsActive)
                .Select(x => new SelectListItem
                {
                    Text = x.FirstName + " " + x.LastName,
                    Value = x.EmployeeId.ToString()
                }).ToList();

            if (id != null)
            {
                var data = _context.LetterRegister
                    .FirstOrDefault(x => x.LetterId == id);

                if (data != null)
                {
                    model.LetterId = data.LetterId;
                    model.ItemName = data.ItemName;
                    model.ReceiverName = data.ReceiverName;
                    model.Mode = data.Mode;
                    model.DocketNo = data.DocketNo;
                    model.SentBy = data.SentBy;
                    model.DelegatedBy = data.DelegatedBy;
                    model.DelegatedTo = data.DelegatedTo;
                    model.Handover = data.Handover;
                    model.HandoverDate = data.HandoverDate;
                    model.Remarks = data.Remarks;
                    model.LetterAttachment = data.LetterAttachment;
                }
            }

            return View(model);
        }

        [HttpPost]
        public IActionResult LetterSend(LetterRegisterVM model)
        {
            if (!ModelState.IsValid)
            {
                model.EmployeeList = _context.Employee
                    .Where(x => x.IsActive)
                    .Select(x => new SelectListItem
                    {
                        Text = x.FirstName + " " + x.LastName,
                        Value = x.EmployeeId.ToString()
                    }).ToList();

                return View(model);
            }

            LetterRegister obj;

            // Edit
            if (model.LetterId > 0)
            {
                obj = _context.LetterRegister.FirstOrDefault(x => x.LetterId == model.LetterId);

                if (obj == null)
                {
                    return NotFound();
                }
            }
            // Add
            else
            {
                obj = new LetterRegister();
                obj.CreatedOn = DateTime.Now;
                obj.IsActive = true;
                obj.LetterType = "Send";
            }

            // Upload Attachment
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

                obj.LetterAttachment = "/uploads/letters/" + fileName;
            }

            // Common Fields
            obj.ItemName = model.ItemName;
            obj.ReceiverName = model.ReceiverName;
            obj.Mode = model.Mode;
            obj.DocketNo = model.DocketNo;
            obj.SentBy = model.SentBy;
            obj.Handover = model.Handover;
            obj.HandoverDate = model.HandoverDate;
            obj.Remarks = model.Remarks;

            if (model.LetterId == 0)
            {
                _context.LetterRegister.Add(obj);
            }
            else
            {
                _context.LetterRegister.Update(obj);
            }

            _context.SaveChanges();

            return RedirectToAction("LetterSendList");
        }

        public IActionResult LetterReceiveList()
        {
            var model = _context.LetterRegister
                .Where(x => x.IsActive && x.LetterType == "Received")
                .Select(x => new LetterRegisterVM
                {
                    LetterId = x.LetterId,
                    ItemName = x.ItemName,
                    SenderName = x.SenderName,
                    Mode = x.Mode,
                    DocketNo = x.DocketNo,
                    ReceivedBy = x.ReceivedBy,
                    DelegatedBy = x.DelegatedBy,
                    DelegatedTo = x.DelegatedTo,
                    Handover = x.Handover,
                    HandoverDate = x.HandoverDate,
                    LetterAttachment = x.LetterAttachment,
                    Remarks = x.Remarks,
                    CreatedOn = x.CreatedOn
                }).ToList();

            return View(model);
        }
        public IActionResult LetterSendList()
        {
            var model = _context.LetterRegister
                .Where(x => x.IsActive && x.LetterType == "Send")
                .Select(x => new LetterRegisterVM
                {
                    LetterId = x.LetterId,
                    ItemName = x.ItemName,
                    ReceiverName = x.ReceiverName,
                    Mode = x.Mode,
                    DocketNo = x.DocketNo,
                    SentBy = x.SentBy,
                    Handover = x.Handover,
                    HandoverDate = x.HandoverDate,
                    LetterAttachment = x.LetterAttachment,
                    Remarks = x.Remarks,
                    CreatedOn = x.CreatedOn
                }).ToList();

            return View(model);
        }
        public IActionResult Delete(int id)
        {
            var data = _context.LetterRegister.FirstOrDefault(x => x.LetterId == id);

            if (data == null)
            {
                return NotFound();
            }

            // for Soft Delete
            data.IsActive = false;

            _context.LetterRegister.Update(data);
            _context.SaveChanges();

            TempData["msg"] = "Letter deleted successfully.";

            if (data.LetterType == "Received")
            {
                return RedirectToAction("LetterReceiveList");
            }
            else
            {
                return RedirectToAction("LetterSendList");
            }
        }
    }
}
