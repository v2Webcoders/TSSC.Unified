using ClosedXML.Excel;
using iText.IO.Font.Constants;
using iText.IO.Image;
using iText.Kernel.Colors;
using iText.Kernel.Font;
using iText.Kernel.Pdf;
using iText.Layout.Element;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Org.BouncyCastle.Ocsp;
using Org.BouncyCastle.Tls;
using QUIZAPP;
using QUIZAPP.Models;
using System.IO.Compression;
using TSSC.Unified.Models;

namespace TSSC.Unified.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    public class CertificateController : Controller
    {
        private readonly ILogger<CertificateController> _logger;
        private readonly AppdbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IWebHostEnvironment _environment;

        public CertificateController(
            ILogger<CertificateController> logger,
            AppdbContext context,
            UserManager<AppUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IWebHostEnvironment environment)
        {
            _logger = logger;
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
            _environment = environment;
        }


        // =========================================================
        // INDEX
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var certificates = await _context.Certificate
                .OrderByDescending(x => x.Id)
                .ToListAsync();
            var states = await _context.State
       .OrderBy(x => x.StateName)
       .ToListAsync();

            ViewBag.States = states;


            return View(certificates);
        }


        // =========================================================
        // BULK GENERATE - GET
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> BulkGenerate(int id, int stateId)
        {
            var certificate = await _context.Certificate
                .FirstOrDefaultAsync(x =>
                    x.Id == id &&
                    x.IsActive);

            if (certificate == null)
            {
                return NotFound("Certificate not found.");
            }

            ViewBag.CompanyName = certificate.CompanyName;
            ViewBag.CertificateId = certificate.Id;
            ViewBag.StateId = stateId;

            return View();
        }


        // =========================================================
        // BULK GENERATE - POST
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkGenerate(int id, IFormFile excelFile, int stateId)
        {
            // =====================================================
            // GET CERTIFICATE MASTER
            // =====================================================

            var certificate = await _context.Certificate
                .FirstOrDefaultAsync(x => x.Id == id && x.IsActive);

            if (certificate == null)
            {
                return NotFound(
                    "Certificate does not exist. ID received: " + id
                );
            }


            // =====================================================
            // VALIDATE EXCEL FILE
            // =====================================================

            if (excelFile == null || excelFile.Length == 0)
            {
                TempData["Error"] = "Please upload an Excel file.";

                return RedirectToAction(
                    nameof(BulkGenerate),
                    new { id = id }
                );
            }


            string extension =
                Path.GetExtension(excelFile.FileName).ToLower();

            if (extension != ".xlsx")
            {
                TempData["Error"] =
                    "Please upload a valid .xlsx Excel file.";

                return RedirectToAction(
                    nameof(BulkGenerate),
                    new { id = id }
                );
            }


            // =====================================================
            // TEMPLATE PATH
            // =====================================================

            if (string.IsNullOrWhiteSpace(certificate.TemplateFile))
            {
                return NotFound(
                    "Certificate template is not assigned."
                );
            }


            string templatePath = Path.Combine(
                _environment.WebRootPath,
                "uploads",
                "certificates",
                "templates",
                certificate.TemplateFile
            );


            if (!System.IO.File.Exists(templatePath))
            {
                return NotFound(
                    "Certificate template not found: " +
                    certificate.TemplateFile
                );
            }


            // =====================================================
            // CREATE GENERATION / BATCH HISTORY
            // =====================================================

            var generation = new CertificateGeneration
            {
                CertificateId = certificate.Id,
                StateId = stateId,
                GeneratedDate = DateTime.Now,
                GeneratedBy = User.Identity?.Name,
                ExcelFileName = excelFile.FileName,

                TotalRecords = 0,
                SuccessRecords = 0,
                FailedRecords = 0,

                IsActive = true
            };

            _context.CertificateGeneration.Add(generation);

            // Save first to get Generation Id
            await _context.SaveChangesAsync();


            // =====================================================
            // COUNTERS
            // =====================================================

            int totalRecords = 0;
            int successRecords = 0;
            int failedRecords = 0;


            // =====================================================
            // CREATE ZIP
            // =====================================================

            using (MemoryStream zipStream = new MemoryStream())
            {
                using (ZipArchive zip = new ZipArchive(
                    zipStream,
                    ZipArchiveMode.Create,
                    true))
                {
                    // =================================================
                    // READ EXCEL
                    // =================================================

                    using (Stream excelStream =
                           excelFile.OpenReadStream())
                    {
                        using (XLWorkbook workbook =
                               new XLWorkbook(excelStream))
                        {
                            var worksheet =
                                workbook.Worksheets.FirstOrDefault();


                            if (worksheet == null)
                            {
                                TempData["Error"] =
                                    "Excel worksheet not found.";

                                return RedirectToAction(
                                    nameof(BulkGenerate),
                                    new { id = id }
                                );
                            }


                            // =================================================
                            // LAST ROW
                            // =================================================

                            var lastUsedRow =
                                worksheet.LastRowUsed();

                            int lastRow =
                                lastUsedRow?.RowNumber() ?? 0;


                            if (lastRow < 2)
                            {
                                TempData["Error"] =
                                    "Excel does not contain any data.";

                                return RedirectToAction(
                                    nameof(BulkGenerate),
                                    new { id = id }
                                );
                            }


                            // =================================================
                            // HEADERS
                            // =================================================

                            var headers =
                                worksheet
                                    .Row(1)
                                    .CellsUsed()
                                    .ToDictionary(
                                        x => x.GetString()
                                            .Trim()
                                            .ToLower(),
                                        x => x.Address.ColumnNumber
                                    );


                            // =================================================
                            // REQUIRED COLUMNS
                            // =================================================

                            string[] requiredColumns =
                            {
                        "certificateno",
                        "companyname",
                        "description",
                        "startdate",
                        "enddate"
                    };


                            foreach (string column in requiredColumns)
                            {
                                if (!headers.ContainsKey(column))
                                {
                                    TempData["Error"] =
                                        "Missing Excel column: " +
                                        column;

                                    return RedirectToAction(
                                        nameof(BulkGenerate),
                                        new { id = id }
                                    );
                                }
                            }


                            // =================================================
                            // PROCESS EVERY EXCEL ROW
                            // =================================================

                            for (
                                int rowNumber = 2;
                                rowNumber <= lastRow;
                                rowNumber++)
                            {
                                var row =
                                    worksheet.Row(rowNumber);


                                // =============================================
                                // READ EXCEL VALUES
                                // =============================================

                                string certificateNo =
                                    row.Cell(
                                        headers["certificateno"]
                                    )
                                    .GetString()
                                    .Trim();


                                string companyName =
                                    row.Cell(
                                        headers["companyname"]
                                    )
                                    .GetString()
                                    .Trim();


                                string description =
                                    row.Cell(
                                        headers["description"]
                                    )
                                    .GetString()
                                    .Trim();


                                string startDate =
                                    GetExcelDate(
                                        row.Cell(
                                            headers["startdate"]
                                        )
                                    );


                                string endDate =
                                    GetExcelDate(
                                        row.Cell(
                                            headers["enddate"]
                                        )
                                    );


                                // =============================================
                                // SKIP EMPTY ROW
                                // =============================================

                                if (
                                    string.IsNullOrWhiteSpace(
                                        certificateNo
                                    )
                                    &&
                                    string.IsNullOrWhiteSpace(
                                        companyName
                                    )
                                )
                                {
                                    continue;
                                }


                                totalRecords++;


                                // =============================================
                                // SAFE FILE NAME
                                // =============================================

                                string safeCertificateNo =
                                    MakeSafeFileName(
                                        certificateNo
                                    );


                                if (
                                    string.IsNullOrWhiteSpace(
                                        safeCertificateNo
                                    ))
                                {
                                    safeCertificateNo =
                                        "Certificate_" +
                                        rowNumber;
                                }


                                // =================================================
                                // UNIQUE FILE NAME
                                // =================================================

                                string generatedFileName =
                                    safeCertificateNo +
                                    "_" +
                                    DateTime.Now.ToString(
                                        "yyyyMMddHHmmssfff"
                                    ) +
                                    ".pdf";


                                try
                                {
                                    // =============================================
                                    // GENERATE PDF
                                    // =============================================

                                    byte[] pdfBytes =
                                        GenerateCertificatePdf(
                                            certificateNo,
                                            companyName,
                                            description,
                                            startDate,
                                            endDate,
                                            templatePath
                                        );


                                    // =============================================
                                    // SAVE PDF PERMANENTLY
                                    // =============================================

                                    string generatedFolder =
                                        Path.Combine(
                                            _environment.WebRootPath,
                                            "uploads",
                                            "certificates",
                                            "generated"
                                        );


                                    if (!Directory.Exists(
                                        generatedFolder))
                                    {
                                        Directory.CreateDirectory(
                                            generatedFolder
                                        );
                                    }


                                    string physicalFilePath =
                                        Path.Combine(
                                            generatedFolder,
                                            generatedFileName
                                        );


                                    await System.IO.File.WriteAllBytesAsync(
                                        physicalFilePath,
                                        pdfBytes
                                    );


                                    // =============================================
                                    // RELATIVE FILE PATH
                                    // =============================================

                                    string generatedRelativePath =
                                        "/uploads/certificates/generated/" +
                                        generatedFileName;


                                    // =============================================
                                    // ADD PDF TO ZIP
                                    // =============================================

                                    ZipArchiveEntry entry =
                                        zip.CreateEntry(
                                            generatedFileName,
                                            CompressionLevel.Fastest
                                        );


                                    using (
                                        Stream entryStream =
                                        entry.Open())
                                    {
                                        await entryStream.WriteAsync(
                                            pdfBytes,
                                            0,
                                            pdfBytes.Length
                                        );
                                    }


                                    // =============================================
                                    // PARSE DATES
                                    // =============================================

                                    DateTime? parsedStartDate = null;
                                    DateTime? parsedEndDate = null;


                                    if (DateTime.TryParse(
                                        startDate,
                                        out DateTime tempStartDate))
                                    {
                                        parsedStartDate =
                                            tempStartDate;
                                    }


                                    if (DateTime.TryParse(
                                        endDate,
                                        out DateTime tempEndDate))
                                    {
                                        parsedEndDate =
                                            tempEndDate;
                                    }


                                    // =============================================
                                    // SAVE GENERATION DETAIL
                                    // =============================================

                                    var detail =
                                        new CertificateGenerationDetail
                                        {
                                            CertificateGenerationId =
                                                generation.Id,

                                            CertificateNo =
                                                certificateNo,

                                            CompanyName =
                                                companyName,

                                            Description =
                                                description,

                                            StartDate =
                                                parsedStartDate,

                                            EndDate =
                                                parsedEndDate,

                                            GeneratedFileName =
                                                generatedFileName,

                                            GeneratedFilePath =
                                                generatedRelativePath,

                                            GeneratedDate =
                                                DateTime.Now,

                                            IsGenerated =
                                                true,

                                            ErrorMessage =
                                                null,

                                            IsActive =
                                                true
                                        };


                                    _context
                                        .CertificateGenerationDetail
                                        .Add(detail);


                                    successRecords++;
                                }
                                catch (Exception ex)
                                {
                                    // =============================================
                                    // SAVE FAILED RECORD HISTORY
                                    // =============================================

                                    DateTime? failedStartDate = null;
                                    DateTime? failedEndDate = null;


                                    if (DateTime.TryParse(
                                        startDate,
                                        out DateTime tempFailedStart))
                                    {
                                        failedStartDate =
                                            tempFailedStart;
                                    }


                                    if (DateTime.TryParse(
                                        endDate,
                                        out DateTime tempFailedEnd))
                                    {
                                        failedEndDate =
                                            tempFailedEnd;
                                    }


                                    var failedDetail =
                                        new CertificateGenerationDetail
                                        {
                                            CertificateGenerationId =
                                                generation.Id,

                                            CertificateNo =
                                                certificateNo,

                                            CompanyName =
                                                companyName,

                                            Description =
                                                description,

                                            StartDate =
                                                failedStartDate,

                                            EndDate =
                                                failedEndDate,

                                            GeneratedFileName =
                                                generatedFileName,

                                            GeneratedFilePath =
                                                null,

                                            GeneratedDate =
                                                DateTime.Now,

                                            IsGenerated =
                                                false,

                                            ErrorMessage =
                                                ex.Message,

                                            IsActive =
                                                true
                                        };


                                    _context
                                        .CertificateGenerationDetail
                                        .Add(failedDetail);


                                    failedRecords++;
                                }
                            }
                        }
                    }
                }


                // =====================================================
                // UPDATE GENERATION / BATCH SUMMARY
                // =====================================================

                generation.TotalRecords =
                    totalRecords;

                generation.SuccessRecords =
                    successRecords;

                generation.FailedRecords =
                    failedRecords;


                // =====================================================
                // SAVE ALL HISTORY
                // =====================================================

                await _context.SaveChangesAsync();


                // =====================================================
                // ZIP BYTES
                // =====================================================

                byte[] zipBytes =
                    zipStream.ToArray();


                if (zipBytes.Length == 0)
                {
                    TempData["Error"] =
                        "No certificates were generated.";

                    return RedirectToAction(
                        nameof(BulkGenerate),
                        new { id = id }
                    );
                }


                // =====================================================
                // DOWNLOAD ZIP
                // =====================================================

                return File(
                    zipBytes,
                    "application/zip",
                    "Certificates.zip"
                );
            }
        }
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> BulkGenerate(int id,IFormFile excelFile)
        //{
        //    // =====================================================
        //    // VALIDATE FILE
        //    // =====================================================
        //    var certificate = await _context.Certificate
        //.FirstOrDefaultAsync(x => x.Id == id);

        //    if (certificate == null)
        //    {
        //        return NotFound(
        //            "Certificate does not exist. ID received: " + id
        //        );
        //    }
        //    if (excelFile == null || excelFile.Length == 0)
        //    {
        //        TempData["Error"] = "Please upload an Excel file.";

        //        return RedirectToAction(nameof(BulkGenerate));
        //    }


        //    string extension =
        //        Path.GetExtension(excelFile.FileName).ToLower();


        //    if (extension != ".xlsx")
        //    {
        //        TempData["Error"] =
        //            "Please upload a valid .xlsx Excel file.";

        //        return RedirectToAction(nameof(BulkGenerate));
        //    }


        //    // =====================================================
        //    // TEMPLATE PATH
        //    // =====================================================

        //    //string templatePath = Path.Combine(
        //    //    _environment.WebRootPath,
        //    //    "uploads",
        //    //    "certificates",
        //    //    "templates",
        //    //    "ONE_POINT_ONE_SOLUTION_LTD.png"
        //    //);
        //    if (string.IsNullOrWhiteSpace(certificate.TemplateFile))
        //    {
        //        return NotFound("Certificate template is not assigned.");
        //    }

        //    string templatePath = Path.Combine(
        //        _environment.WebRootPath,
        //        "uploads",
        //        "certificates",
        //        "templates",
        //        certificate.TemplateFile
        //    );

        //    if (!System.IO.File.Exists(templatePath))
        //    {
        //        return NotFound(
        //            "Certificate template not found: " +
        //            certificate.TemplateFile
        //        );
        //    }


        //    if (!System.IO.File.Exists(templatePath))
        //    {
        //        TempData["Error"] =
        //            "Certificate template not found: " +
        //            templatePath;

        //        return RedirectToAction(nameof(BulkGenerate));
        //    }


        //    // =====================================================
        //    // CREATE ZIP
        //    // =====================================================

        //    using (MemoryStream zipStream =
        //           new MemoryStream())
        //    {
        //        using (ZipArchive zip =
        //               new ZipArchive(
        //                   zipStream,
        //                   ZipArchiveMode.Create,
        //                   true))
        //        {
        //            // =================================================
        //            // READ EXCEL
        //            // =================================================

        //            using (Stream excelStream =
        //                   excelFile.OpenReadStream())
        //            {
        //                using (XLWorkbook workbook =
        //                       new XLWorkbook(excelStream))
        //                {
        //                    var worksheet =
        //                        workbook.Worksheets.FirstOrDefault();


        //                    if (worksheet == null)
        //                    {
        //                        TempData["Error"] =
        //                            "Excel worksheet not found.";

        //                        return RedirectToAction(
        //                            nameof(BulkGenerate)
        //                        );
        //                    }


        //                    // =============================================
        //                    // LAST ROW
        //                    // =============================================

        //                    var lastUsedRow =
        //                        worksheet.LastRowUsed();


        //                    int lastRow =
        //                        lastUsedRow?.RowNumber() ?? 0;


        //                    if (lastRow < 2)
        //                    {
        //                        TempData["Error"] =
        //                            "Excel does not contain any data.";

        //                        return RedirectToAction(
        //                            nameof(BulkGenerate)
        //                        );
        //                    }


        //                    // =============================================
        //                    // HEADERS
        //                    // =============================================

        //                    var headers =
        //                        worksheet
        //                            .Row(1)
        //                            .CellsUsed()
        //                            .ToDictionary(
        //                                x => x.GetString()
        //                                    .Trim()
        //                                    .ToLower(),
        //                                x => x.Address.ColumnNumber
        //                            );


        //                    // =============================================
        //                    // REQUIRED COLUMNS
        //                    // =============================================

        //                    string[] requiredColumns =
        //                    {
        //                        "certificateno",
        //                        "companyname",
        //                        "description",
        //                        "startdate",
        //                        "enddate"
        //                    };


        //                    foreach (
        //                        string column
        //                        in requiredColumns)
        //                    {
        //                        if (!headers.ContainsKey(column))
        //                        {
        //                            TempData["Error"] =
        //                                "Missing Excel column: " +
        //                                column;

        //                            return RedirectToAction(
        //                                nameof(BulkGenerate)
        //                            );
        //                        }
        //                    }


        //                    // =============================================
        //                    // PROCESS EVERY EXCEL ROW
        //                    // =============================================

        //                    for (
        //                        int rowNumber = 2;
        //                        rowNumber <= lastRow;
        //                        rowNumber++)
        //                    {
        //                        var row =
        //                            worksheet.Row(rowNumber);


        //                        // =========================================
        //                        // READ EXCEL VALUES
        //                        // =========================================

        //                        string certificateNo =
        //                            row.Cell(
        //                                headers["certificateno"]
        //                            )
        //                            .GetString()
        //                            .Trim();


        //                        string companyName =
        //                            row.Cell(
        //                                headers["companyname"]
        //                            )
        //                            .GetString()
        //                            .Trim();


        //                        string description =
        //                            row.Cell(
        //                                headers["description"]
        //                            )
        //                            .GetString()
        //                            .Trim();


        //                        string startDate =
        //                            GetExcelDate(
        //                                row.Cell(
        //                                    headers["startdate"]
        //                                )
        //                            );


        //                        string endDate =
        //                            GetExcelDate(
        //                                row.Cell(
        //                                    headers["enddate"]
        //                                )
        //                            );


        //                        // =========================================
        //                        // SKIP EMPTY ROW
        //                        // =========================================

        //                        if (
        //                            string.IsNullOrWhiteSpace(
        //                                certificateNo
        //                            )
        //                            &&
        //                            string.IsNullOrWhiteSpace(
        //                                companyName
        //                            ))
        //                        {
        //                            continue;
        //                        }


        //                        // =========================================
        //                        // GENERATE PDF
        //                        // =========================================

        //                        byte[] pdfBytes =
        //                            GenerateCertificatePdf(
        //                                certificateNo,
        //                                companyName,
        //                                description,
        //                                startDate,
        //                                endDate,
        //                                templatePath
        //                            );


        //                        // =========================================
        //                        // SAFE FILE NAME
        //                        // =========================================

        //                        string safeCertificateNo =
        //                            MakeSafeFileName(
        //                                certificateNo
        //                            );


        //                        if (
        //                            string.IsNullOrWhiteSpace(
        //                                safeCertificateNo
        //                            ))
        //                        {
        //                            safeCertificateNo =
        //                                "Certificate_" +
        //                                rowNumber;
        //                        }


        //                        // =========================================
        //                        // ADD PDF TO ZIP
        //                        // =========================================

        //                        ZipArchiveEntry entry =
        //                            zip.CreateEntry(
        //                                safeCertificateNo +
        //                                ".pdf",
        //                                CompressionLevel.Fastest
        //                            );


        //                        using (
        //                            Stream entryStream =
        //                            entry.Open())
        //                        {
        //                            await entryStream.WriteAsync(
        //                                pdfBytes,
        //                                0,
        //                                pdfBytes.Length
        //                            );
        //                        }
        //                    }
        //                }
        //            }
        //        }


        //        // =====================================================
        //        // ZIP BYTES
        //        // =====================================================

        //        byte[] zipBytes =
        //            zipStream.ToArray();


        //        if (zipBytes.Length == 0)
        //        {
        //            TempData["Error"] =
        //                "No certificates were generated.";

        //            return RedirectToAction(
        //                nameof(BulkGenerate)
        //            );
        //        }


        //        // =====================================================
        //        // DOWNLOAD ZIP
        //        // =====================================================

        //        return File(
        //            zipBytes,
        //            "application/zip",
        //            "Certificates.zip"
        //        );
        //    }
        //}


        // =========================================================
        // SINGLE CERTIFICATE - GET
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> GenerateCertificate(int id)
        {
            // =====================================================
            // GET FROM DATABASE
            // =====================================================

            var certificate =
                await _context.Certificate
                    .FirstOrDefaultAsync(x =>
                        x.Id == id &&
                        x.IsActive);


            if (certificate == null)
            {
                return NotFound(
                    "Certificate not found for Id: " + id
                );
            }


            // =====================================================
            // DYNAMIC VALUES
            // =====================================================

            string certificateNo =
                certificate.CertificateNo ?? "";


            string companyName =
                certificate.CompanyName ?? "";


            string description =
                certificate.Description ?? "";


            string startDate =
                certificate.StartDate.HasValue
                    ? certificate.StartDate.Value
                        .ToString("dd-MMMM-yyyy")
                    : "";


            string endDate =
                certificate.EndDate.HasValue
                    ? certificate.EndDate.Value
                        .ToString("dd-MMMM-yyyy")
                    : "";


            // =====================================================
            // TEMPLATE
            // =====================================================

            //string templatePath =
            //    Path.Combine(
            //        _environment.WebRootPath,
            //        "uploads",
            //        "certificates",
            //        "templates",
            //        "ONE_POINT_ONE_SOLUTION_LTD.png"
            //    );


            //if (!System.IO.File.Exists(templatePath))
            //{
            //    return NotFound(
            //        "Certificate template not found: " +
            //        templatePath
            //    );
            //}
          
    // ============================================

    if (string.IsNullOrWhiteSpace(certificate.TemplateFile))
            {
                return NotFound(
                    "Certificate template is not assigned."
                );
            }

            string templatePath = Path.Combine(
                _environment.WebRootPath,
                "uploads",
                "certificates",
                "templates",
                certificate.TemplateFile
            );

            if (!System.IO.File.Exists(templatePath))
            {
                return NotFound(
                    "Certificate template not found: " +
                    certificate.TemplateFile
                );
            }


            // =====================================================
            // GENERATE PDF
            // =====================================================

            byte[] pdfBytes =
                GenerateCertificatePdf(
                    certificateNo,
                    companyName,
                    description,
                    startDate,
                    endDate,
                    templatePath
                );


            return File(
                pdfBytes,
                "application/pdf",
                $"{certificateNo}.pdf"
            );
        }


        // =========================================================
        // ITEXT PDF GENERATION
        // =========================================================

        private byte[] GenerateCertificatePdf(
            string certificateNo,
            string companyName,
            string description,
            string startDate,
            string endDate,
            string templatePath)
        {
            // =====================================================
            // CREATE PDF MEMORY
            // =====================================================

            using (MemoryStream outputStream =
                   new MemoryStream())
            {
                // =================================================
                // A4 LANDSCAPE
                // =================================================

                iText.Kernel.Geom.PageSize pageSize =
                    iText.Kernel.Geom.PageSize
                        .A4
                        .Rotate();


                // =================================================
                // PDF WRITER
                // =================================================

                PdfWriter writer =
                    new PdfWriter(
                        outputStream
                    );


                // =================================================
                // PDF DOCUMENT
                // =================================================

                PdfDocument pdf =
                    new PdfDocument(
                        writer
                    );


                pdf.SetDefaultPageSize(
                    pageSize
                );


                // =================================================
                // LAYOUT DOCUMENT
                // =================================================

                iText.Layout.Document document =
                    new iText.Layout.Document(
                        pdf
                    );


                document.SetMargins(
                    0,
                    0,
                    0,
                    0
                );


                float pageWidth =
                    pageSize.GetWidth();


                float pageHeight =
                    pageSize.GetHeight();


                // =================================================
                // FONTS
                // =================================================
                PdfFont regularFont =
                    PdfFontFactory.CreateFont(
                        StandardFonts.HELVETICA
                    );

                PdfFont boldFont =
                    PdfFontFactory.CreateFont(
                        StandardFonts.HELVETICA_BOLD
                    );

                PdfFont timesFont =
                    PdfFontFactory.CreateFont(
                        StandardFonts.TIMES_ROMAN
                    );
                // =================================================
                // BACKGROUND IMAGE
                // =================================================

                ImageData imageData =
                    ImageDataFactory.Create(
                        templatePath
                    );


                Image background =
                    new Image(
                        imageData
                    );


                background
                    .ScaleAbsolute(
                        pageWidth,
                        pageHeight
                    )
                    .SetFixedPosition(
                        0,
                        0
                    );


                document.Add(
                    background
                );


                // =================================================
                // COMPANY NAME
                // =================================================
                Paragraph company =
     new Paragraph(companyName)
         .SetFont(boldFont)
         .SetFontSize(18)
         .SetFontColor(ColorConstants.BLACK)
         .SetTextAlignment(
             iText.Layout.Properties.TextAlignment.CENTER
         )
         .SetFixedPosition(
             0,
             290,
             pageWidth
         );

                document.Add(company);

                // =================================================
                // DESCRIPTION
                // =================================================

               Paragraph descriptionText =
    new Paragraph(description)
        .SetFont(timesFont)
        .SetFontSize(18)
        .SetFontColor(ColorConstants.BLACK)
        .SetTextAlignment(
            iText.Layout.Properties.TextAlignment.CENTER
        )
        .SetFixedPosition(
            0,
            252,
            pageWidth
        );

document.Add(descriptionText);
                // =================================================
                // DATE
                // =================================================

                Paragraph dates =
    new Paragraph()
        .SetTextAlignment(
            iText.Layout.Properties.TextAlignment.CENTER
        )
        .SetFixedPosition(
            0,
            215,
            pageWidth
        );

                dates.Add(
                    new Text(startDate)
                        .SetFont(boldFont)
                        .SetFontSize(20)
                        .SetFontColor(ColorConstants.BLACK)
                );

                dates.Add(
                    new Text("  to  ")
                        .SetFont(regularFont)
                        .SetFontSize(20)
                        .SetFontColor(ColorConstants.BLACK)
                );

                dates.Add(
                    new Text(endDate)
                        .SetFont(boldFont)
                        .SetFontSize(20)
                        .SetFontColor(ColorConstants.BLACK)
                );

                document.Add(dates);
                // =================================================
                // ID NUMBER
                // CertificateNo = ID No.
                // =================================================

                Paragraph idNumber =
                    new Paragraph()
                        .SetTextAlignment(
                            iText.Layout.Properties.TextAlignment.LEFT
                        )
                        .SetFixedPosition(
                            88,
                            75,
                            220
                        );

                idNumber.Add(
                    new Text("ID No.: ")
                        .SetFont(regularFont)
                        .SetFontSize(10)
                        .SetFontColor(
                            ColorConstants.BLACK
                        )
                );

                idNumber.Add(
                    new Text(certificateNo)
                        .SetFont(boldFont)
                        .SetFontSize(10)
                        .SetFontColor(
                            ColorConstants.BLACK
                        )
                );

                document.Add(idNumber);
                // =================================================
                // CLOSE
                // =================================================

                document.Close();


                // =================================================
                // RETURN PDF
                // =================================================

                return outputStream.ToArray();
            }
        }


        // =========================================================
        // EXCEL DATE HELPER
        // =========================================================

        private string GetExcelDate(
            IXLCell cell)
        {
            if (cell.IsEmpty())
            {
                return "";
            }


            if (
                cell.TryGetValue<DateTime>(
                    out DateTime date
                ))
            {
                return date.ToString(
                    "dd-MMMM-yyyy"
                );
            }


            string value =
                cell.GetString()
                    .Trim();


            if (
                DateTime.TryParse(
                    value,
                    out DateTime parsedDate
                ))
            {
                return parsedDate.ToString(
                    "dd-MMMM-yyyy"
                );
            }


            return value;
        }


        // =========================================================
        // SAFE FILE NAME
        // =========================================================

        private string MakeSafeFileName(
            string fileName)
        {
            foreach (
                char invalidChar
                in Path.GetInvalidFileNameChars())
            {
                fileName =
                    fileName.Replace(
                        invalidChar.ToString(),
                        "_"
                    );
            }


            return fileName.Trim();
        }


        // =========================================================
        // DOWNLOAD SAMPLE EXCEL
        // =========================================================

        [HttpGet]
        public IActionResult DownloadSampleExcel()
        {
            using (XLWorkbook workbook =
                   new XLWorkbook())
            {
                var worksheet =
                    workbook.Worksheets.Add(
                        "Certificates"
                    );


                // =================================================
                // HEADERS
                // =================================================

                worksheet.Cell(
                    1,
                    1
                ).Value =
                    "CertificateNo";


                worksheet.Cell(
                    1,
                    2
                ).Value =
                    "CompanyName";


                worksheet.Cell(
                    1,
                    3
                ).Value =
                    "Description";


                worksheet.Cell(
                    1,
                    4
                ).Value =
                    "StartDate";


                worksheet.Cell(
                    1,
                    5
                ).Value =
                    "EndDate";


                // =================================================
                // SAMPLE ROW 1
                // =================================================

                worksheet.Cell(
                    2,
                    1
                ).Value =
                    "TSSC-IM-P/05";


                worksheet.Cell(
                    2,
                    2
                ).Value =
                    "One Point One Solutions Limited";


                worksheet.Cell(
                    2,
                    3
                ).Value =
                    "Is an Platinum Industry member of Telecom Sector Skill Council for a period";


                worksheet.Cell(
                    2,
                    4
                ).Value =
                    new DateTime(
                        2022,
                        12,
                        14
                    );


                worksheet.Cell(
                    2,
                    5
                ).Value =
                    new DateTime(
                        2027,
                        12,
                        13
                    );


                // =================================================
                // SAMPLE ROW 2
                // =================================================

                worksheet.Cell(
                    3,
                    1
                ).Value =
                    "TSSC-IM-P/06";


                worksheet.Cell(
                    3,
                    2
                ).Value =
                    "ABC Solutions Pvt Ltd";


                worksheet.Cell(
                    3,
                    3
                ).Value =
                    "Is an Platinum Industry member of Telecom Sector Skill Council for a period";


                worksheet.Cell(
                    3,
                    4
                ).Value =
                    new DateTime(
                        2023,
                        1,
                        1
                    );


                worksheet.Cell(
                    3,
                    5
                ).Value =
                    new DateTime(
                        2027,
                        12,
                        31
                    );


                // =================================================
                // DATE FORMAT
                // =================================================

                worksheet.Column(4)
                    .Style
                    .DateFormat
                    .Format =
                    "dd-MMMM-yyyy";


                worksheet.Column(5)
                    .Style
                    .DateFormat
                    .Format =
                    "dd-MMMM-yyyy";


                // =================================================
                // HEADER STYLE
                // =================================================

                var headerRange =
                    worksheet.Range(
                        "A1:E1"
                    );


                headerRange
                    .Style
                    .Font
                    .Bold = true;


                headerRange
                    .Style
                    .Alignment
                    .Horizontal =
                    XLAlignmentHorizontalValues.Center;


                // =================================================
                // COLUMN WIDTH
                // =================================================

                worksheet.Column(1).Width = 22;
                worksheet.Column(2).Width = 40;
                worksheet.Column(3).Width = 70;
                worksheet.Column(4).Width = 20;
                worksheet.Column(5).Width = 20;


                // =================================================
                // FREEZE HEADER
                // =================================================

                worksheet
                    .SheetView
                    .FreezeRows(1);


                // =================================================
                // RETURN EXCEL
                // =================================================

                using (
                    MemoryStream stream =
                    new MemoryStream())
                {
                    workbook.SaveAs(
                        stream
                    );


                    byte[] fileBytes =
                        stream.ToArray();


                    return File(
                        fileBytes,
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        "Certificate_Sample.xlsx"
                    );
                }
            }
        }
        //[HttpGet]
        //public async Task<IActionResult> CertificateHistory()
        //{
        //    var history = await _context.CertificateGenerationDetail
        //        .Include(x => x.CertificateGeneration)
        //        .ThenInclude(x=>x.StateId)
        //        .Where(x =>
        //            x.IsActive &&
        //            x.CertificateGeneration != null &&
        //            x.CertificateGeneration.IsActive)
        //        .OrderByDescending(x => x.CertificateGeneration.GeneratedDate)
        //        .ToListAsync();

        //    return View(history);
        //}
        [HttpGet]
        public async Task<IActionResult> CertificateHistory()
        {
            var history = await _context.CertificateGenerationDetail
                .Include(x => x.CertificateGeneration)
                .Where(x =>
                    x.IsActive &&
                    x.CertificateGeneration != null &&
                    x.CertificateGeneration.IsActive)
                .OrderByDescending(x => x.CertificateGeneration.GeneratedDate)
                .ToListAsync();

            ViewBag.States = await _context.State
                .ToDictionaryAsync(x => x.Id, x => x.StateName);

            return View(history);
        }
    }
}