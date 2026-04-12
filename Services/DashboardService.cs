using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InternshipManagementSystem.Data;
using InternshipManagementSystem.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace InternshipManagementSystem.Services
{
    public class DashboardService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMemoryCache _cache;
        private const int CacheMinutes = 5;

        public DashboardService(ApplicationDbContext context, IMemoryCache cache)
        {
            _context = context;
            _cache = cache;
        }

        public async Task<AdminStats> GetAdminStatsAsync()
        {
            const string cacheKey = "AdminDashboardStats";
            if (!_cache.TryGetValue(cacheKey, out AdminStats stats))
            {
                stats = new AdminStats
                {
                    TotalStudents = await _context.Students.CountAsync(),
                    TotalGuides = await _context.Guides.CountAsync(),
                    TotalAssignments = await _context.GuideAssignments.CountAsync(),
                    UnassignedStudents = await _context.Students.CountAsync(s => !_context.GuideAssignments.Any(ga => ga.StudentId == s.StudentId)),
                    DepartmentDistribution = await _context.Students
                        .GroupBy(s => s.Department)
                        .Select(g => new KeyValuePair<string, int>(g.Key, g.Count()))
                        .ToListAsync()
                };

                _cache.Set(cacheKey, stats, TimeSpan.FromMinutes(CacheMinutes));
            }
            return stats;
        }

        public async Task<GuideStats> GetGuideStatsAsync(int guideId)
        {
            string cacheKey = $"GuideDashboardStats_{guideId}";
            if (!_cache.TryGetValue(cacheKey, out GuideStats stats))
            {
                var assignedStudentIds = await _context.GuideAssignments
                    .Where(ga => ga.GuideId == guideId)
                    .Select(ga => ga.StudentId)
                    .ToListAsync();

                stats = new GuideStats
                {
                    AssignedStudents = assignedStudentIds.Count,
                    PendingDiaries = await _context.DailyDiaries
                        .CountAsync(d => assignedStudentIds.Contains(d.StudentId) && d.Status == "Pending"),
                    TasksAssigned = await _context.Tasks.CountAsync(t => t.GuideId == guideId),
                    TasksCompleted = await _context.Tasks.CountAsync(t => t.GuideId == guideId && t.Status == "Completed"),
                    RecentDiaries = await _context.DailyDiaries
                        .Include(d => d.Student.User)
                        .Where(d => assignedStudentIds.Contains(d.StudentId))
                        .OrderByDescending(d => d.CreatedAt)
                        .Take(5)
                        .ToListAsync()
                };

                _cache.Set(cacheKey, stats, TimeSpan.FromMinutes(CacheMinutes));
            }
            return stats;
        }

        public async Task<StudentStats> GetStudentStatsAsync(int studentId)
        {
            string cacheKey = $"StudentDashboardStats_{studentId}";
            if (!_cache.TryGetValue(cacheKey, out StudentStats stats))
            {
                var diaries = await _context.DailyDiaries.Where(d => d.StudentId == studentId).ToListAsync();
                var tasks = await _context.Tasks.Where(t => t.StudentId == studentId).ToListAsync();
                var internship = await _context.InternshipDetails.FirstOrDefaultAsync(i => i.StudentId == studentId);

                int daysRemaining = 0;
                int progress = 0;
                if (internship != null)
                {
                    var totalDays = (internship.EndDate - internship.StartDate).Days;
                    if (totalDays > 0)
                    {
                        var daysDone = (DateTime.UtcNow - internship.StartDate).Days;
                        progress = Math.Clamp((int)((double)daysDone / totalDays * 100), 0, 100);
                        daysRemaining = Math.Max(0, (internship.EndDate - DateTime.UtcNow).Days);
                    }
                }

                stats = new StudentStats
                {
                    TotalDiaries = diaries.Count,
                    ApprovedDiaries = diaries.Count(d => d.Status == "Approved"),
                    TasksAssigned = tasks.Count,
                    TasksCompleted = tasks.Count(t => t.Status == "Completed"),
                    DaysRemaining = daysRemaining,
                    CompanyName = internship?.CompanyName ?? "No Internship",
                    ProgressPercentage = progress,
                    UpcomingDeadlines = tasks.Where(t => t.Status != "Completed" && t.Deadline > DateTime.UtcNow)
                        .OrderBy(t => t.Deadline)
                        .Take(3)
                        .ToList()
                };

                _cache.Set(cacheKey, stats, TimeSpan.FromMinutes(CacheMinutes));
            }
            return stats;
        }

        public void InvalidateAdminCache()
        {
            _cache.Remove("AdminDashboardStats");
        }

        public void InvalidateGuideCache(int guideId)
        {
            _cache.Remove($"GuideDashboardStats_{guideId}");
        }

        public void InvalidateStudentCache(int studentId)
        {
            _cache.Remove($"StudentDashboardStats_{studentId}");
        }

        public void InvalidateAll()
        {
            _cache.Remove("AdminDashboardStats");
        }
    }

    public class AdminStats
    {
        public int TotalStudents { get; set; }
        public int TotalGuides { get; set; }
        public int TotalAssignments { get; set; }
        public int UnassignedStudents { get; set; }
        public List<KeyValuePair<string, int>> DepartmentDistribution { get; set; }
    }

    public class GuideStats
    {
        public int AssignedStudents { get; set; }
        public int PendingDiaries { get; set; }
        public int TasksAssigned { get; set; }
        public int TasksCompleted { get; set; }
        public List<DailyDiary> RecentDiaries { get; set; }
    }

    public class StudentStats
    {
        public int TotalDiaries { get; set; }
        public int ApprovedDiaries { get; set; }
        public int TasksAssigned { get; set; }
        public int TasksCompleted { get; set; }
        public int DaysRemaining { get; set; }
        public string CompanyName { get; set; }
        public int ProgressPercentage { get; set; }
        public List<InternshipTask> UpcomingDeadlines { get; set; }
    }
}
