using System;
using System.Collections.Generic;
using System.Linq;
using InternshipManagementSystem.Data;
using InternshipManagementSystem.Models;
using InternshipManagementSystem.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace InternshipManagementSystem.Services
{
    public class GuideService : IGuideService
    {
        private readonly ApplicationDbContext _context;

        public GuideService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<int> GetGuideIdByUserIdAsync(int userId)
        {
            var guide = await _context.Guides.FirstOrDefaultAsync(g => g.UserId == userId);
            return guide?.GuideId ?? 0;
        }

        public async Task<Guide> GetGuideByIdAsync(int guideId)
        {
            return await _context.Guides
                .Include(g => g.User)
                .FirstOrDefaultAsync(g => g.GuideId == guideId);
        }

        public async Task<IEnumerable<Guide>> GetAllGuidesAsync()
        {
            return await _context.Guides.Include(g => g.User).ToListAsync();
        }

        public async Task<(bool Success, string Message)> CreateGuideAsync(CreateGuideViewModel model)
        {
            if (await _context.Users.AnyAsync(u => u.Email == model.Email))
            {
                return (false, "Email is already registered.");
            }

            try
            {
                var user = new User { Name = model.Name, Email = model.Email, Password = model.Password, Role = "Guide" };
                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                var guide = new Guide { UserId = user.UserId, Department = model.Department, Designation = model.Designation };
                _context.Guides.Add(guide);
                await _context.SaveChangesAsync();

                return (true, "Guide registered successfully.");
            }
            catch (Exception ex)
            {
                return (false, "Database error: " + ex.Message);
            }
        }

        public async Task<(bool Success, string Message)> DeleteGuideAsync(int guideId)
        {
            var guide = await _context.Guides.FindAsync(guideId);
            if (guide != null)
            {
                try
                {
                    var user = await _context.Users.FindAsync(guide.UserId);
                    if (user != null) _context.Users.Remove(user);
                    _context.Guides.Remove(guide);
                    await _context.SaveChangesAsync();
                    return (true, "Guide deleted successfully.");
                }
                catch (Exception ex)
                {
                    return (false, "Delete error: " + ex.Message);
                }
            }
            return (false, "Guide not found.");
        }

        public async Task<IEnumerable<GuideAssignment>> GetAllAssignmentsAsync()
        {
            return await _context.GuideAssignments
                .Include(ga => ga.Student).ThenInclude(s => s.User)
                .Include(ga => ga.Guide).ThenInclude(g => g.User)
                .ToListAsync();
        }

        public async Task<IEnumerable<Student>> GetAssignedStudentsAsync(int guideId)
        {
            return await _context.GuideAssignments
                .Include(ga => ga.Student).ThenInclude(s => s.User)
                .Where(ga => ga.GuideId == guideId)
                .Select(ga => ga.Student)
                .ToListAsync();
        }

        public async Task<IEnumerable<GuideAssignment>> GetAssignmentsByGuideIdAsync(int guideId)
        {
            return await _context.GuideAssignments
                .Include(ga => ga.Student).ThenInclude(s => s.User)
                .Where(ga => ga.GuideId == guideId)
                .ToListAsync();
        }

        public async Task<IEnumerable<Student>> GetUnassignedStudentsAsync()
        {
            var assignedStudentIds = await _context.GuideAssignments.Select(ga => ga.StudentId).ToListAsync();
            return await _context.Students
                .Include(s => s.User)
                .Where(s => !assignedStudentIds.Contains(s.StudentId))
                .ToListAsync();
        }

        public async Task<(bool Success, string Message)> AssignGuideToStudentAsync(int guideId, int studentId)
        {
            var exists = await _context.GuideAssignments.AnyAsync(ga => ga.StudentId == studentId);
            if (!exists)
            {
                try
                {
                    _context.GuideAssignments.Add(new GuideAssignment { GuideId = guideId, StudentId = studentId });
                    await _context.SaveChangesAsync();
                    return (true, "Guide assigned successfully.");
                }
                catch (Exception ex)
                {
                    return (false, "Assignment error: " + ex.Message);
                }
            }
            return (false, "Student already has a guide assigned.");
        }

        public async Task<bool> IsStudentAssignedToGuideAsync(int guideId, int studentId)
        {
            return await _context.GuideAssignments.AnyAsync(ga => ga.GuideId == guideId && ga.StudentId == studentId);
        }

        public async Task<GuideAssignment> GetAssignmentByStudentIdAsync(int studentId)
        {
            return await _context.GuideAssignments
                .Include(ga => ga.Guide).ThenInclude(g => g.User)
                .FirstOrDefaultAsync(ga => ga.StudentId == studentId);
        }
    }
}
