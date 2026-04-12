using InternshipManagementSystem.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using System;
using System.Collections.Generic;

namespace InternshipManagementSystem.Data
{
    public static class DbSeeder
    {
        public static void Initialize(IServiceProvider serviceProvider)
        {
            using var context = new ApplicationDbContext(
                serviceProvider.GetRequiredService<DbContextOptions<ApplicationDbContext>>());

            // DB already initialized by Program.cs — just seed if empty

            if (!context.Users.Any(u => u.Role == "Admin"))
            {
                // 1. Admin
                context.Users.Add(new User { Name = "System Admin", Email = "admin@example.com", Password = "admin", Role = "Admin" });

                // 2. Guides (3 total)
                var guideUsers = new List<User>
                {
                    new User { Name = "Prof. Arvind Sharma", Email = "arvind@example.com", Password = "password", Role = "Guide" },
                    new User { Name = "Dr. Priya Patel", Email = "priya@example.com", Password = "password", Role = "Guide" },
                    new User { Name = "Prof. Rajesh Kumar", Email = "rajesh@example.com", Password = "password", Role = "Guide" }
                };
                context.Users.AddRange(guideUsers);
                context.SaveChanges();

                context.Guides.Add(new Guide { UserId = guideUsers[0].UserId, Department = "Computer Science", Designation = "Senior Professor" });
                context.Guides.Add(new Guide { UserId = guideUsers[1].UserId, Department = "Information Technology", Designation = "Assistant Professor" });
                context.Guides.Add(new Guide { UserId = guideUsers[2].UserId, Department = "Electrical Engineering", Designation = "Associate Professor" });

                // 3. Students (10 total)
                var studentNames = new[] { "Aryan Singh", "Ishani Roy", "Kabir Das", "Meera Nair", "Rohan Gupta", "Sanya Verma", "Tushar Rao", "Aditi Shah", "Vikram Joshi", "Zoya Khan" };
                var depts = new[] { "Computer Science", "Information Technology", "Computer Science", "Information Technology", "Electrical", "Mechanical", "Civil", "Computer Science", "Computer Science", "IT" };
                
                for (int i = 0; i < 10; i++)
                {
                    var studentUser = new User 
                    { 
                        Name = studentNames[i], 
                        Email = $"student{i+1}@example.com", 
                        Password = "password", 
                        Role = "Student" 
                    };
                    context.Users.Add(studentUser);
                    context.SaveChanges();

                    context.Students.Add(new Student 
                    { 
                        UserId = studentUser.UserId, 
                        EnrollmentNumber = "EN" + (1000 + i + 1), 
                        Department = depts[i % depts.Length], 
                        Semester = 8 
                    });
                }
                
                context.SaveChanges();
            }
        }
    }
}
