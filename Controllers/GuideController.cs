using InternshipManagementSystem.Data;
using InternshipManagementSystem.Models;
using InternshipManagementSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace InternshipManagementSystem.Controllers
{
    [Authorize(Roles = "Guide")]
    public class GuideController : Controller
    {
        private readonly ApplicationDbContext _context;

        public GuideController(ApplicationDbContext context)
        {
            _context = context;
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
        public IActionResult AssignTask(AssignTaskViewModel model)
        {
            var guideId = GetGuideId();
            if (guideId == 0) return RedirectToAction("Index");

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
                _context.Tasks.Add(task);
                _context.SaveChanges();
                return RedirectToAction("Index");
            }

            var students2 = _context.GuideAssignments
                .Include(ga => ga.Student).ThenInclude(s => s.User)
                .Where(ga => ga.GuideId == guideId)
                .Select(ga => ga.Student)
                .ToList();
            ViewBag.Students = students2;
            return View(model);
        }

        [HttpPost]
        public IActionResult ApproveDiary(int diaryId, string action, string comment)
        {
            var diary = _context.DailyDiaries.Find(diaryId);
            if (diary != null)
            {
                diary.Status = action == "Approve" ? "Approved" : "Rejected";
                diary.GuideComment = comment;
                _context.SaveChanges();
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
    }
}
