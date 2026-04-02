using InternshipManagementSystem.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;

namespace InternshipManagementSystem.Data
{
    public static class DbSeeder
    {
        public static void Initialize(IServiceProvider serviceProvider)
        {
            using var context = new ApplicationDbContext(
                serviceProvider.GetRequiredService<DbContextOptions<ApplicationDbContext>>());

            context.Database.EnsureCreated(); // Ensure database exists

            if (!context.Users.Any(u => u.Role == "Admin"))
            {
                context.Users.Add(new User
                {
                    Name = "System Admin",
                    Email = "admin@example.com",
                    Password = "admin",
                    Role = "Admin"
                });
                
                var studentUser = new User { Name = "Jane Doe", Email = "student@example.com", Password = "password", Role = "Student" };
                context.Users.Add(studentUser);
                
                var guideUser = new User { Name = "Prof. Smith", Email = "guide@example.com", Password = "password", Role = "Guide" };
                context.Users.Add(guideUser);
                
                context.SaveChanges();
                
                context.Students.Add(new Student { UserId = studentUser.UserId, EnrollmentNumber = "EN1001", Department = "Computer Science", Semester = 8 });
                context.Guides.Add(new Guide { UserId = guideUser.UserId, Department = "Computer Science", Designation = "Senior Professor" });
                
                context.SaveChanges();
            }
        }
    }
}
