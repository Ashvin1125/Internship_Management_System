using InternshipManagementSystem.Data;
using InternshipManagementSystem.Models;
using InternshipManagementSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using InternshipManagementSystem.Services;

namespace InternshipManagementSystem.Controllers
{
    [Authorize(Roles = "Guide")]
    public class GuideController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _hostingEnvironment;
        private readonly DashboardService _dashboardService;
        private readonly NotificationService _notificationService;

        public GuideController(
            ApplicationDbContext context, 
            IWebHostEnvironment hostingEnvironment,
            DashboardService dashboardService,
            NotificationService notificationService)
        {
            _context = context;
            _hostingEnvironment = hostingEnvironment;
            _dashboardService = dashboardService;
            _notificationService = notificationService;
        }

        private async Task<int> GetGuideIdAsync()
        {
            var userIdStr = User.FindFirst("UserId")?.Value;
            if (string.IsNullOrEmpty(userIdStr)) return 0;

            var userId = int.Parse(userIdStr);
            var guide = await _context.Guides.FirstOrDefaultAsync(g => g.UserId == userId);
            return guide?.GuideId ?? 0;
        }

        public async Task<IActionResult> Index()
        {
            var guideId = await GetGuideIdAsync();
            if (guideId == 0) return RedirectToAction("Login", "Account");

            var stats = await _dashboardService.GetGuideStatsAsync(guideId);
            return View(stats);
        }

        public async Task<IActionResult> Students()
        {
            var guideId = await GetGuideIdAsync();
            var assignments = await _context.GuideAssignments
                .Include(ga => ga.Student)
                .ThenInclude(s => s.User)
                .Where(ga => ga.GuideId == guideId)
                .ToListAsync();
            return View(assignments);
        }
        
        public async Task<IActionResult> Diaries()
        {
            var guideId = await GetGuideIdAsync();
            
            var assignedStudentIds = await _context.GuideAssignments
                .Where(ga => ga.GuideId == guideId)
                .Select(ga => ga.StudentId)
                .ToListAsync();

            var diaries = await _context.DailyDiaries
                .Include(d => d.Student)
                .ThenInclude(s => s.User)
                .Where(d => assignedStudentIds.Contains(d.StudentId))
                .OrderByDescending(d => d.CreatedAt)
                .ToListAsync();
                
            return View(diaries);
        }

        public async Task<IActionResult> WeeklyReports()
        {
            var guideId = await GetGuideIdAsync();
            
            var assignedStudentIds = await _context.GuideAssignments
                .Where(ga => ga.GuideId == guideId)
                .Select(ga => ga.StudentId)
                .ToListAsync();

            var reports = await _context.WeeklyReports
                .Include(r => r.Student)
                .ThenInclude(s => s.User)
                .Where(r => assignedStudentIds.Contains(r.StudentId))
                .OrderByDescending(r => r.WeekNumber)
                .ToListAsync();
                
            return View(reports);
        }

        [HttpGet]
        public async Task<IActionResult> AssignTask()
        {
            var guideId = await GetGuideIdAsync();
            var students = await _context.GuideAssignments
                .Include(ga => ga.Student).ThenInclude(s => s.User)
                .Where(ga => ga.GuideId == guideId)
                .Select(ga => ga.Student)
                .ToListAsync();
            ViewBag.Students = students;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignTask(AssignTaskViewModel model)
        {
            var guideId = await GetGuideIdAsync();
            if (guideId == 0) return RedirectToAction("Login", "Account");

            var student = await _context.Students.Include(s => s.User).FirstOrDefaultAsync(s => s.StudentId == model.StudentId);
            var isAssigned = await _context.GuideAssignments.AnyAsync(ga => ga.GuideId == guideId && ga.StudentId == model.StudentId);
            
            if (student == null || !isAssigned)
            {
                TempData["Error"] = "Unauthorized: Student is not assigned to you.";
                return RedirectToAction("Index");
            }

            ModelState.Remove("Description");
            if (ModelState.IsValid)
            {
                var task = new InternshipTask
                {
                    GuideId  = guideId,
                    StudentId = model.StudentId,
                    Title = model.Title,
                    Description = model.Description,
                    Deadline = model.Deadline,
                    Status = "Assigned"
                };
                try 
                {
                    _context.Tasks.Add(task);
                    await _context.SaveChangesAsync();
                    
                    // Trigger Notification
                    await _notificationService.CreateNotificationAsync(student.UserId, $"New Task Assigned: {model.Title}", "Task");
                    
                    // Invalidate Cache
                    _dashboardService.InvalidateGuideCache(guideId);
                    _dashboardService.InvalidateStudentCache(model.StudentId);

                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                        return Json(new { success = true, message = "Task assigned successfully.", redirectUrl = Url.Action("Tasks") });

                    TempData["Success"] = "Task assigned successfully.";
                    return RedirectToAction("Tasks");
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
            
            var students2 = await _context.GuideAssignments
                .Include(ga => ga.Student).ThenInclude(s => s.User)
                .Where(ga => ga.GuideId == guideId)
                .Select(ga => ga.Student)
                .ToListAsync();
            ViewBag.Students = students2;
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveDiary(int diaryId, string action, string comment)
        {
            var guideId = await GetGuideIdAsync();
            var diary = await _context.DailyDiaries.Include(d => d.Student).FirstOrDefaultAsync(d => d.DiaryId == diaryId);
            
            if (diary != null)
            {
                var isAssigned = await _context.GuideAssignments.AnyAsync(ga => ga.GuideId == guideId && ga.StudentId == diary.StudentId);
                if (!isAssigned) return Unauthorized();

                try 
                {
                    diary.Status = action == "Approve" ? "Approved" : "Rejected";
                    diary.GuideComment = comment;
                    await _context.SaveChangesAsync();
                    
                    // Trigger Notification
                    await _notificationService.CreateNotificationAsync(diary.Student.UserId, $"Daily Diary of {diary.WorkDate.ToShortDateString()} has been {diary.Status}.", "Diary");
                    
                    // Invalidate Cache
                    _dashboardService.InvalidateGuideCache(guideId);
                    _dashboardService.InvalidateStudentCache(diary.StudentId);

                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                        return Json(new { success = true, message = $"Diary {diary.Status.ToLower()} successfully.", reload = true });

                    TempData["Success"] = $"Diary {diary.Status.ToLower()} successfully.";
                }
                catch (Exception ex)
                {
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                        return Json(new { success = false, message = "Error updating diary: " + ex.Message });
                    TempData["Error"] = "Error updating diary: " + ex.Message;
                }
            }
            return RedirectToAction("Diaries");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveWeeklyReport(int reportId, string action, string comment)
        {
            var guideId = await GetGuideIdAsync();
            var report = await _context.WeeklyReports.Include(r => r.Student).FirstOrDefaultAsync(r => r.ReportId == reportId);
            
            if (report != null)
            {
                var isAssigned = await _context.GuideAssignments.AnyAsync(ga => ga.GuideId == guideId && ga.StudentId == report.StudentId);
                if (!isAssigned) return Unauthorized();

                try 
                {
                    report.Status = action == "Approve" ? "Approved" : "Rejected";
                    report.GuideComment = comment;
                    await _context.SaveChangesAsync();
                    
                    // Trigger Notification
                    await _notificationService.CreateNotificationAsync(report.Student.UserId, $"Weekly Report #{report.WeekNumber} has been {report.Status}.", "Report");
                    
                    // Invalidate Cache (Reports contribute to progress in future)
                    _dashboardService.InvalidateGuideCache(guideId);
                    _dashboardService.InvalidateStudentCache(report.StudentId);

                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                        return Json(new { success = true, message = $"Weekly Report {report.Status.ToLower()} successfully.", reload = true });

                    TempData["Success"] = $"Weekly Report {report.Status.ToLower()} successfully.";
                }
                catch (Exception ex)
                {
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                        return Json(new { success = false, message = "Error updating report: " + ex.Message });
                    TempData["Error"] = "Error updating report: " + ex.Message;
                }
            }
            return RedirectToAction("WeeklyReports");
        }

        public async Task<IActionResult> Tasks()
        {
            var guideId = await GetGuideIdAsync();
            var tasks = await _context.Tasks
                .Include(t => t.Student)
                .ThenInclude(s => s.User)
                .Where(t => t.GuideId == guideId)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
            return View(tasks);
        }

        public async Task<IActionResult> StudentDocuments(int studentId)
        {
            var guideId = await GetGuideIdAsync();
            var assignment = await _context.GuideAssignments
                .FirstOrDefaultAsync(ga => ga.GuideId == guideId && ga.StudentId == studentId);
            
            if (assignment == null) return Unauthorized();

            var student = await _context.Students.Include(s => s.User).FirstOrDefaultAsync(s => s.StudentId == studentId);
            var docs = await _context.Documents.Where(d => d.StudentId == studentId).ToListAsync();

            ViewBag.StudentName = student?.User?.Name;
            return View(docs);
        }

        public async Task<IActionResult> DownloadDocument(int id)
        {
            var guideId = await GetGuideIdAsync();
            var doc = await _context.Documents.Include(d => d.Student).FirstOrDefaultAsync(d => d.DocumentId == id);
            if (doc == null) return NotFound();

            var isAssigned = await _context.GuideAssignments.AnyAsync(ga => ga.GuideId == guideId && ga.StudentId == doc.StudentId);
            if (!isAssigned) return Unauthorized();

            var filePath = Path.Combine(_hostingEnvironment.WebRootPath, "uploads", doc.FilePath);
            if (!System.IO.File.Exists(filePath)) return NotFound();

            var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
            return File(fileBytes, "application/octet-stream", doc.FileName);
        }

        public async Task<IActionResult> StudentDetails(int id)
        {
            var guideId = await GetGuideIdAsync();
            if (guideId == 0) return RedirectToAction("Login", "Account");

            var assignment = await _context.GuideAssignments
                .FirstOrDefaultAsync(ga => ga.GuideId == guideId && ga.StudentId == id);
            
            if (assignment == null) 
            {
                TempData["Error"] = "Unauthorized: You are not supervising this student.";
                return RedirectToAction("Students");
            }

            var student = await _context.Students
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.StudentId == id);
            
            if (student == null) return NotFound();

            ViewBag.Internship = await _context.InternshipDetails.FirstOrDefaultAsync(i => i.StudentId == id);
            ViewBag.TotalDiaries = await _context.DailyDiaries.CountAsync(d => d.StudentId == id);
            ViewBag.PendingDiaries = await _context.DailyDiaries.CountAsync(d => d.StudentId == id && d.Status == "Pending");
            ViewBag.TotalTasks = await _context.Tasks.CountAsync(t => t.StudentId == id && t.GuideId == guideId);

            return View(student);
        }
    }
}
