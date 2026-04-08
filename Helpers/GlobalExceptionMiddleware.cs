using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using InternshipManagementSystem.Models;
using System;

namespace InternshipManagementSystem.Helpers
{
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;

        public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unhandled exception has occurred.");

                var path = context.Request.Path.Value;

                if (path != null && path.StartsWith("/api/"))
                {
                    // Handle API Exception
                    context.Response.ContentType = "application/json";
                    context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

                    var response = ApiResponse<object>.Error("An internal server error occurred.", ex.Message);
                    var json = JsonSerializer.Serialize(response);

                    await context.Response.WriteAsync(json);
                }
                else
                {
                    // Handle MVC Exception - let standard MVC error handler take it or redirect depending on environment
                    throw; 
                }
            }
        }
    }
}
