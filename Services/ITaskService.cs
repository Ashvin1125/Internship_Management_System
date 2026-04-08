using System.Collections.Generic;
using InternshipManagementSystem.Models;

namespace InternshipManagementSystem.Services
{
    public interface ITaskService
    {
        Task<IEnumerable<InternshipTask>> GetTasksByStudentIdAsync(int studentId);
        Task<IEnumerable<InternshipTask>> GetTasksByGuideIdAsync(int guideId);
        Task<int> CountTasksByStudentIdAsync(int studentId);
        Task<int> CountPendingTasksByStudentIdAsync(int studentId);
        Task<int> CountTasksByGuideIdAsync(int guideId);
        Task<int> CountTasksByStudentAndGuideIdAsync(int studentId, int guideId);
        Task<(bool Success, string Message)> CreateTaskAsync(InternshipTask task);
        Task<(bool Success, string Message)> UpdateTaskStatusAsync(int taskId, int studentId, string status);
    }
}
