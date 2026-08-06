using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using QUIZAPP.Models;
using TSSC.Unified.Models;
using TSSC.Unified.ViewModel;
using QUIZAPP;

namespace TSSC.Unified.Areas.HRMS.Controllers
{
    [Area("HRMS")]
    [Authorize(Roles = "HR,Employee")]
    public class TaskController : Controller
    {
        private readonly AppdbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly IWebHostEnvironment _environment;
        public TaskController(AppdbContext context, UserManager<AppUser> userManager, IWebHostEnvironment environment)
        {
            _context = context;
            _userManager = userManager;
            _environment = environment;
        }
        [HttpGet]
        //public async Task<IActionResult> Create()
        //{
        //    ViewBag.EmployeeList = new SelectList(
        //        await _context.Employee
        //            .Where(x => x.IsActive)
        //            .Select(x => new
        //            {
        //                x.EmployeeId,
        //                FullName = x.FirstName + " " + x.LastName
        //            })
        //            .ToListAsync(),
        //        "EmployeeId",
        //        "FullName");

        //    return View(new TaskVM());
        //}
        public async Task<IActionResult> Create()
        {
            var currentUser = await _userManager.GetUserAsync(User);

            var employee = await _context.Employee
                .FirstOrDefaultAsync(x => x.ApplicationUserId == currentUser.Id);

            if (employee != null)
            {
                ViewBag.EmployeeList = new SelectList(
                    await _context.Employee
                        .Where(x => x.IsActive &&
                                    x.ReportingManagerId == employee.EmployeeId)
                        .Select(x => new
                        {
                            x.EmployeeId,
                            FullName = x.FirstName + " " + x.LastName
                        })
                        .ToListAsync(),
                    "EmployeeId",
                    "FullName");
            }
            else
            {
                ViewBag.EmployeeList = new SelectList(new List<object>(), "EmployeeId", "FullName");
            }

            return View(new TaskVM());
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TaskVM model)
        {
            ViewBag.EmployeeList = new SelectList(
                _context.Employee.Where(x => x.IsActive),
                "EmployeeId",
                "FirstName");

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            string? fileName = null;

            if (model.AttachmentFile != null)
            {
                string uploadFolder = Path.Combine(_environment.WebRootPath, "Uploads", "Tasks");

                if (!Directory.Exists(uploadFolder))
                    Directory.CreateDirectory(uploadFolder);

                fileName = Guid.NewGuid().ToString() + Path.GetExtension(model.AttachmentFile.FileName);

                string filePath = Path.Combine(uploadFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await model.AttachmentFile.CopyToAsync(stream);
                }
            }
            var user = await _userManager.GetUserAsync(User);

            var emp = await _context.Employee
                .FirstOrDefaultAsync(x => x.ApplicationUserId == user.Id);

            Tasks task = new Tasks
            {
                TaskName = model.TaskName,
                TaskDescription = model.TaskDescription,
                AssignedTo = model.AssignedTo,
                AssignedBy = emp.EmployeeId,
                Attachment = fileName,
                Deadline = model.Deadline,
                Remarks = model.Remarks,
                Status = "Pending",
                CreatedOn = DateTime.Now,
                UpdatedOn = DateTime.Now,
                IsActive = true
            };

            _context.Tasks.Add(task);

            await _context.SaveChangesAsync();

            TempData["msg"] = "Task Assigned Successfully.";

            return RedirectToAction(nameof(TaskList));
        }
        [HttpGet]
        public async Task<IActionResult> EditTask(int id)
        {
            var task = await _context.Tasks
                .FirstOrDefaultAsync(x => x.TaskId == id);

            if (task == null)
                return NotFound();

            ViewBag.EmployeeList = new SelectList(_context.Employee
                .Where(x => x.IsActive),
                "EmployeeId",
                "FirstName",
                task.AssignedTo);

            TaskVM model = new TaskVM
            {
                TaskId = task.TaskId,
                TaskName = task.TaskName,
                TaskDescription = task.TaskDescription,
                AssignedTo = task.AssignedTo,
                Deadline = task.Deadline,
                Remarks = task.Remarks,
                Status = task.Status,
                Attachment = task.Attachment
            };

            return View("Create", model);
        }
        [HttpPost]
        public async Task<IActionResult> EditTask(TaskVM model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.EmployeeList = new SelectList(_context.Employee
                    .Where(x => x.IsActive),
                    "EmployeeId",
                    "FirstName",
                    model.AssignedTo);

                return View("Create", model);
            }

            var task = await _context.Tasks
                .FirstOrDefaultAsync(x => x.TaskId == model.TaskId);

            if (task == null)
                return NotFound();

            task.TaskName = model.TaskName;
            task.TaskDescription = model.TaskDescription;
            task.AssignedTo = model.AssignedTo;
            task.Deadline = model.Deadline;
            task.Remarks = model.Remarks;
            task.Status = model.Status;
            task.UpdatedOn = DateTime.Now;

            if (model.AttachmentFile != null)
            {
                string folder = Path.Combine(_environment.WebRootPath, "Uploads", "Tasks");

                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);

                string fileName = Guid.NewGuid() +
                                  Path.GetExtension(model.AttachmentFile.FileName);

                using (var stream = new FileStream(Path.Combine(folder, fileName), FileMode.Create))
                {
                    await model.AttachmentFile.CopyToAsync(stream);
                }

                task.Attachment = fileName;
            }

            await _context.SaveChangesAsync();

            TempData["msg"] = "Task Updated Successfully";

            return RedirectToAction("TaskList");
        }

        [HttpGet]
        public async Task<IActionResult> DeleteTask(int id)
        {
            var task = await _context.Tasks
                .FirstOrDefaultAsync(x => x.TaskId == id);

            if (task == null)
                return NotFound();

            _context.Tasks.Remove(task);

            await _context.SaveChangesAsync();

            TempData["msg"] = "Task Deleted Successfully";

            return RedirectToAction("TaskList");
        }
        public async Task<IActionResult> TaskList()
        {
            var taskList = await (
                from t in _context.Tasks
                join at in _context.Employee on t.AssignedTo equals at.EmployeeId
                join ab in _context.Employee on t.AssignedBy equals ab.EmployeeId

                select new TaskVM
                {
                    TaskId = t.TaskId,
                    TaskName = t.TaskName,
                    TaskDescription = t.TaskDescription,
                    AssignedTo = t.AssignedTo,
                    AssignedBy = t.AssignedBy,

                    AssignedToName = at.FirstName + " " + at.LastName,
                    AssignedByName = ab.FirstName + " " + ab.LastName,

                    Attachment = t.Attachment,
                    Deadline = t.Deadline,
                    Remarks = t.Remarks,
                    Status = t.Status,
                    CreatedOn = t.CreatedOn,
                    UpdatedOn = t.UpdatedOn,
                    IsActive = t.IsActive
                }).ToListAsync();

            return View(taskList);
        }

        public async Task<IActionResult> MyTasks()
        {
            var user = await _userManager.GetUserAsync(User);

            var employee = await _context.Employee
                .FirstOrDefaultAsync(x => x.ApplicationUserId == user.Id);

            if (employee == null)
                return RedirectToAction("Index", "Home");

            var model = await (from t in _context.Tasks
                               join e in _context.Employee
                                   on t.AssignedBy equals e.EmployeeId
                               where t.AssignedTo == employee.EmployeeId
                               select new TaskVM
                               {
                                   TaskId = t.TaskId,
                                   TaskName = t.TaskName,
                                   TaskDescription = t.TaskDescription,
                                   Deadline = t.Deadline,
                                   Status = t.Status,
                                   Attachment = t.Attachment,
                                   Remarks = t.Remarks,

                                   AssignedBy = t.AssignedBy,
                                   AssignedTo = t.AssignedTo,

                                   AssignedByName = e.FirstName + " " + e.LastName
                               }).ToListAsync();

            return View(model);
        }
        
        public async Task<IActionResult> Details(int id)
        {
            var task = await _context.Tasks
                .FirstOrDefaultAsync(x => x.TaskId == id);

            if (task == null)
                return NotFound();

            TaskVM model = new TaskVM
            {
                TaskId = task.TaskId,
                TaskName = task.TaskName,
                TaskDescription = task.TaskDescription,
                Deadline = task.Deadline,
                Status = task.Status,
                Remarks = task.Remarks,
                Attachment = task.Attachment,
                EmployeeRemarks=task.EmployeeRemarks,
                AttachmentByEmployee=task.AttachmentByEmployee,
                CompletedOn= task.CompletedOn

            };

            return View(model);
        }
        [HttpPost]
        public async Task<IActionResult> Details(TaskVM model)
        {
            string? fileName = null;

            if (model.AttachmentByEmployeeFile != null)
            {
                string uploadFolder = Path.Combine(_environment.WebRootPath, "Uploads", "Tasks");

                if (!Directory.Exists(uploadFolder))
                    Directory.CreateDirectory(uploadFolder);

                fileName = Guid.NewGuid().ToString() + Path.GetExtension(model.AttachmentByEmployeeFile.FileName);

                string filePath = Path.Combine(uploadFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await model.AttachmentByEmployeeFile.CopyToAsync(stream);
                }
            }
            if (!ModelState.IsValid)
                return View(model);

            var task = await _context.Tasks
                .FirstOrDefaultAsync(x => x.TaskId == model.TaskId);

            if (task == null)
                return NotFound();

            task.Status = model.Status;

            task.EmployeeRemarks = model.EmployeeRemarks;
            task.AttachmentByEmployee = fileName;
            task.CompletedOn = DateTime.Now;
            task.UpdatedOn = DateTime.Now;

            if (model.Status == "Completed")
            {
                task.CompletedOn = DateTime.Now;
            }

            await _context.SaveChangesAsync();

            TempData["msg"] = "Task Updated Successfully";

            return RedirectToAction("MyTasks");
        }



        [HttpGet]
        public async Task<IActionResult> CompletedTask()
        {
            var model = await (from t in _context.Tasks
                               join e1 in _context.Employee on t.AssignedTo equals e1.EmployeeId
                               join e2 in _context.Employee on t.AssignedBy equals e2.EmployeeId
                               where t.Status == "Completed" && !t.IsClosed
                               select new TaskVM
                               {
                                   TaskId = t.TaskId,
                                   TaskName = t.TaskName,
                                   TaskDescription = t.TaskDescription,
                                   AssignedToName = e1.FirstName + " " + e1.LastName,
                                   AssignedByName = e2.FirstName + " " + e2.LastName,
                                   Deadline = t.Deadline,
                                   Status = t.Status,
                                   Attachment = t.Attachment,
                                   AttachmentByEmployee = t.AttachmentByEmployee,
                                   EmployeeRemarks = t.EmployeeRemarks,
                                   CreatedOn = t.CreatedOn,
                                   UpdatedOn = t.UpdatedOn,
                                   CompletedOn = t.CompletedOn
                               }).ToListAsync();

            return View(model);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CloseTask(int id)
        {
            var task = await _context.Tasks
                .FirstOrDefaultAsync(x => x.TaskId == id);

            if (task == null)
                return NotFound();

            task.IsClosed = true;
            task.UpdatedOn = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["msg"] = "Task Closed Successfully.";

            return RedirectToAction(nameof(CompletedTask));
        }
        [HttpGet]
        public async Task<IActionResult> CompletedTaskDetails(int id)
        {
            var model = await (from t in _context.Tasks
                               join e in _context.Employee
                               on t.AssignedTo equals e.EmployeeId
                               where t.TaskId == id
                               select new TaskVM
                               {
                                   TaskId = t.TaskId,
                                   TaskName = t.TaskName,
                                   AssignedTo = t.AssignedTo,
                                   AssignedToName = e.FirstName + " " + e.LastName,
                                   TaskDescription = t.TaskDescription,
                                   Deadline = t.Deadline,
                                   Status = t.Status,
                                   Remarks = t.Remarks,
                                   Attachment = t.Attachment,
                                   EmployeeRemarks = t.EmployeeRemarks,
                                   AttachmentByEmployee = t.AttachmentByEmployee,
                                   CompletedOn = t.CompletedOn
                               }).FirstOrDefaultAsync();

            if (model == null)
                return NotFound();

            return View(model);
        }
        [HttpGet]
        public async Task<IActionResult> ClosedTask()
        {
            var model = await (from t in _context.Tasks
                               join e1 in _context.Employee on t.AssignedTo equals e1.EmployeeId
                               join e2 in _context.Employee on t.AssignedBy equals e2.EmployeeId
                               where t.Status == "Completed" && t.IsClosed
                               select new TaskVM
                               {
                                   TaskId = t.TaskId,
                                   TaskName = t.TaskName,
                                   TaskDescription = t.TaskDescription,
                                   AssignedToName = e1.FirstName + " " + e1.LastName,
                                   AssignedByName = e2.FirstName + " " + e2.LastName,
                                   Deadline = t.Deadline,
                                   Status = t.Status,
                                   Attachment = t.Attachment,
                                   IsClosed = t.IsClosed,
                                   EmployeeRemarks=t.EmployeeRemarks,
                                   AttachmentByEmployee=t.AttachmentByEmployee,
                                   CompletedOn=t.CompletedOn
                               }).ToListAsync();

            ViewBag.IsClosedPage = true;

            return View("CompletedTask", model);
        }
    }
}
