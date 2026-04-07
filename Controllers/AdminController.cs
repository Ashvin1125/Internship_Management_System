using InternshipManagementSystem.Data;
using InternshipManagementSystem.Models;
using InternshipManagementSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
        [ValidateAntiForgeryToken]
        public IActionResult CreateStudent(CreateStudentViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Check if email already exists
                if (_context.Users.Any(u => u.Email == model.Email))
                {
                    ModelState.AddModelError("Email", "Email is already registered.");
                    return View(model);
                }

                // Check if enrollment already exists
                if (_context.Students.Any(s => s.EnrollmentNumber == model.EnrollmentNumber))
                {
                    ModelState.AddModelError("EnrollmentNumber", "Enrollment number is already registered.");
                    return View(model);
                }

                try 
                {
                    var user = new User { Name = model.Name, Email = model.Email, Password = model.Password, Role = "Student" };
                    _context.Users.Add(user);
                    _context.SaveChanges();
                    
                    var student = new Student { UserId = user.UserId, EnrollmentNumber = model.EnrollmentNumber, Department = model.Department, Semester = model.Semester };
                    _context.Students.Add(student);
                    _context.SaveChanges();
                    
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                        return Json(new { success = true, message = "Student registered successfully.", redirectUrl = Url.Action("Students") });

                    TempData["Success"] = "Student registered successfully.";
                    return RedirectToAction("Students");
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
        [ValidateAntiForgeryToken]
        public IActionResult CreateGuide(CreateGuideViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Check if email already exists
                if (_context.Users.Any(u => u.Email == model.Email))
                {
                    ModelState.AddModelError("Email", "Email is already registered.");
                    return View(model);
                }

                try 
                {
                    var user = new User { Name = model.Name, Email = model.Email, Password = model.Password, Role = "Guide" };
                    _context.Users.Add(user);
                    _context.SaveChanges();
                    
                    var guide = new Guide { UserId = user.UserId, Department = model.Department, Designation = model.Designation };
                    _context.Guides.Add(guide);
                    _context.SaveChanges();
                    
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                        return Json(new { success = true, message = "Guide registered successfully.", redirectUrl = Url.Action("Guides") });

                    TempData["Success"] = "Guide registered successfully.";
                    return RedirectToAction("Guides");
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

        [HttpGet]
        public IActionResult AssignGuide()
        {
            ViewBag.Guides = _context.Guides.Include(g => g.User).ToList();
            var assignedStudentIds = _context.GuideAssignments.Select(ga => ga.StudentId).ToList();
            ViewBag.Students = _context.Students.Include(s => s.User).Where(s => !assignedStudentIds.Contains(s.StudentId)).ToList();
            
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AssignGuide(int guideId, int studentId)
        {
            if (guideId > 0 && studentId > 0)
            {
                // Check if already assigned
                var exists = _context.GuideAssignments.Any(ga => ga.StudentId == studentId);
                if (!exists)
                {
                    try 
                    {
                        _context.GuideAssignments.Add(new GuideAssignment { GuideId = guideId, StudentId = studentId });
                        _context.SaveChanges();
                        TempData["Success"] = "Guide assigned successfully.";
                    }
                    catch (Exception ex)
                    {
                        TempData["Error"] = "Assignment error: " + ex.Message;
                    }
                }
                else 
                {
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                        return Json(new { success = false, message = "Student already has a guide assigned." });
                    TempData["Error"] = "Student already has a guide assigned.";
                }

                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return Json(new { success = true, message = "Guide assigned successfully.", redirectUrl = Url.Action("Index") });
                return RedirectToAction("Index");
            }
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                return Json(new { success = false, message = "Please select both a guide and a student." });
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteStudent(int id)
        {
            var student = _context.Students.Find(id);
            if (student != null)
            {
                try 
                {
                    var user = _context.Users.Find(student.UserId);
                    if (user != null) _context.Users.Remove(user);
                    _context.Students.Remove(student);
                    _context.SaveChanges();
                    TempData["Success"] = "Student deleted successfully.";
                }
                catch (Exception ex)
                {
                    TempData["Error"] = "Delete error: " + ex.Message;
                }
            }
            return RedirectToAction("Students");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteGuide(int id)
        {
            var guide = _context.Guides.Find(id);
            if (guide != null)
            {
                try 
                {
                    var user = _context.Users.Find(guide.UserId);
                    if (user != null) _context.Users.Remove(user);
                    _context.Guides.Remove(guide);
                    _context.SaveChanges();
                    TempData["Success"] = "Guide deleted successfully.";
                }
                catch (Exception ex)
                {
                    TempData["Error"] = "Delete error: " + ex.Message;
                }
            }
            return RedirectToAction("Guides");
        }

        public IActionResult StudentDetails(int id)
        {
            var student = _context.Students
                .Include(s => s.User)
                .FirstOrDefault(s => s.StudentId == id);
            
            if (student == null) return NotFound();

            ViewBag.Internship = _context.InternshipDetails.FirstOrDefault(i => i.StudentId == id);
            ViewBag.Assignment = _context.GuideAssignments
                .Include(ga => ga.Guide).ThenInclude(g => g.User)
                .FirstOrDefault(ga => ga.StudentId == id);

            return View(student);
        }

        public IActionResult GuideDetails(int id)
        {
            var guide = _context.Guides
                .Include(g => g.User)
                .FirstOrDefault(g => g.GuideId == id);
            
            if (guide == null) return NotFound();

            ViewBag.AssignedStudents = _context.GuideAssignments
                .Include(ga => ga.Student).ThenInclude(s => s.User)
                .Where(ga => ga.GuideId == id)
                .ToList();

            return View(guide);
        }
    }
}
