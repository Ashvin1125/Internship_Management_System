using System.Linq;
using Microsoft.AspNetCore.Mvc;
using InternshipManagementSystem.Services;
using InternshipManagementSystem.Models;
using InternshipManagementSystem.DTOs;

namespace InternshipManagementSystem.ApiControllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DocumentsApiController : ControllerBase
    {
        private readonly IDocumentService _documentService;
        private readonly Microsoft.AspNetCore.Hosting.IWebHostEnvironment _hostingEnvironment;

        public DocumentsApiController(IDocumentService documentService, Microsoft.AspNetCore.Hosting.IWebHostEnvironment hostingEnvironment)
        {
            _documentService = documentService;
            _hostingEnvironment = hostingEnvironment;
        }

        [HttpGet("student/{studentId}")]
        public async Task<IActionResult> GetDocumentsByStudent(int studentId)
        {
            var documents = await _documentService.GetDocumentsByStudentIdAsync(studentId);
            var docs = documents.Select(d => new DocumentDto
            {
                DocumentId = d.DocumentId,
                StudentId = d.StudentId,
                FileName = d.FileName,
                FilePath = d.FilePath,
                UploadDate = d.UploadDate
            }).ToList();

            return Ok(ApiResponse<object>.Ok(docs));
        }

        [HttpPost]
        public async Task<IActionResult> UploadDocument(Microsoft.AspNetCore.Http.IFormFile file, [FromForm] int studentId)
        {
            if (studentId <= 0) return BadRequest(ApiResponse<object>.Error("Valid StudentId is required"));
            
            var uploadsFolder = System.IO.Path.Combine(_hostingEnvironment.WebRootPath, "uploads");
            var uploadResult = await _documentService.UploadDocumentAsync(file, uploadsFolder, studentId);
            
            if (!uploadResult.Success)
                return BadRequest(ApiResponse<object>.Error(uploadResult.Message));

            var document = new Document
            {
                StudentId = studentId,
                FileName = file.FileName,
                FilePath = uploadResult.FileName,
                UploadDate = System.DateTime.Now
            };

            var dbResult = await _documentService.AddDocumentAsync(document);
            if (dbResult.Success) 
                return Ok(ApiResponse<object>.Ok(null, "Document uploaded and secured successfully."));
                
            return BadRequest(ApiResponse<object>.Error(dbResult.Message));
        }
    }
}
