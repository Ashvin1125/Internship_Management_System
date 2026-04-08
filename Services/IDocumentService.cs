using System.Collections.Generic;
using InternshipManagementSystem.Models;

namespace InternshipManagementSystem.Services
{
    public interface IDocumentService
    {
        Task<IEnumerable<Document>> GetDocumentsByStudentIdAsync(int studentId);
        Task<Document> GetDocumentByIdAsync(int documentId);
        Task<(bool Success, string Message)> AddDocumentAsync(Document document);
        Task<(bool Success, string Message, string FileName)> UploadDocumentAsync(Microsoft.AspNetCore.Http.IFormFile file, string uploadsFolder, int studentId);
        Task<(bool Success, string Message)> DeleteDocumentAsync(int documentId, int studentId);
    }
}
