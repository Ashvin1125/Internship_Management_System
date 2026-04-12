using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace InternshipManagementSystem.Helpers
{
    public class CustomUserIdProvider : IUserIdProvider
    {
        public virtual string? GetUserId(HubConnectionContext connection)
        {
            // Use the "UserId" claim as the SignalR user identifier
            return connection.User?.FindFirst("UserId")?.Value;
        }
    }
}
