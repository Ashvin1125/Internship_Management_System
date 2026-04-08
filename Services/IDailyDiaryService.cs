using System.Collections.Generic;
using InternshipManagementSystem.Models;

namespace InternshipManagementSystem.Services
{
    public interface IDailyDiaryService
    {
        Task<IEnumerable<DailyDiary>> GetDiariesByStudentIdAsync(int studentId);
        Task<IEnumerable<DailyDiary>> GetDiariesByStudentIdsAsync(IEnumerable<int> studentIds);
        Task<DailyDiary> GetDiaryByIdAsync(int diaryId);
        Task<int> CountDiariesByStudentIdAsync(int studentId);
        Task<int> CountPendingDiariesByStudentIdAsync(int studentId);
        Task<int> CountPendingDiariesByStudentIdsAsync(IEnumerable<int> studentIds);
        Task<(bool Success, string Message)> CreateDiaryAsync(DailyDiary diary);
        Task<(bool Success, string Message)> UpdateDiaryStatusAsync(int diaryId, string status, string comment);
    }
}
