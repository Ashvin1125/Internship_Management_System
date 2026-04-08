using System.Linq;
using Microsoft.AspNetCore.Mvc;
using InternshipManagementSystem.Services;
using InternshipManagementSystem.Models;
using InternshipManagementSystem.DTOs;
using System.ComponentModel.DataAnnotations;

namespace InternshipManagementSystem.ApiControllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TasksApiController : ControllerBase
    {
        private readonly ITaskService _taskService;

        public TasksApiController(ITaskService taskService)
        {
            _taskService = taskService;
        }

        [HttpGet("student/{studentId}")]
        public async Task<IActionResult> GetTasksByStudent(int studentId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10, [FromQuery] string search = null)
        {
            var studentTasks = await _taskService.GetTasksByStudentIdAsync(studentId);
            var query = studentTasks.AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(t => 
                    (t.Title != null && t.Title.Contains(search, System.StringComparison.OrdinalIgnoreCase)) ||
                    (t.Description != null && t.Description.Contains(search, System.StringComparison.OrdinalIgnoreCase)) ||
                    (t.Status != null && t.Status.Contains(search, System.StringComparison.OrdinalIgnoreCase))
                );
            }

            var totalRecords = query.Count();

            var tasks = query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(t => new TaskDto
                {
                    TaskId = t.TaskId,
                    StudentId = t.StudentId,
                    GuideId = t.GuideId,
                    Title = t.Title,
                    Description = t.Description,
                    Deadline = t.Deadline,
                    Status = t.Status
                }).ToList();

            var result = new 
            {
                Data = tasks,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalRecords = totalRecords
            };

            return Ok(ApiResponse<object>.Ok(result));
        }

        [HttpPut("{taskId}")]
        public async Task<IActionResult> UpdateTaskStatus(int taskId, [FromBody] UpdateTaskDto dto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<object>.Error("Validation failed", string.Join(" | ", errors)));
            }

            var result = await _taskService.UpdateTaskStatusAsync(taskId, dto.StudentId, dto.Status);
            if (result.Success) return Ok(ApiResponse<object>.Ok(null, result.Message));
            return BadRequest(ApiResponse<object>.Error(result.Message));
        }
    }

    public class UpdateTaskDto
    {
        [Required(ErrorMessage = "StudentId is required.")]
        public int StudentId { get; set; }
        
        [Required(ErrorMessage = "Status is required.")]
        [RegularExpression("^(Pending|In Progress|Completed)$", ErrorMessage = "Invalid status value.")]
        public string Status { get; set; }
    }
}
