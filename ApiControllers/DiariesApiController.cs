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
    public class DiariesApiController : ControllerBase
    {
        private readonly IDailyDiaryService _diaryService;

        public DiariesApiController(IDailyDiaryService diaryService)
        {
            _diaryService = diaryService;
        }

        [HttpGet("student/{studentId}")]
        public async Task<IActionResult> GetDiariesByStudent(int studentId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10, [FromQuery] string search = null)
        {
            var studentDiaries = await _diaryService.GetDiariesByStudentIdAsync(studentId);
            var query = studentDiaries.AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(d => 
                    (d.WorkDescription != null && d.WorkDescription.Contains(search, System.StringComparison.OrdinalIgnoreCase)) ||
                    (d.GuideComment != null && d.GuideComment.Contains(search, System.StringComparison.OrdinalIgnoreCase)) ||
                    (d.Status != null && d.Status.Contains(search, System.StringComparison.OrdinalIgnoreCase))
                );
            }

            var totalRecords = query.Count();

            var diaries = query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(d => new DailyDiaryDto
                {
                    DiaryId = d.DiaryId,
                    StudentId = d.StudentId,
                    WorkDescription = d.WorkDescription,
                    WorkDate = d.WorkDate,
                    Status = d.Status,
                    GuideComment = d.GuideComment
                }).ToList();

            var result = new 
            {
                Data = diaries,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalRecords = totalRecords
            };

            return Ok(ApiResponse<object>.Ok(result));
        }

        [HttpPost]
        public async Task<IActionResult> SubmitDiary([FromBody] DailyDiary diary)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<object>.Error("Validation failed", string.Join(" | ", errors)));
            }
            
            var result = await _diaryService.CreateDiaryAsync(diary);
            if (result.Success)
            {
                return Ok(ApiResponse<object>.Ok(null, result.Message));
            }
            return BadRequest(ApiResponse<object>.Error(result.Message));
        }
    }
}
