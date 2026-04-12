using Microsoft.AspNetCore.Mvc;
using InternshipManagementSystem.Data;
using Microsoft.EntityFrameworkCore;

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
            try
            {
                var canConnect = await _context.Database.CanConnectAsync();
                if (canConnect)
                    return Ok(new { status = "Healthy", database = "Connected", timestamp = DateTime.UtcNow });

                return StatusCode(503, new { status = "Unhealthy", database = "Disconnected", timestamp = DateTime.UtcNow });
            }
            catch (Exception ex)
            {
                return StatusCode(503, new { status = "Unhealthy", database = "Error: " + ex.Message, timestamp = DateTime.UtcNow });
            }
        }
    }
}
