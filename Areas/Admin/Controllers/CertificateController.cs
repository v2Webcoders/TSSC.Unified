using ClosedXML.Excel;
using iText.Barcodes;
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
        [HttpGet]
        public IActionResult DownloadSampleAssExcel()
        {
            using (XLWorkbook workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Assessment");

                // =================================================
                // HEADERS
                // =================================================

                worksheet.Cell(1, 1).Value = "BatchId";
                worksheet.Cell(1, 2).Value = "EnrollmentNo";
                worksheet.Cell(1, 3).Value = "Candidate Name";
                worksheet.Cell(1, 4).Value = "Gender";
                worksheet.Cell(1, 5).Value = "Father Name";
                worksheet.Cell(1, 6).Value = "PhotoFileName";
                worksheet.Cell(1, 7).Value = "Jobrole";
                worksheet.Cell(1, 8).Value = "Date Of Issue";


                // =================================================
                // SAMPLE ROW 1
                // =================================================

                worksheet.Cell(2, 1).Value = "BATCH001";
                worksheet.Cell(2, 2).Value = "ENR001";
                worksheet.Cell(2, 3).Value = "Ms. Nishtha Nath";
                worksheet.Cell(2, 4).Value = "Female";
                worksheet.Cell(2, 5).Value = "Manoj Kumar Nath";
                worksheet.Cell(2, 6).Value = "nishtha_nath.jpg";
                worksheet.Cell(2, 7).Value = "Customer Care Executive";
                worksheet.Cell(2, 8).Value = new DateTime(2026, 9, 15);


                // =================================================
                // SAMPLE ROW 2
                // =================================================

                worksheet.Cell(3, 1).Value = "BATCH001";
                worksheet.Cell(3, 2).Value = "ENR002";
                worksheet.Cell(3, 3).Value = "Mr. Rahul Sharma";
                worksheet.Cell(3, 4).Value = "Male";
                worksheet.Cell(3, 5).Value = "Rajesh Sharma";
                worksheet.Cell(3, 6).Value = "rahul_sharma.jpg";
                worksheet.Cell(3, 7).Value = "Field Technician";
                worksheet.Cell(3, 8).Value = new DateTime(2026, 9, 15);


                // =================================================
                // SAMPLE ROW 3
                // =================================================

                worksheet.Cell(4, 1).Value = "BATCH001";
                worksheet.Cell(4, 2).Value = "ENR003";
                worksheet.Cell(4, 3).Value = "Ms. Priya Kumari";
                worksheet.Cell(4, 4).Value = "Female";
                worksheet.Cell(4, 5).Value = "Suresh Kumar";
                worksheet.Cell(4, 6).Value = "priya_kumari.jpg";
                worksheet.Cell(4, 7).Value = "Telecom Sales Executive";
                worksheet.Cell(4, 8).Value = new DateTime(2026, 9, 15);


                // =================================================
                // DATE FORMAT
                // =================================================

                worksheet.Column(8)
                    .Style
                    .DateFormat
                    .Format = "dd-MMMM-yyyy";


                // =================================================
                // HEADER STYLE
                // =================================================

                var headerRange = worksheet.Range("A1:H1");

                headerRange.Style.Font.Bold = true;

                headerRange.Style.Alignment.Horizontal =
                    XLAlignmentHorizontalValues.Center;

                headerRange.Style.Alignment.Vertical =
                    XLAlignmentVerticalValues.Center;


                // =================================================
                // COLUMN WIDTH
                // =================================================

                worksheet.Column(1).Width = 15; // BatchId
                worksheet.Column(2).Width = 20; // EnrollmentNo
                worksheet.Column(3).Width = 30; // Candidate Name
                worksheet.Column(4).Width = 15; // Gender
                worksheet.Column(5).Width = 30; // Father Name
                worksheet.Column(6).Width = 30; // PhotoFileName
                worksheet.Column(7).Width = 40; // Jobrole
                worksheet.Column(8).Width = 20; // Date Of Issue


                // =================================================
                // WRAP TEXT
                // =================================================

                worksheet.Range("A1:H4")
                    .Style
                    .Alignment
                    .WrapText = true;


                // =================================================
                // FREEZE HEADER
                // =================================================

                worksheet
                    .SheetView
                    .FreezeRows(1);


                // =================================================
                // RETURN EXCEL
                // =================================================

                using (MemoryStream stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);

                    byte[] fileBytes = stream.ToArray();

                    return File(
                        fileBytes,
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        "Assessment_Certificate_Sample.xlsx"
                    );
                }
            }
        }

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

        // =========================================================
        // BULK GENERATE - GET
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> BulkGenerateAssessment(int id, int stateId)
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
        public async Task<IActionResult> BulkGenerateAssessment(
        int id,
        IFormFile excelFile,
        int stateId)
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
                    nameof(BulkGenerateAssessment),
                    new { id = id }
                );
            }


            string extension =
                Path.GetExtension(excelFile.FileName).ToLowerInvariant();

            if (extension != ".xlsx")
            {
                TempData["Error"] =
                    "Please upload a valid .xlsx Excel file.";

                return RedirectToAction(
                    nameof(BulkGenerateAssessment),
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


            // Save first so that Generation Id is available
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


                            // =================================================
                            // VALIDATE WORKSHEET
                            // =================================================

                            if (worksheet == null)
                            {
                                TempData["Error"] =
                                    "Excel worksheet not found.";

                                return RedirectToAction(
                                    nameof(BulkGenerateAssessment),
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
                                    nameof(BulkGenerateAssessment),
                                    new { id = id }
                                );
                            }


                            // =================================================
                            // HEADERS
                            // =================================================
                            //
                            // Normalization allows:
                            //
                            // Candidate Name
                            // CandidateName
                            // Candidate_Name
                            //
                            // all to resolve as "candidatename".
                            // =================================================

                            var headers = worksheet
                                .Row(1)
                                .CellsUsed()
                                .ToDictionary(
                                    x => x.GetString()
                                        .Trim()
                                        .ToLowerInvariant()
                                        .Replace(" ", "")
                                        .Replace("_", ""),
                                    x => x.Address.ColumnNumber
                                );


                            // =================================================
                            // REQUIRED COLUMNS
                            // =================================================

                            string[] requiredColumns =
                            {
                                "batchid",
                                "enrollmentno",
                                "candidatename",
                                "gender",
                                "fathername",
                                "photofilename",
                                "jobrole",
                                "dateofissue"
                            };


                            foreach (string column in requiredColumns)
                            {
                                if (!headers.ContainsKey(column))
                                {
                                    TempData["Error"] =
                                        "Missing Excel column: " + column;

                                    return RedirectToAction(
                                        nameof(BulkGenerateAssessment),
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

                                string batchId =
                                row.Cell(
                                    headers["batchid"]
                                )
                                .GetString()
                                .Trim();

                                string enrollmentNo =
                                row.Cell(
                                    headers["enrollmentno"]
                                )
                                .GetString()
                                .Trim();

                                string candidateName =
                                    row.Cell(
                                        headers["candidatename"]
                                    )
                                    .GetString()
                                    .Trim();


                                string gender =
                                    row.Cell(
                                        headers["gender"]
                                    )
                                    .GetString()
                                    .Trim();


                                string fatherName =
                                    row.Cell(
                                        headers["fathername"]
                                    )
                                    .GetString()
                                    .Trim();


                                string photoFileName =
                                    row.Cell(
                                        headers["photofilename"]
                                    )
                                    .GetString()
                                    .Trim();


                                string jobRole =
                                    row.Cell(
                                        headers["jobrole"]
                                    )
                                    .GetString()
                                    .Trim();


                                string dateOfIssue =
                                    GetExcelDate(
                                        row.Cell(
                                            headers["dateofissue"]
                                        )
                                    );


                                // =============================================
                                // SKIP EMPTY ROW
                                // =============================================

                                if (string.IsNullOrWhiteSpace(candidateName) &&
                                    string.IsNullOrWhiteSpace(jobRole))
                                {
                                    continue;
                                }


                                totalRecords++;


                                // =============================================
                                // SAFE FILE NAME
                                // =============================================

                                string safeCandidateName =
                                    MakeSafeFileName(candidateName);


                                if (string.IsNullOrWhiteSpace(
                                    safeCandidateName))
                                {
                                    safeCandidateName =
                                        "Certificate_" + rowNumber;
                                }


                                // =============================================
                                // UNIQUE FILE NAME
                                // =============================================

                                string generatedFileName =
                                    safeCandidateName +
                                    "_" +
                                    DateTime.Now.ToString(
                                        "yyyyMMddHHmmssfff"
                                    ) +
                                    ".pdf";


                                try
                                {
                                    // =========================================
                                    // VALIDATE REQUIRED ROW DATA
                                    // =========================================

                                    if (string.IsNullOrWhiteSpace(batchId))
                                    {
                                        throw new Exception(
                                            "BatchId is required."
                                        );
                                    }

                                    if (string.IsNullOrWhiteSpace(enrollmentNo))
                                    {
                                        throw new Exception(
                                            "EnrollmentNo is required."
                                        );
                                    }

                                    if (string.IsNullOrWhiteSpace(
                                        candidateName))
                                    {
                                        throw new Exception(
                                            "Candidate Name is required."
                                        );
                                    }


                                    if (string.IsNullOrWhiteSpace(
                                        jobRole))
                                    {
                                        throw new Exception(
                                            "Jobrole is required."
                                        );
                                    }


                                    if (string.IsNullOrWhiteSpace(
                                        dateOfIssue))
                                    {
                                        throw new Exception(
                                            "Date Of Issue is required."
                                        );
                                    }


                                    // =========================================
                                    // PARSE DATE OF ISSUE
                                    // =========================================

                                    DateTime parsedDateOfIssue;

                                    if (!DateTime.TryParse(
                                        dateOfIssue,
                                        out parsedDateOfIssue))
                                    {
                                        throw new Exception(
                                            "Invalid Date Of Issue: " +
                                            dateOfIssue
                                        );
                                    }


                                    string displayDateOfIssue =
                                        parsedDateOfIssue.ToString(
                                            "dd-MM-yyyy"
                                        );


                                    // =========================================
                                    // CANDIDATE PHOTO PATH
                                    // =========================================

                                    string photoPath = null;


                                    if (!string.IsNullOrWhiteSpace(
                                        photoFileName))
                                    {
                                        photoPath = Path.Combine(
                                       _environment.WebRootPath,
                                       "uploads",
                                       "images",
                                       photoFileName
                                    );


                                        if (!System.IO.File.Exists(
                                            photoPath))
                                        {
                                            throw new Exception(
                                                "Candidate photo not found: " +
                                                photoFileName
                                            );
                                        }
                                    }

                                    string certificateUniqueNo = Guid.NewGuid().ToString("N");
                                    // =========================================
                                    // GENERATE PDF
                                    // =========================================

                                    byte[] pdfBytes =
                                        GenerateCertificatePdf(
                                            candidateName,
                                            gender,
                                            fatherName,
                                            photoPath,
                                            jobRole,
                                            displayDateOfIssue,
                                            certificateUniqueNo,
                                            templatePath
                                        );


                                    if (pdfBytes == null ||
                                        pdfBytes.Length == 0)
                                    {
                                        throw new Exception(
                                            "Certificate PDF could not be generated."
                                        );
                                    }


                                    // =========================================
                                    // SAVE PDF PERMANENTLY
                                    // =========================================

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


                                    // =========================================
                                    // RELATIVE FILE PATH
                                    // =========================================

                                    string generatedRelativePath =
                                        "/uploads/certificates/generated/" +
                                        generatedFileName;


                                    // =========================================
                                    // ADD PDF TO ZIP
                                    // =========================================

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


                                    // =========================================
                                    // SAVE GENERATION DETAIL
                                    // =========================================

                                    var detail =
                                        new CertificateGenerationAssessmentDetail
                                        {
                                            CertificateGenerationId =
                                                generation.Id,
                                            BatchId =
                                                batchId,
                                            EnrollmentNo =
                                                enrollmentNo,
                                            CandidateName =
                                                candidateName,

                                            Gender =
                                                gender,

                                            FatherName =
                                                fatherName,

                                            PhotoFileName =
                                                photoFileName,

                                            JobRole =
                                                jobRole,

                                            DateOfIssue =
                                                parsedDateOfIssue,

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
                                                true,
                                            CertificateUniqueNo = certificateUniqueNo
                                        };


                                    _context
                                        .CertificateGenerationAssessmentDetail
                                        .Add(detail);


                                    successRecords++;
                                }
                                catch (Exception ex)
                                {
                                    // =========================================
                                    // SAVE FAILED RECORD HISTORY
                                    // =========================================

                                    DateTime? failedDateOfIssue = null;


                                    if (DateTime.TryParse(
                                        dateOfIssue,
                                        out DateTime tempDate))
                                    {
                                        failedDateOfIssue =
                                            tempDate;
                                    }


                                    var failedDetail =
                                        new CertificateGenerationAssessmentDetail
                                        {
                                            CertificateGenerationId =
                                                generation.Id,

                                            BatchId =
                                                batchId,

                                            EnrollmentNo =
                                                enrollmentNo,

                                            CandidateName =
                                                candidateName,

                                            Gender =
                                                gender,

                                            FatherName =
                                                fatherName,

                                            PhotoFileName =
                                                photoFileName,

                                            JobRole =
                                                jobRole,

                                            DateOfIssue =
                                                failedDateOfIssue,

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
                                        .CertificateGenerationAssessmentDetail
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


                // =====================================================
                // NO CERTIFICATES GENERATED
                // =====================================================

                if (zipBytes.Length == 0 ||
                    successRecords == 0)
                {
                    TempData["Error"] =
                        "No assessment certificates were generated.";

                    return RedirectToAction(
                        nameof(BulkGenerateAssessment),
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

        private byte[] GenerateCertificatePdf(
        string candidateName,
        string gender,
        string fatherName,
        string photoPath,
        string jobRole,
        string dateOfIssue,
        string certificateUniqueNo,
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
                // CANDIDATE NAME
                // =================================================
                // =================================================
                // CERTIFICATE HEADING
                // =================================================

                Paragraph certificateHeading =
                    new Paragraph("CERTIFICATE")
                        .SetFont(boldFont)
                        .SetFontSize(28)
                        .SetFontColor(ColorConstants.BLACK)
                        .SetTextAlignment(
                            iText.Layout.Properties.TextAlignment.CENTER
                        )
                        .SetFixedPosition(
                            0,
                            350,
                            pageWidth
                        );

                document.Add(certificateHeading);


                // =================================================
                // THIS IS TO CERTIFY THAT
                // =================================================

                PdfFont normalFont =
                    PdfFontFactory.CreateFont(
                        StandardFonts.HELVETICA
                    );

                Paragraph certifyText =
                    new Paragraph("This is to certify that")
                        .SetFont(normalFont)
                        .SetFontSize(16)
                        .SetFontColor(ColorConstants.BLACK)
                        .SetTextAlignment(
                            iText.Layout.Properties.TextAlignment.CENTER
                        )
                        .SetFixedPosition(
                            0,
                            315,
                            pageWidth
                        );

                document.Add(certifyText);


                // =================================================
                // CANDIDATE NAME + FATHER NAME
                // =================================================

                string candidateDisplayName = candidateName;

                if (!string.IsNullOrWhiteSpace(fatherName))
                {
                    string relation =
                        gender.Equals("Male", StringComparison.OrdinalIgnoreCase)
                            ? "S/O"
                            : "D/O";

                    candidateDisplayName +=
                        " " + relation + " " + fatherName;
                }

                Paragraph candidate =
                    new Paragraph(candidateDisplayName)
                        .SetFont(boldFont)
                        .SetFontSize(16)
                        .SetFontColor(ColorConstants.BLACK)
                        .SetTextAlignment(
                            iText.Layout.Properties.TextAlignment.CENTER
                        )
                        .SetFixedPosition(
                            0,
                            285,
                            pageWidth
                        );

                document.Add(candidate);


                // =================================================
                // ASSESSMENT TEXT
                // =================================================

                Paragraph assessmentText =
                    new Paragraph(
                        "has Successfully cleared the assessment for the course of"
                    )
                    .SetFont(normalFont)
                    .SetFontSize(16)
                    .SetFontColor(ColorConstants.BLACK)
                    .SetTextAlignment(
                        iText.Layout.Properties.TextAlignment.CENTER
                    )
                    .SetFixedPosition(
                        0,
                        255,
                        pageWidth
                    );

                document.Add(assessmentText);


                // =================================================
                // JOB ROLE / COURSE
                // =================================================

                Paragraph jobRoleParagraph =
                    new Paragraph(jobRole)
                        .SetFont(boldFont)
                        .SetFontSize(16)
                        .SetFontColor(ColorConstants.BLACK)
                        .SetTextAlignment(
                            iText.Layout.Properties.TextAlignment.CENTER
                        )
                        .SetFixedPosition(
                            0,
                            225,
                            pageWidth
                        );

                document.Add(jobRoleParagraph);






                // =================================================
                // DATE OF ISSUE
                // =================================================

                Paragraph issueDate =
                new Paragraph()
                    .SetTextAlignment(
                        iText.Layout.Properties.TextAlignment.RIGHT
                    )
                    .SetFixedPosition(
                        pageWidth - 265,
                        105,
                        220
                    );

                issueDate.Add(
                    new Text("Date of Issuance: ")
                        .SetFont(regularFont)
                        .SetFontSize(14)
                        .SetFontColor(ColorConstants.BLACK)
                );

                issueDate.Add(
                    new Text(dateOfIssue)
                        .SetFont(regularFont)
                        .SetFontSize(14)
                        .SetFontColor(ColorConstants.BLACK)
                );

                document.Add(issueDate);

                // =================================================
                // QR CODE - BELOW DATE OF ISSUANCE
                // =================================================

                //string qrText =
                //    "Candidate Name: " + candidateName +
                //    "\nFather Name: " + fatherName +
                //    "\nCourse: " + jobRole +
                //    "\nDate of Issue: " + dateOfIssue;

                //BarcodeQRCode qrCode =
                //    new BarcodeQRCode(qrText);

                //Image qrImage =
                //    new Image(qrCode.CreateFormXObject(pdf));
                string qrText =
                $"https://erp.tsscindia.com/certificate-verify?certificateId={certificateUniqueNo}";

                BarcodeQRCode qrCode = new BarcodeQRCode(qrText);

                Image qrImage = new Image(qrCode.CreateFormXObject(pdf));

                //qrImage
                //    .ScaleToFit(70, 70)
                //    .SetFixedPosition(pageWidth - 105, 30, 70);

                //document.Add(qrImage);
                qrImage
                    .ScaleToFit(60, 60)
                    .SetFixedPosition(
                        pageWidth - 170,
                        30,
                        60
                    );

                document.Add(qrImage);



                // =================================================
                // CANDIDATE PHOTO
                // =================================================

                if (!string.IsNullOrWhiteSpace(photoPath) &&
                    System.IO.File.Exists(photoPath))
                {
                    ImageData photoData =
                        ImageDataFactory.Create(
                            photoPath
                        );


                    Image candidatePhoto =
                        new Image(photoData);


                    // ---------------------------------------------
                    // PHOTO POSITION
                    // ---------------------------------------------
                    //
                    // Adjust these values according to your
                    // certificate template.
                    // ---------------------------------------------

                    candidatePhoto
                        .ScaleToFit(
                            90,
                            110
                        )
                        .SetFixedPosition(
                            pageWidth - 145,
                            pageHeight - 145
                        );


                    document.Add(
                        candidatePhoto
                    );
                }


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

        [HttpGet]
        public async Task<IActionResult> AssessmentCertificateHistory()
        {
            var history = await _context.CertificateGenerationAssessmentDetail
                .Where(x => x.IsActive)
                .OrderByDescending(x => x.GeneratedDate)
                .ToListAsync();

            return View(history);
        }
        //[HttpGet]
        //public async Task<IActionResult> AssessmentCertificateHistory()
        //{
        //    var history = await _context.CertificateGenerationAssessmentDetail
        //        .Include(x => x.CertificateGeneration)
        //            .ThenInclude(x => x.State)
        //        .Where(x => x.IsActive)
        //        .OrderByDescending(x => x.GeneratedDate)
        //        .ToListAsync();

        //    return View(history);
        //}
    }


}