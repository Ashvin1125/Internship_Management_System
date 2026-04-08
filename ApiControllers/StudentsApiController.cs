using System.Linq;
using Microsoft.AspNetCore.Mvc;
using InternshipManagementSystem.Services;
using InternshipManagementSystem.Models;
using InternshipManagementSystem.DTOs;
using Microsoft.AspNetCore.Authorization;

namespace InternshipManagementSystem.ApiControllers
{
    [ApiController]
    [Route("api/[controller]")]
    // [Authorize] Defaults to using existing Cookie auth as discussed
    public class StudentsApiController : ControllerBase
    {
        private readonly IStudentService _studentService;

        public StudentsApiController(IStudentService studentService)
        {
            _studentService = studentService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllStudents([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10, [FromQuery] string search = null)
        {
            var allStudents = await _studentService.GetAllStudentsAsync();
            var query = allStudents.AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(s => 
                    (s.User != null && s.User.Name != null && s.User.Name.Contains(search, System.StringComparison.OrdinalIgnoreCase)) ||
                    (s.EnrollmentNumber != null && s.EnrollmentNumber.Contains(search, System.StringComparison.OrdinalIgnoreCase)) ||
                    (s.Department != null && s.Department.Contains(search, System.StringComparison.OrdinalIgnoreCase))
                );
            }

            var totalRecords = query.Count();

            var students = query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(s => new StudentDto
                {
                    StudentId = s.StudentId,
                    UserId = s.UserId,
                    EnrollmentNumber = s.EnrollmentNumber,
                    Department = s.Department,
                    Semester = s.Semester,
                    Name = s.User != null ? s.User.Name : null,
                    Email = s.User != null ? s.User.Email : null
                }).ToList();

            var result = new 
            {
                Data = students,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalRecords = totalRecords
            };

            return Ok(ApiResponse<object>.Ok(result));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetStudentById(int id)
        {
            var s = await _studentService.GetStudentByIdAsync(id);
            if (s == null) return NotFound(ApiResponse<object>.Error("Student not found"));

            var dto = new StudentDto
            {
                StudentId = s.StudentId,
                UserId = s.UserId,
                EnrollmentNumber = s.EnrollmentNumber,
                Department = s.Department,
                Semester = s.Semester,
                Name = s.User?.Name,
                Email = s.User?.Email
            };

            return Ok(ApiResponse<object>.Ok(dto));
        }
    }
}
