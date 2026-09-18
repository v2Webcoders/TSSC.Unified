using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TSSC.Unified.ViewModel;

namespace TSSC.Unified.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    public class ImageController : Controller
    {
        private readonly IWebHostEnvironment _environment;

        public ImageController(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        [HttpGet]
        public IActionResult Index()
        {
            var uploadFolder = Path.Combine(
                _environment.WebRootPath,
                "uploads",
                "images"
            );

            var images = new List<string>();

            //if (Directory.Exists(uploadFolder))
            //{
            //    var files = Directory.GetFiles(uploadFolder);

            //    foreach (var file in files)
            //    {
            //        var fileName = Path.GetFileName(file);

            //        images.Add($"/uploads/images/{fileName}");
            //    }
            //}

            if (Directory.Exists(uploadFolder))
            {
                var files = Directory.GetFiles(uploadFolder)
                .OrderByDescending(f => System.IO.File.GetLastWriteTime(f));

                foreach (var file in files)
                {
                    var fileName = Path.GetFileName(file);

                    images.Add($"/uploads/images/{fileName}");
                }
            }
            return View(images);
        }

        [HttpGet]
        public IActionResult Upload()
        {
            return View();
        }

        [HttpPost]
        [RequestSizeLimit(104857600)] // 100 MB
        public async Task<IActionResult> Upload(BulkImageUploadVM model)
        {
            if (model.Images == null || model.Images.Count == 0)
            {
                ModelState.AddModelError("", "Please select at least one image.");
                return View(model);
            }

            // Folder: wwwroot/uploads/images
            var uploadFolder = Path.Combine(
                _environment.WebRootPath,
                "uploads",
                "images"
            );

            if (!Directory.Exists(uploadFolder))
            {
                Directory.CreateDirectory(uploadFolder);
            }

            foreach (var image in model.Images)
            {
                if (image.Length == 0)
                    continue;

                // Validate extension
                var extension = Path.GetExtension(image.FileName).ToLowerInvariant();

                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };

                if (!allowedExtensions.Contains(extension))
                    continue;

                //// Generate unique filename
                //var fileName = $"{Guid.NewGuid()}{extension}";

                // Original filename without extension
                var originalFileName =
                    Path.GetFileNameWithoutExtension(image.FileName);

                // Remove invalid filename characters
                foreach (var character in Path.GetInvalidFileNameChars())
                {
                    originalFileName = originalFileName.Replace(character, '_');
                }
                // Replace spaces with underscore
                //originalFileName = originalFileName.Replace(" ", "_");
                // Random 5 digit number
                var randomNumber = Random.Shared.Next(10000, 99999);

                // OriginalName + random number + extension
                var fileName =
                    $"{originalFileName}{extension}";

                var filePath = Path.Combine(uploadFolder, fileName);

                using var stream = new FileStream(
                    filePath,
                    FileMode.Create
                );

                await image.CopyToAsync(stream);
            }

            TempData["Success"] = "Images uploaded successfully.";

            return RedirectToAction(nameof(Upload));
        }
    }
}
