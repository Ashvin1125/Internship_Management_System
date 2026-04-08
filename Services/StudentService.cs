using System;
using System.Collections.Generic;
using System.Linq;
using InternshipManagementSystem.Data;
using InternshipManagementSystem.Models;
using InternshipManagementSystem.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace InternshipManagementSystem.Services
{
    public class StudentService : IStudentService
    {
        private readonly ApplicationDbContext _context;

        public StudentService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<int> GetStudentIdByUserIdAsync(int userId)
        {
            var student = await _context.Students.FirstOrDefaultAsync(s => s.UserId == userId);
            return student?.StudentId ?? 0;
        }

        public async Task<Student> GetStudentByIdAsync(int studentId)
        {
            return await _context.Students
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.StudentId == studentId);
        }

        public async Task<IEnumerable<Student>> GetAllStudentsAsync()
        {
            return await _context.Students.Include(s => s.User).ToListAsync();
        }

        public async Task<(bool Success, string Message)> CreateStudentAsync(CreateStudentViewModel model)
        {
            if (await _context.Users.AnyAsync(u => u.Email == model.Email))
            {
                return (false, "Email is already registered.");
            }

            if (await _context.Students.AnyAsync(s => s.EnrollmentNumber == model.EnrollmentNumber))
            {
                return (false, "Enrollment number is already registered.");
            }

            try
            {
                var user = new User { Name = model.Name, Email = model.Email, Password = model.Password, Role = "Student" };
                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                var student = new Student { UserId = user.UserId, EnrollmentNumber = model.EnrollmentNumber, Department = model.Department, Semester = model.Semester };
                _context.Students.Add(student);
                await _context.SaveChangesAsync();

                return (true, "Student registered successfully.");
            }
            catch (Exception ex)
            {
                return (false, "Database error: " + ex.Message);
            }
        }

        public async Task<(bool Success, string Message)> DeleteStudentAsync(int studentId)
        {
            var student = await _context.Students.FindAsync(studentId);
            if (student != null)
            {
                try
                {
                    var user = await _context.Users.FindAsync(student.UserId);
                    if (user != null) _context.Users.Remove(user);
                    _context.Students.Remove(student);
                    await _context.SaveChangesAsync();
                    return (true, "Student deleted successfully.");
                }
                catch (Exception ex)
                {
                    return (false, "Delete error: " + ex.Message);
                }
            }
            return (false, "Student not found.");
        }

        public async Task<InternshipDetails> GetInternshipDetailsAsync(int studentId)
        {
            return await _context.InternshipDetails.FirstOrDefaultAsync(i => i.StudentId == studentId);
        }

        public async Task<(bool Success, string Message)> AddInternshipDetailsAsync(int studentId, InternshipDetailsViewModel model)
        {
            var existing = await _context.InternshipDetails.FirstOrDefaultAsync(i => i.StudentId == studentId);
            if (existing != null) return (false, "Internship details already exist.");

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
                return (true, "Internship details added.");
            }
            catch (Exception ex)
            {
                return (false, "Database error: " + ex.Message);
            }
        }
    }
}
