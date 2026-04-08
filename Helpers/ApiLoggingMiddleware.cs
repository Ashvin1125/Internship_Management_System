using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Threading.Tasks;

namespace InternshipManagementSystem.Helpers
{
    public class ApiLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ApiLoggingMiddleware> _logger;

        public ApiLoggingMiddleware(RequestDelegate next, ILogger<ApiLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task Invoke(HttpContext context)
        {
            var path = context.Request.Path.Value;

            if (path != null && path.StartsWith("/api/"))
            {
                var sw = Stopwatch.StartNew();
                _logger.LogInformation("Receiving API Request: {Method} {Path}", context.Request.Method, path);
                
                await _next(context);
                
                sw.Stop();
                _logger.LogInformation("Completed API Request: {Method} {Path} with status {StatusCode} in {ElapsedMilliseconds}ms", 
                    context.Request.Method, path, context.Response.StatusCode, sw.ElapsedMilliseconds);
            }
            else
            {
                await _next(context);
            }
        }
    }
}
