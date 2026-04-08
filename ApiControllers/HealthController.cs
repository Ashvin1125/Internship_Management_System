using Microsoft.AspNetCore.Mvc;
using InternshipManagementSystem.Data;
using Microsoft.EntityFrameworkCore;
using InternshipManagementSystem.Models;

namespace InternshipManagementSystem.ApiControllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HealthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public HealthController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetHealth()
        {
            var healthStatus = new HealthStatus
            {
                Status = "Healthy",
                Timestamp = System.DateTime.UtcNow,
                Database = "Disconnected"
            };

            try
            {
                if (await _context.Database.CanConnectAsync())
                {
                    healthStatus.Database = "Connected";
                }
                else
                {
                    healthStatus.Status = "Unhealthy";
                    return StatusCode(503, ApiResponse<object>.Ok(healthStatus, "Database connection failed."));
                }
            }
            catch (System.Exception ex)
            {
                healthStatus.Status = "Unhealthy";
                healthStatus.Database = "Error: " + ex.Message;
                return StatusCode(503, ApiResponse<object>.Ok(healthStatus, "Health check failed."));
            }

            return Ok(ApiResponse<object>.Ok(healthStatus, "System is running."));
        }

        private class HealthStatus
        {
            public string Status { get; set; }
            public System.DateTime Timestamp { get; set; }
            public string Database { get; set; }
        }
    }
}
