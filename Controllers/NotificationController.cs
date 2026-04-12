using System.Threading.Tasks;
using InternshipManagementSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InternshipManagementSystem.Controllers
{
    [Authorize]
    public class NotificationController : Controller
    {
        private readonly NotificationService _notificationService;

        public NotificationController(NotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        [HttpGet]
        public async Task<IActionResult> GetLatest()
        {
            var userIdStr = User.FindFirst("UserId")?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
                return Json(new { success = false });

            var summary = await _notificationService.GetNotificationSummaryAsync(userId);
            return Json(new { success = true, notifications = summary.Notifications, unreadCount = summary.UnreadCount });
        }

        [HttpPost]
        public async Task<IActionResult> MarkRead(int id)
        {
            await _notificationService.MarkAsReadAsync(id);
            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> MarkAllRead()
        {
            var userIdStr = User.FindFirst("UserId")?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
                return Json(new { success = false });

            await _notificationService.MarkAllAsReadAsync(userId);
            return Json(new { success = true });
        }
    }
}
