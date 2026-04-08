using System;
using System.Collections.Generic;
using System.Linq;
using InternshipManagementSystem.Data;
using InternshipManagementSystem.Models;
using Microsoft.EntityFrameworkCore;

using Microsoft.Extensions.Logging;

namespace InternshipManagementSystem.Services
{
    public class TaskService : ITaskService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<TaskService> _logger;

        public TaskService(ApplicationDbContext context, ILogger<TaskService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IEnumerable<InternshipTask>> GetTasksByStudentIdAsync(int studentId)
        {
            return await _context.Tasks
                .Include(t => t.Guide).ThenInclude(g => g.User)
                .Where(t => t.StudentId == studentId)
                .OrderBy(t => t.Deadline)
                .ToListAsync();
        }

        public async Task<IEnumerable<InternshipTask>> GetTasksByGuideIdAsync(int guideId)
        {
            return await _context.Tasks
                .Include(t => t.Student).ThenInclude(s => s.User)
                .Where(t => t.GuideId == guideId)
                .OrderByDescending(t => t.Deadline)
                .ToListAsync();
        }

        public async Task<int> CountTasksByStudentIdAsync(int studentId)
        {
            return await _context.Tasks.CountAsync(t => t.StudentId == studentId);
        }

        public async Task<int> CountPendingTasksByStudentIdAsync(int studentId)
        {
            return await _context.Tasks.CountAsync(t => t.StudentId == studentId && t.Status != "Completed");
        }

        public async Task<int> CountTasksByGuideIdAsync(int guideId)
        {
            return await _context.Tasks.CountAsync(t => t.GuideId == guideId);
        }

        public async Task<int> CountTasksByStudentAndGuideIdAsync(int studentId, int guideId)
        {
            return await _context.Tasks.CountAsync(t => t.StudentId == studentId && t.GuideId == guideId);
        }

        public async Task<(bool Success, string Message)> CreateTaskAsync(InternshipTask task)
        {
            try
            {
                task.Status = "Assigned";
                _context.Tasks.Add(task);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Task assigned successfully to Student {StudentId} by Guide {GuideId}: {Title}", 
                    task.StudentId, task.GuideId, task.Title);
                    
                return (true, "Task assigned successfully.");
            }
            catch (Exception ex)
            {
                return (false, "Database error: " + ex.Message);
            }
        }

        public async Task<(bool Success, string Message)> UpdateTaskStatusAsync(int taskId, int studentId, string status)
        {
            var task = await _context.Tasks.FirstOrDefaultAsync(t => t.TaskId == taskId && t.StudentId == studentId);
            if (task != null)
            {
                try
                {
                    task.Status = status;
                    await _context.SaveChangesAsync();
                    return (true, "Task status updated.");
                }
                catch (Exception ex)
                {
                    return (false, "Error updating task: " + ex.Message);
                }
            }
            return (false, "Task not found.");
        }
    }
}
