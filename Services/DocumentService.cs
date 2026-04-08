using System;
using System.Collections.Generic;
using System.Linq;
using InternshipManagementSystem.Data;
using InternshipManagementSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace InternshipManagementSystem.Services
{
    public class DocumentService : IDocumentService
    {
        private readonly ApplicationDbContext _context;
        private readonly Microsoft.Extensions.Logging.ILogger<DocumentService> _logger;

        public DocumentService(ApplicationDbContext context, Microsoft.Extensions.Logging.ILogger<DocumentService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IEnumerable<Document>> GetDocumentsByStudentIdAsync(int studentId)
        {
            return await _context.Documents
                .Where(d => d.StudentId == studentId)
                .OrderByDescending(d => d.UploadDate)
                .ToListAsync();
        }

        public async Task<Document> GetDocumentByIdAsync(int documentId)
        {
            return await _context.Documents
                .Include(d => d.Student)
                .FirstOrDefaultAsync(d => d.DocumentId == documentId);
        }

        public async Task<(bool Success, string Message)> AddDocumentAsync(Document document)
        {
            try
            {
                _context.Documents.Add(document);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Document record created: {FileName} for Student {StudentId}", document.FileName, document.StudentId);
                return (true, "Document uploaded successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving document to database");
                return (false, "Error saving document to database: " + ex.Message);
            }
        }

        public async Task<(bool Success, string Message, string FileName)> UploadDocumentAsync(Microsoft.AspNetCore.Http.IFormFile file, string uploadsFolder, int studentId)
        {
            if (file == null || file.Length == 0)
                return (false, "File is empty", null);

            var allowedExtensions = new[] { ".pdf", ".docx", ".zip", ".png", ".jpg" };
            var extension = System.IO.Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(extension))
            {
                _logger.LogWarning("Blocked upload of invalid file type: {Extension} from Student {StudentId}", extension, studentId);
                return (false, "File type not allowed. Supported: PDF, DOCX, ZIP, PNG, JPG.", null);
            }

            if (file.Length > 10 * 1024 * 1024)
            {
                _logger.LogWarning("Blocked upload of oversized file: {Size} bytes from Student {StudentId}", file.Length, studentId);
                return (false, "File is too large. Maximum size allowed is 10MB.", null);
            }

            try
            {
                if (!System.IO.Directory.Exists(uploadsFolder))
                    System.IO.Directory.CreateDirectory(uploadsFolder);

                var uniqueFileName = Guid.NewGuid().ToString() + extension;
                var filePath = System.IO.Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new System.IO.FileStream(filePath, System.IO.FileMode.Create))
                {
                    await file.CopyToAsync(fileStream);
                }

                _logger.LogInformation("File uploaded successfully: {UniqueName} for Student {StudentId}", uniqueFileName, studentId);
                return (true, "Upload successful", uniqueFileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Physical file upload failed for Student {StudentId}", studentId);
                return (false, "Internal server error during file upload", null);
            }
        }

        public async Task<(bool Success, string Message)> DeleteDocumentAsync(int documentId, int studentId)
        {
            var doc = await _context.Documents.FirstOrDefaultAsync(d => d.DocumentId == documentId && d.StudentId == studentId);
            if (doc != null)
            {
                try
                {
                    _context.Documents.Remove(doc);
                    await _context.SaveChangesAsync();
                    return (true, "Document deleted.");
                }
                catch (Exception ex)
                {
                    return (false, "Delete error: " + ex.Message);
                }
            }
            return (false, "Document not found or unauthorized.");
        }
    }
}
