using System.Collections.Generic;
using InternshipManagementSystem.Models;
using InternshipManagementSystem.ViewModels;

namespace InternshipManagementSystem.Services
{
    public interface IStudentService
    {
        Task<int> GetStudentIdByUserIdAsync(int userId);
        Task<Student> GetStudentByIdAsync(int studentId);
        Task<IEnumerable<Student>> GetAllStudentsAsync();
        Task<(bool Success, string Message)> CreateStudentAsync(CreateStudentViewModel model);
        Task<(bool Success, string Message)> DeleteStudentAsync(int studentId);
        Task<InternshipDetails> GetInternshipDetailsAsync(int studentId);
        Task<(bool Success, string Message)> AddInternshipDetailsAsync(int studentId, InternshipDetailsViewModel model);
    }
}
