using System;
using System.Collections.Generic;
using System.Linq;
using InternshipManagementSystem.Data;
using InternshipManagementSystem.Models;
using Microsoft.EntityFrameworkCore;

using Microsoft.Extensions.Logging;

namespace InternshipManagementSystem.Services
{
    public class DailyDiaryService : IDailyDiaryService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DailyDiaryService> _logger;

        public DailyDiaryService(ApplicationDbContext context, ILogger<DailyDiaryService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IEnumerable<DailyDiary>> GetDiariesByStudentIdAsync(int studentId)
        {
            return await _context.DailyDiaries
                .Where(d => d.StudentId == studentId)
                .OrderByDescending(d => d.WorkDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<DailyDiary>> GetDiariesByStudentIdsAsync(IEnumerable<int> studentIds)
        {
            return await _context.DailyDiaries
                .Include(d => d.Student)
                .ThenInclude(s => s.User)
                .Where(d => studentIds.Contains(d.StudentId))
                .OrderByDescending(d => d.WorkDate)
                .ToListAsync();
        }

        public async Task<DailyDiary> GetDiaryByIdAsync(int diaryId)
        {
            return await _context.DailyDiaries
                .Include(d => d.Student)
                .FirstOrDefaultAsync(d => d.DiaryId == diaryId);
        }

        public async Task<int> CountDiariesByStudentIdAsync(int studentId)
        {
            return await _context.DailyDiaries.CountAsync(d => d.StudentId == studentId);
        }

        public async Task<int> CountPendingDiariesByStudentIdAsync(int studentId)
        {
            return await _context.DailyDiaries.CountAsync(d => d.StudentId == studentId && d.Status == "Pending");
        }

        public async Task<int> CountPendingDiariesByStudentIdsAsync(IEnumerable<int> studentIds)
        {
            return await _context.DailyDiaries.CountAsync(d => studentIds.Contains(d.StudentId) && d.Status == "Pending");
        }

        public async Task<(bool Success, string Message)> CreateDiaryAsync(DailyDiary diary)
        {
            try
            {
                diary.Status = "Pending";
                _context.DailyDiaries.Add(diary);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Diary submitted successfully by Student {StudentId} for work date {WorkDate}", 
                    diary.StudentId, diary.WorkDate.ToString("yyyy-MM-dd"));
                    
                return (true, "Daily diary submitted successfully.");
            }
            catch (Exception ex)
            {
                return (false, "Database error: " + ex.Message);
            }
        }

        public async Task<(bool Success, string Message)> UpdateDiaryStatusAsync(int diaryId, string status, string comment)
        {
            var diary = await _context.DailyDiaries.FirstOrDefaultAsync(d => d.DiaryId == diaryId);
            if (diary != null)
            {
                try
                {
                    diary.Status = status;
                    diary.GuideComment = comment;
                    await _context.SaveChangesAsync();
                    return (true, $"Diary {status.ToLower()} successfully.");
                }
                catch (Exception ex)
                {
                    return (false, "Error updating diary: " + ex.Message);
                }
            }
            return (false, "Diary not found.");
        }
    }
}
