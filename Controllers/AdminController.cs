using InternshipManagementSystem.Data;
using InternshipManagementSystem.Models;
using InternshipManagementSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;

namespace InternshipManagementSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            ViewBag.TotalStudents = _context.Students.Count();
            ViewBag.TotalGuides = _context.Guides.Count();
            ViewBag.TotalAssignments = _context.GuideAssignments.Count();
            var assignedStudentIds = _context.GuideAssignments.Select(g => g.StudentId).ToList();
            ViewBag.Unassigned = _context.Students.Count(s => !assignedStudentIds.Contains(s.StudentId));
            ViewBag.Assignments = _context.GuideAssignments
                .Include(ga => ga.Student).ThenInclude(s => s.User)
                .Include(ga => ga.Guide).ThenInclude(g => g.User)
                .ToList();
            return View();
        }

        public IActionResult Students()
        {
            var students = _context.Students.Include(s => s.User).ToList();
            return View(students);
        }

        [HttpGet]
        public IActionResult CreateStudent()
        {
            return View();
        }

        [HttpPost]
        public IActionResult CreateStudent(CreateStudentViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new User { Name = model.Name, Email = model.Email, Password = model.Password, Role = "Student" };
                _context.Users.Add(user);
                _context.SaveChanges();
                
                var student = new Student { UserId = user.UserId, EnrollmentNumber = model.EnrollmentNumber, Department = model.Department, Semester = model.Semester };
                _context.Students.Add(student);
                _context.SaveChanges();
                
                return RedirectToAction("Students");
            }
            return View(model);
        }

        public IActionResult Guides()
        {
            var guides = _context.Guides.Include(g => g.User).ToList();
            return View(guides);
        }

        [HttpGet]
        public IActionResult CreateGuide()
        {
            return View();
        }

        [HttpPost]
        public IActionResult CreateGuide(CreateGuideViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new User { Name = model.Name, Email = model.Email, Password = model.Password, Role = "Guide" };
                _context.Users.Add(user);
                _context.SaveChanges();
                
                var guide = new Guide { UserId = user.UserId, Department = model.Department, Designation = model.Designation };
                _context.Guides.Add(guide);
                _context.SaveChanges();
                
                return RedirectToAction("Guides");
            }
            return View(model);
        }

        [HttpGet]
        public IActionResult AssignGuide()
        {
            ViewBag.Guides = _context.Guides.Include(g => g.User).ToList();
            var assignedStudentIds = _context.GuideAssignments.Select(ga => ga.StudentId).ToList();
            ViewBag.Students = _context.Students.Include(s => s.User).Where(s => !assignedStudentIds.Contains(s.StudentId)).ToList();
            
            return View();
        }

        [HttpPost]
        public IActionResult AssignGuide(int guideId, int studentId)
        {
            if (guideId > 0 && studentId > 0)
            {
                // Check if already assigned
                var exists = _context.GuideAssignments.Any(ga => ga.StudentId == studentId);
                if (!exists)
                {
                    _context.GuideAssignments.Add(new GuideAssignment { GuideId = guideId, StudentId = studentId });
                    _context.SaveChanges();
                }
                return RedirectToAction("Index");
            }
            return View();
        }
    }
}
