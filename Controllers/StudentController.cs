using InternshipManagementSystem.Data;
using InternshipManagementSystem.Models;
using InternshipManagementSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace InternshipManagementSystem.Controllers
{
    [Authorize(Roles = "Student")]
    public class StudentController : Controller
    {
        private readonly ApplicationDbContext _context;

        public StudentController(ApplicationDbContext context)
        {
            _context = context;
        }

        private int GetStudentId()
        {
            var userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
            var student = _context.Students.FirstOrDefault(s => s.UserId == userId);
            return student?.StudentId ?? 0;
        }

        public IActionResult Index()
        {
            var studentId = GetStudentId();
            ViewBag.TotalDiaries = _context.DailyDiaries.Count(d => d.StudentId == studentId);
            ViewBag.TasksAssigned = _context.Tasks.Count(t => t.StudentId == studentId);
            ViewBag.PendingTasks = _context.Tasks.Count(t => t.StudentId == studentId && t.Status != "Completed");
            ViewBag.HasInternship = _context.InternshipDetails.Any(i => i.StudentId == studentId);
            return View();
        }

        // ── Daily Diary ──────────────────────────────────────────────
        public IActionResult Diary()
        {
            var studentId = GetStudentId();
            var diaries = _context.DailyDiaries
                .Where(d => d.StudentId == studentId)
                .OrderByDescending(d => d.WorkDate)
                .ToList();
            return View(diaries);
        }

        [HttpGet]
        public IActionResult CreateDiary()
        {
            return View();
        }

        [HttpPost]
        public IActionResult CreateDiary(DailyDiary model)
        {
            var studentId = GetStudentId();
            if (studentId == 0) return RedirectToAction("Index");
            model.StudentId = studentId;
            model.Status = "Pending";
            ModelState.Remove("Student");
            ModelState.Remove("GuideComment");
            if (ModelState.IsValid)
            {
                _context.DailyDiaries.Add(model);
                _context.SaveChanges();
                return RedirectToAction("Diary");
            }
            return View(model);
        }

        // ── Tasks ─────────────────────────────────────────────────────
        public IActionResult Tasks()
        {
            var studentId = GetStudentId();
            var tasks = _context.Tasks
                .Include(t => t.Guide).ThenInclude(g => g.User)
                .Where(t => t.StudentId == studentId)
                .OrderBy(t => t.Deadline)
                .ToList();
            return View(tasks);
        }

        [HttpPost]
        public IActionResult UpdateTaskStatus(int taskId, string status)
        {
            var studentId = GetStudentId();
            var task = _context.Tasks.FirstOrDefault(t => t.TaskId == taskId && t.StudentId == studentId);
            if (task != null)
            {
                task.Status = status;
                _context.SaveChanges();
            }
            return RedirectToAction("Tasks");
        }

        // ── Internship Details ────────────────────────────────────────
        public IActionResult Internship()
        {
            var studentId = GetStudentId();
            var internship = _context.InternshipDetails.FirstOrDefault(i => i.StudentId == studentId);
            return View(internship);
        }

        [HttpGet]
        public IActionResult AddInternship()
        {
            var studentId = GetStudentId();
            var existing = _context.InternshipDetails.FirstOrDefault(i => i.StudentId == studentId);
            if (existing != null) return RedirectToAction("Internship");
            return View();
        }

        [HttpPost]
        public IActionResult AddInternship(InternshipDetailsViewModel model)
        {
            var studentId = GetStudentId();
            if (studentId == 0) return RedirectToAction("Index");
            if (ModelState.IsValid)
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
                _context.SaveChanges();
                return RedirectToAction("Internship");
            }
            return View(model);
        }

        // ── Weekly Reports ────────────────────────────────────────────
        public IActionResult WeeklyReports()
        {
            var studentId = GetStudentId();
            var reports = _context.WeeklyReports
                .Where(r => r.StudentId == studentId)
                .OrderByDescending(r => r.WeekNumber)
                .ToList();
            return View(reports);
        }

        [HttpGet]
        public IActionResult SubmitReport()
        {
            return View();
        }

        [HttpPost]
        public IActionResult SubmitReport(WeeklyReport model)
        {
            var studentId = GetStudentId();
            if (studentId == 0) return RedirectToAction("Index");
            model.StudentId = studentId;
            model.Status = "Pending";
            ModelState.Remove("Student");
            ModelState.Remove("GuideComment");
            if (ModelState.IsValid)
            {
                _context.WeeklyReports.Add(model);
                _context.SaveChanges();
                return RedirectToAction("WeeklyReports");
            }
            return View(model);
        }
    }
}
