using InternshipManagementSystem.Data;
using InternshipManagementSystem.Models;
using InternshipManagementSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using InternshipManagementSystem.Services;

namespace InternshipManagementSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly DashboardService _dashboardService;

        public AdminController(ApplicationDbContext context, DashboardService dashboardService)
        {
            _context = context;
            _dashboardService = dashboardService;
        }

        public async Task<IActionResult> Index()
        {
            var stats = await _dashboardService.GetAdminStatsAsync();
            
            // Still need assignments list for the table in Index
            ViewBag.Assignments = await _context.GuideAssignments
                .Include(ga => ga.Student).ThenInclude(s => s.User)
                .Include(ga => ga.Guide).ThenInclude(g => g.User)
                .ToListAsync();

            return View(stats);
        }

        public async Task<IActionResult> Students()
        {
            var students = await _context.Students.Include(s => s.User).ToListAsync();
            return View(students);
        }

        [HttpGet]
        public IActionResult CreateStudent()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateStudent(CreateStudentViewModel model)
        {
            if (ModelState.IsValid)
            {
                try 
                {
                    var user = new User { Name = model.Name, Email = model.Email, Password = model.Password, Role = "Student" };
                    _context.Users.Add(user);
                    await _context.SaveChangesAsync();
                    
                    var student = new Student { UserId = user.UserId, EnrollmentNumber = model.EnrollmentNumber, Department = model.Department, Semester = model.Semester };
                    _context.Students.Add(student);
                    await _context.SaveChangesAsync();
                    
                    _dashboardService.InvalidateAdminCache();

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

        public async Task<IActionResult> Guides()
        {
            var guides = await _context.Guides.Include(g => g.User).ToListAsync();
            return View(guides);
        }

        [HttpGet]
        public IActionResult CreateGuide()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateGuide(CreateGuideViewModel model)
        {
            if (ModelState.IsValid)
            {
                try 
                {
                    var user = new User { Name = model.Name, Email = model.Email, Password = model.Password, Role = "Guide" };
                    _context.Users.Add(user);
                    await _context.SaveChangesAsync();
                    
                    var guide = new Guide { UserId = user.UserId, Department = model.Department, Designation = model.Designation };
                    _context.Guides.Add(guide);
                    await _context.SaveChangesAsync();
                    
                    _dashboardService.InvalidateAdminCache();

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
        public async Task<IActionResult> AssignGuide()
        {
            ViewBag.Guides = await _context.Guides.Include(g => g.User).ToListAsync();
            var assignedStudentIds = await _context.GuideAssignments.Select(ga => ga.StudentId).ToListAsync();
            ViewBag.Students = await _context.Students.Include(s => s.User).Where(s => !assignedStudentIds.Contains(s.StudentId)).ToListAsync();
            
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignGuide(int guideId, int studentId)
        {
            if (guideId > 0 && studentId > 0)
            {
                // Check if already assigned
                var exists = await _context.GuideAssignments.AnyAsync(ga => ga.StudentId == studentId);
                if (!exists)
                {
                    try 
                    {
                        _context.GuideAssignments.Add(new GuideAssignment { GuideId = guideId, StudentId = studentId });
                        await _context.SaveChangesAsync();
                        
                        _dashboardService.InvalidateAdminCache();
                        _dashboardService.InvalidateGuideCache(guideId);

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
        public async Task<IActionResult> DeleteStudent(int id)
        {
            var student = await _context.Students.FindAsync(id);
            if (student != null)
            {
                try 
                {
                    var user = await _context.Users.FindAsync(student.UserId);
                    if (user != null) _context.Users.Remove(user);
                    _context.Students.Remove(student);
                    await _context.SaveChangesAsync();
                    
                    _dashboardService.InvalidateAdminCache();

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
        public async Task<IActionResult> DeleteGuide(int id)
        {
            var guide = await _context.Guides.FindAsync(id);
            if (guide != null)
            {
                try 
                {
                    var user = await _context.Users.FindAsync(guide.UserId);
                    if (user != null) _context.Users.Remove(user);
                    _context.Guides.Remove(guide);
                    await _context.SaveChangesAsync();
                    
                    _dashboardService.InvalidateAdminCache();

                    TempData["Success"] = "Guide deleted successfully.";
                }
                catch (Exception ex)
                {
                    TempData["Error"] = "Delete error: " + ex.Message;
                }
            }
            return RedirectToAction("Guides");
        }

        public async Task<IActionResult> StudentDetails(int id)
        {
            var student = await _context.Students
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.StudentId == id);
            
            if (student == null) return NotFound();

            ViewBag.Internship = await _context.InternshipDetails.FirstOrDefaultAsync(i => i.StudentId == id);
            ViewBag.Assignment = await _context.GuideAssignments
                .Include(ga => ga.Guide).ThenInclude(g => g.User)
                .FirstOrDefaultAsync(ga => ga.StudentId == id);

            return View(student);
        }

        public async Task<IActionResult> GuideDetails(int id)
        {
            var guide = await _context.Guides
                .Include(g => g.User)
                .FirstOrDefaultAsync(g => g.GuideId == id);
            
            if (guide == null) return NotFound();

            ViewBag.AssignedStudents = await _context.GuideAssignments
                .Include(ga => ga.Student).ThenInclude(s => s.User)
                .Where(ga => ga.GuideId == id)
                .ToListAsync();

            return View(guide);
        }
    }
}
