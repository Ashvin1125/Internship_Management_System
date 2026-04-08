using InternshipManagementSystem.Data;
using InternshipManagementSystem.Models;
using InternshipManagementSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.IO;
using Microsoft.AspNetCore.Hosting;

namespace InternshipManagementSystem.Controllers
{
    [Authorize(Roles = "Guide")]
    public class GuideController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _hostingEnvironment;

        public GuideController(ApplicationDbContext context, IWebHostEnvironment hostingEnvironment)
        {
            _context = context;
            _hostingEnvironment = hostingEnvironment;
        }

        private int GetGuideId()
        {
            var userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
            var guide = _context.Guides.FirstOrDefault(g => g.UserId == userId);
            return guide?.GuideId ?? 0;
        }

        public IActionResult Index()
        {
            var guideId = GetGuideId();
            var assignedStudentIds = _context.GuideAssignments
                .Where(g => g.GuideId == guideId).Select(g => g.StudentId).ToList();
            ViewBag.AssignedStudents = assignedStudentIds.Count;
            ViewBag.PendingDiaries = _context.DailyDiaries
                .Count(d => assignedStudentIds.Contains(d.StudentId) && d.Status == "Pending");
            ViewBag.TasksAssigned = _context.Tasks.Count(t => t.GuideId == guideId);
            return View();
        }

        public IActionResult Students()
        {
            var guideId = GetGuideId();
            var assignments = _context.GuideAssignments
                .Include(ga => ga.Student)
                .ThenInclude(s => s.User)
                .Where(ga => ga.GuideId == guideId)
                .ToList();
            return View(assignments);
        }
        
        public IActionResult Diaries()
        {
            var guideId = GetGuideId();
            
            // Get student IDs assigned to this guide
            var assignedStudentIds = _context.GuideAssignments
                .Where(ga => ga.GuideId == guideId)
                .Select(ga => ga.StudentId)
                .ToList();

            var diaries = _context.DailyDiaries
                .Include(d => d.Student)
                .ThenInclude(s => s.User)
                .Where(d => assignedStudentIds.Contains(d.StudentId))
                .OrderByDescending(d => d.WorkDate)
                .ToList();
                
            return View(diaries);
        }
        [HttpGet]
        public IActionResult AssignTask()
        {
            var guideId = GetGuideId();
            var students = _context.GuideAssignments
                .Include(ga => ga.Student).ThenInclude(s => s.User)
                .Where(ga => ga.GuideId == guideId)
                .Select(ga => ga.Student)
                .ToList();
            ViewBag.Students = students;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AssignTask(AssignTaskViewModel model)
        {
            var guideId = GetGuideId();
            if (guideId == 0) return RedirectToAction("Login", "Account");

            // Security: Check if student is assigned to this guide
            var isAssigned = _context.GuideAssignments.Any(ga => ga.GuideId == guideId && ga.StudentId == model.StudentId);
            if (!isAssigned)
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
                    _context.SaveChanges();

                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                        return Json(new { success = true, message = "Task assigned successfully.", redirectUrl = Url.Action("Tasks") });

                    TempData["Success"] = "Task assigned successfully.";
                    return RedirectToAction("Index");
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
            
            var students2 = _context.GuideAssignments
                .Include(ga => ga.Student).ThenInclude(s => s.User)
                .Where(ga => ga.GuideId == guideId)
                .Select(ga => ga.Student)
                .ToList();
            ViewBag.Students = students2;
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ApproveDiary(int diaryId, string action, string comment)
        {
            var guideId = GetGuideId();
            var diary = _context.DailyDiaries.Include(d => d.Student).FirstOrDefault(d => d.DiaryId == diaryId);
            
            if (diary != null)
            {
                // Security: Verify student belongs to this guide
                var isAssigned = _context.GuideAssignments.Any(ga => ga.GuideId == guideId && ga.StudentId == diary.StudentId);
                if (!isAssigned) return Unauthorized();

                try 
                {
                    diary.Status = action == "Approve" ? "Approved" : "Rejected";
                    diary.GuideComment = comment;
                    _context.SaveChanges();
                    
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

        public IActionResult Tasks()
        {
            var guideId = GetGuideId();
            var tasks = _context.Tasks
                .Include(t => t.Student)
                .ThenInclude(s => s.User)
                .Where(t => t.GuideId == guideId)
                .OrderByDescending(t => t.Deadline)
                .ToList();
            return View(tasks);
        }

        public IActionResult StudentDocuments(int studentId)
        {
            var guideId = GetGuideId();
            var assignment = _context.GuideAssignments
                .FirstOrDefault(ga => ga.GuideId == guideId && ga.StudentId == studentId);
            
            if (assignment == null) return Unauthorized();

            var student = _context.Students.Include(s => s.User).FirstOrDefault(s => s.StudentId == studentId);
            var docs = _context.Documents.Where(d => d.StudentId == studentId).ToList();

            ViewBag.StudentName = student?.User?.Name;
            return View(docs);
        }

        public IActionResult DownloadDocument(int id)
        {
            var guideId = GetGuideId();
            var doc = _context.Documents.Include(d => d.Student).FirstOrDefault(d => d.DocumentId == id);
            if (doc == null) return NotFound();

            // Security: Verify if this guide is assigned to the student who owns this document
            var isAssigned = _context.GuideAssignments.Any(ga => ga.GuideId == guideId && ga.StudentId == doc.StudentId);
            if (!isAssigned) return Unauthorized();

            var filePath = Path.Combine(_hostingEnvironment.WebRootPath, "uploads", doc.FilePath);
            if (!System.IO.File.Exists(filePath)) return NotFound();

            var fileBytes = System.IO.File.ReadAllBytes(filePath);
            return File(fileBytes, "application/octet-stream", doc.FileName);
        }

        public IActionResult StudentDetails(int id)
        {
            var guideId = GetGuideId();
            if (guideId == 0) return RedirectToAction("Login", "Account");

            // Security: Verify student belongs to this guide
            var assignment = _context.GuideAssignments
                .FirstOrDefault(ga => ga.GuideId == guideId && ga.StudentId == id);
            
            if (assignment == null) 
            {
                TempData["Error"] = "Unauthorized: You are not supervising this student.";
                return RedirectToAction("Students");
            }

            var student = _context.Students
                .Include(s => s.User)
                .FirstOrDefault(s => s.StudentId == id);
            
            if (student == null) return NotFound();

            ViewBag.Internship = _context.InternshipDetails.FirstOrDefault(i => i.StudentId == id);
            ViewBag.TotalDiaries = _context.DailyDiaries.Count(d => d.StudentId == id);
            ViewBag.PendingDiaries = _context.DailyDiaries.Count(d => d.StudentId == id && d.Status == "Pending");
            ViewBag.TotalTasks = _context.Tasks.Count(t => t.StudentId == id && t.GuideId == guideId);

            return View(student);
        }
    }
}
