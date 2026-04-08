using InternshipManagementSystem.Data;
using InternshipManagementSystem.Models;
using InternshipManagementSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using InternshipManagementSystem.Services;

namespace InternshipManagementSystem.Controllers
{
    [Authorize(Roles = "Student")]
    public class StudentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _hostingEnvironment;
        private readonly IStudentService _studentService;
        private readonly IDailyDiaryService _diaryService;
        private readonly ITaskService _taskService;
        private readonly IDocumentService _documentService;

        public StudentController(
            ApplicationDbContext context, 
            IWebHostEnvironment hostingEnvironment,
            IStudentService studentService,
            IDailyDiaryService diaryService,
            ITaskService taskService,
            IDocumentService documentService)
        {
            _context = context;
            _hostingEnvironment = hostingEnvironment;
            _studentService = studentService;
            _diaryService = diaryService;
            _taskService = taskService;
            _documentService = documentService;
        }

        private async Task<int> GetStudentIdAsync()
        {
            var userIdStr = User.FindFirst("UserId")?.Value;
            if (string.IsNullOrEmpty(userIdStr)) return 0;
            
            var userId = int.Parse(userIdStr);
            var student = await _context.Students.FirstOrDefaultAsync(s => s.UserId == userId);
            return student?.StudentId ?? 0;
        }

        public async Task<IActionResult> Index()
        {
            var studentId = await GetStudentIdAsync();
            ViewBag.TotalDiaries = await _context.DailyDiaries.CountAsync(d => d.StudentId == studentId);
            ViewBag.TasksAssigned = await _context.Tasks.CountAsync(t => t.StudentId == studentId);
            ViewBag.PendingTasks = await _context.Tasks.CountAsync(t => t.StudentId == studentId && t.Status != "Completed");
            ViewBag.HasInternship = await _context.InternshipDetails.AnyAsync(i => i.StudentId == studentId);
            return View();
        }

        // ── Daily Diary ──────────────────────────────────────────────
        public async Task<IActionResult> Diary()
        {
            var studentId = await GetStudentIdAsync();
            var diaries = await _context.DailyDiaries
                .Where(d => d.StudentId == studentId)
                .OrderByDescending(d => d.WorkDate)
                .ToListAsync();
            return View(diaries);
        }

        [HttpGet]
        public IActionResult CreateDiary()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateDiary(DailyDiary model)
        {
            var studentId = await GetStudentIdAsync();
            if (studentId == 0) return RedirectToAction("Login", "Account");
            
            model.StudentId = studentId;
            model.Status = "Pending";
            ModelState.Remove("Student");
            ModelState.Remove("GuideComment");
            
            if (ModelState.IsValid)
            {
                try 
                {
                    _context.DailyDiaries.Add(model);
                    await _context.SaveChangesAsync();

                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                        return Json(new { success = true, message = "Daily diary submitted successfully.", redirectUrl = Url.Action("Diary") });

                    TempData["Success"] = "Daily diary submitted successfully.";
                    return RedirectToAction("Diary");
                }
                catch (Exception ex)
                {
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                        return Json(new { success = false, message = "Database error: " + ex.Message });
                    ModelState.AddModelError("", "Database error: " + ex.Message);
                }
            }
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                return Json(new { success = false, message = "Please check your inputs and try again." });
            return View(model);
        }

        // ── Tasks ─────────────────────────────────────────────────────
        public async Task<IActionResult> Tasks()
        {
            var studentId = await GetStudentIdAsync();
            var tasks = await _context.Tasks
                .Include(t => t.Guide)
                .ThenInclude(g => g.User)
                .Where(t => t.StudentId == studentId)
                .OrderBy(t => t.Deadline)
                .ToListAsync();
            return View(tasks);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateTaskStatus(int taskId, string status)
        {
            var studentId = await GetStudentIdAsync();
            if (studentId == 0) return RedirectToAction("Login", "Account");

            var task = await _context.Tasks.FirstOrDefaultAsync(t => t.TaskId == taskId && t.StudentId == studentId);
            if (task != null)
            {
                try 
                {
                    task.Status = status;
                    await _context.SaveChangesAsync();
                    
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                        return Json(new { success = true, message = "Task status updated.", reload = true });

                    TempData["Success"] = "Task status updated.";
                }
                catch (Exception ex)
                {
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                        return Json(new { success = false, message = "Error updating task: " + ex.Message });
                    TempData["Error"] = "Error updating task: " + ex.Message;
                }
            }
            return RedirectToAction("Tasks");
        }

        // ── Internship Details ────────────────────────────────────────
        public async Task<IActionResult> Internship()
        {
            var studentId = await GetStudentIdAsync();
            var internship = await _context.InternshipDetails.FirstOrDefaultAsync(i => i.StudentId == studentId);
            return View(internship);
        }

        [HttpGet]
        public async Task<IActionResult> AddInternship()
        {
            var studentId = await GetStudentIdAsync();
            var existing = await _context.InternshipDetails.FirstOrDefaultAsync(i => i.StudentId == studentId);
            if (existing != null) return RedirectToAction("Internship");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddInternship(InternshipDetailsViewModel model)
        {
            var studentId = await GetStudentIdAsync();
            if (studentId == 0) return RedirectToAction("Login", "Account");

            if (ModelState.IsValid)
            {
                try 
                {
                    var detail = new InternshipDetails
                    {
                        StudentId = studentId,
                        CompanyName = model.CompanyName,
                        Role = model.Role,
                        TechnologyUsed = model.TechnologyUsed,
                        StartDate = model.StartDate,
                        EndDate = model.EndDate,
                        Description = model.Description
                    };
                    _context.InternshipDetails.Add(detail);
                    await _context.SaveChangesAsync();

                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                        return Json(new { success = true, message = "Internship details added.", redirectUrl = Url.Action("Internship") });

                    TempData["Success"] = "Internship details added.";
                    return RedirectToAction("Internship");
                }
                catch (Exception ex)
                {
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                        return Json(new { success = false, message = "Database error: " + ex.Message });
                    ModelState.AddModelError("", "Database error: " + ex.Message);
                }
            }
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                return Json(new { success = false, message = "Please check your inputs and try again." });
            return View(model);
        }

        // ── Weekly Reports ────────────────────────────────────────────
        public async Task<IActionResult> WeeklyReports()
        {
            var studentId = await GetStudentIdAsync();
            var reports = await _context.WeeklyReports
                .Where(r => r.StudentId == studentId)
                .OrderByDescending(r => r.WeekNumber)
                .ToListAsync();
            return View(reports);
        }

        [HttpGet]
        public IActionResult SubmitReport()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitReport(WeeklyReport model)
        {
            var studentId = await GetStudentIdAsync();
            if (studentId == 0) return RedirectToAction("Login", "Account");
            
            model.StudentId = studentId;
            model.Status = "Pending";
            ModelState.Remove("Student");
            ModelState.Remove("GuideComment");
            
            if (ModelState.IsValid)
            {
                try 
                {
                    _context.WeeklyReports.Add(model);
                    await _context.SaveChangesAsync();

                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                        return Json(new { success = true, message = "Weekly report submitted.", redirectUrl = Url.Action("WeeklyReports") });

                    TempData["Success"] = "Weekly report submitted.";
                    return RedirectToAction("WeeklyReports");
                }
                catch (Exception ex)
                {
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                        return Json(new { success = false, message = "Database error: " + ex.Message });
                    ModelState.AddModelError("", "Database error: " + ex.Message);
                }
            }
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                return Json(new { success = false, message = "Please check your inputs and try again." });
            return View(model);
        }

        // ── Documents ────────────────────────────────────────────────
        public async Task<IActionResult> Documents()
        {
            var studentId = await GetStudentIdAsync();
            var docs = await _context.Documents
                .Where(d => d.StudentId == studentId)
                .OrderByDescending(d => d.UploadDate)
                .ToListAsync();
            return View(docs);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(52428800)] // 50MB limit
        public async Task<IActionResult> UploadDocument(IFormFile file)
        {
            var studentId = await GetStudentIdAsync();
            if (studentId == 0 || file == null || file.Length == 0) 
                return RedirectToAction("Documents");

            // Validate file extension
            var allowedExtensions = new[] { ".pdf", ".doc", ".docx", ".ppt", ".pptx", ".zip" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(extension))
            {
                TempData["Error"] = "File type not allowed. Supported: PDF, DOC, DOCX, PPT, PPTX, ZIP.";
                return RedirectToAction("Documents");
            }

            // Validate file size (e.g., 50MB)
            if (file.Length > 50 * 1024 * 1024)
            {
                TempData["Error"] = "File is too large. Maximum size allowed is 50MB.";
                return RedirectToAction("Documents");
            }

            try 
            {
                // Create uploads folder if not exists
                var uploadsFolder = Path.Combine(_hostingEnvironment.WebRootPath, "uploads");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                // Generate unique filename
                var uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(file.FileName);
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(fileStream);
                }

                var document = new Document
                {
                    StudentId = studentId,
                    FileName = file.FileName,
                    FilePath = uniqueFileName,
                    UploadDate = DateTime.Now
                };

                _context.Documents.Add(document);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Document uploaded successfully.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Server error during upload: " + ex.Message;
            }
            return RedirectToAction("Documents");
        }

        public async Task<IActionResult> DownloadDocument(int id)
        {
            var studentId = await GetStudentIdAsync();
            var doc = await _context.Documents.FirstOrDefaultAsync(d => d.DocumentId == id && d.StudentId == studentId);
            if (doc == null) return Unauthorized();

            var filePath = Path.Combine(_hostingEnvironment.WebRootPath, "uploads", doc.FilePath);
            if (!System.IO.File.Exists(filePath)) return NotFound();

            var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
            return File(fileBytes, "application/octet-stream", doc.FileName);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteDocument(int id)
        {
            var studentId = await GetStudentIdAsync();
            if (studentId == 0) return Unauthorized();

            var doc = await _context.Documents.FirstOrDefaultAsync(d => d.DocumentId == id && d.StudentId == studentId);
            if (doc != null)
            {
                try 
                {
                    var filePath = Path.Combine(_hostingEnvironment.WebRootPath, "uploads", doc.FilePath);
                    if (System.IO.File.Exists(filePath)) System.IO.File.Delete(filePath);
                    
                    _context.Documents.Remove(doc);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Document deleted.";
                }
                catch (Exception ex)
                {
                    TempData["Error"] = "Delete error: " + ex.Message;
                }
            }
            return RedirectToAction("Documents");
        }
    }
}
