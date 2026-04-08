using System.Collections.Generic;
using InternshipManagementSystem.Models;
using InternshipManagementSystem.ViewModels;

namespace InternshipManagementSystem.Services
{
    public interface IGuideService
    {
        Task<int> GetGuideIdByUserIdAsync(int userId);
        Task<Guide> GetGuideByIdAsync(int guideId);
        Task<IEnumerable<Guide>> GetAllGuidesAsync();
        Task<(bool Success, string Message)> CreateGuideAsync(CreateGuideViewModel model);
        Task<(bool Success, string Message)> DeleteGuideAsync(int guideId);
        
        Task<IEnumerable<GuideAssignment>> GetAllAssignmentsAsync();
        Task<IEnumerable<Student>> GetAssignedStudentsAsync(int guideId);
        Task<IEnumerable<GuideAssignment>> GetAssignmentsByGuideIdAsync(int guideId);
        Task<IEnumerable<Student>> GetUnassignedStudentsAsync();
        Task<(bool Success, string Message)> AssignGuideToStudentAsync(int guideId, int studentId);
        Task<bool> IsStudentAssignedToGuideAsync(int guideId, int studentId);
        Task<GuideAssignment> GetAssignmentByStudentIdAsync(int studentId);
    }
}
