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
    public class GuidesApiController : ControllerBase
    {
        private readonly IGuideService _guideService;

        public GuidesApiController(IGuideService guideService)
        {
            _guideService = guideService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllGuides()
        {
            var allGuides = await _guideService.GetAllGuidesAsync();
            var guides = allGuides.Select(g => new GuideDto
            {
                GuideId = g.GuideId,
                UserId = g.UserId,
                Department = g.Department,
                Designation = g.Designation,
                Name = g.User != null ? g.User.Name : null,
                Email = g.User != null ? g.User.Email : null
            }).ToList();

            return Ok(ApiResponse<object>.Ok(guides));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetGuideById(int id)
        {
            var g = await _guideService.GetGuideByIdAsync(id);
            if (g == null) return NotFound(ApiResponse<object>.Error("Guide not found"));

            var dto = new GuideDto
            {
                GuideId = g.GuideId,
                UserId = g.UserId,
                Department = g.Department,
                Designation = g.Designation,
                Name = g.User != null ? g.User.Name : null,
                Email = g.User != null ? g.User.Email : null
            };

            return Ok(ApiResponse<object>.Ok(dto));
        }
    }
}
