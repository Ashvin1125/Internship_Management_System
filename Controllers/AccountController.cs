using InternshipManagementSystem.Data;
using InternshipManagementSystem.Models;
using InternshipManagementSystem.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace InternshipManagementSystem.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AccountController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Login()
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToDashboard(User.FindFirst(ClaimTypes.Role)?.Value);
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = _context.Users.FirstOrDefault(u => u.Email == model.Email && u.Password == model.Password);
                if (user != null)
                {
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Name, user.Name),
                        new Claim(ClaimTypes.Email, user.Email),
                        new Claim(ClaimTypes.Role, user.Role),
                        new Claim("UserId", user.UserId.ToString())
                    };

                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                    await HttpContext.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme, 
                        new ClaimsPrincipal(claimsIdentity));

                    return RedirectToDashboard(user.Role);
                }
                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            }
            return View(model);
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Account");
        }

        public IActionResult AccessDenied()
        {
            return View();
        }

        [Authorize]
        [HttpGet]
        public IActionResult Profile()
        {
            var userIdStr = User.FindFirst("UserId")?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId)) 
                return RedirectToAction("Login");

            var user = _context.Users.FirstOrDefault(u => u.UserId == userId);
            if (user == null) return NotFound();

            var model = new ProfileViewModel
            {
                Name = user.Name,
                Email = user.Email,
                Role = user.Role
            };

            // Fetch role-specific details
            if (user.Role == "Student")
            {
                var student = _context.Students.FirstOrDefault(s => s.UserId == userId);
                if (student != null)
                {
                    model.Department = student.Department;
                    model.EnrollmentOrDesignation = student.EnrollmentNumber;
                }
            }
            else if (user.Role == "Guide")
            {
                var guide = _context.Guides.FirstOrDefault(g => g.UserId == userId);
                if (guide != null)
                {
                    model.Department = guide.Department;
                    model.EnrollmentOrDesignation = guide.Designation;
                }
            }

            return View(model);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Profile(ProfileViewModel model)
        {
            var userIdStr = User.FindFirst("UserId")?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId)) 
                return RedirectToAction("Login");

            var user = _context.Users.Find(userId);
            if (user == null) return NotFound();

            if (ModelState.IsValid)
            {
                try 
                {
                    user.Name = model.Name;
                    if (!string.IsNullOrEmpty(model.NewPassword))
                    {
                        user.Password = model.NewPassword;
                    }
                    _context.SaveChanges();
                    
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                        return Json(new { success = true, message = "Profile updated successfully.", reload = true });

                    TempData["Success"] = "Profile updated successfully.";
                    return RedirectToAction("Profile");
                }
                catch (Exception ex)
                {
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                        return Json(new { success = false, message = "Update error: " + ex.Message });
                    ModelState.AddModelError("", "Update error: " + ex.Message);
                }
            }

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                return Json(new { success = false, message = "Please check your inputs." });

            // Restore metadata for the view if invalid
            model.Role = user.Role;
            if (user.Role == "Student") model.EnrollmentOrDesignation = _context.Students.FirstOrDefault(s => s.UserId == userId)?.EnrollmentNumber;
            if (user.Role == "Guide") model.EnrollmentOrDesignation = _context.Guides.FirstOrDefault(g => g.UserId == userId)?.Designation;
            
            return View(model);
        }

        private IActionResult RedirectToDashboard(string role)
        {
            if (role == "Admin") return RedirectToAction("Index", "Admin");
            if (role == "Guide") return RedirectToAction("Index", "Guide");
            if (role == "Student") return RedirectToAction("Index", "Student");
            
            return RedirectToAction("Login");
        }
    }
}
