using System;
using System.Security.Claims;
using System.Threading.Tasks;
using InternshipManagementSystem.Data;
using InternshipManagementSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InternshipManagementSystem.Controllers
{
    [Authorize]
    public class ChatController : Controller
    {
        private readonly MessageService _messageService;
        private readonly ApplicationDbContext _context;

        public ChatController(MessageService messageService, ApplicationDbContext context)
        {
            _messageService = messageService;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var userIdStr = User.FindFirst("UserId")?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
                return RedirectToAction("Login", "Account");

            var partners = await _messageService.GetChatPartnersAsync(userId);
            return View(partners);
        }

        public async Task<IActionResult> Conversation(int partnerId)
        {
            var userIdStr = User.FindFirst("UserId")?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
                return RedirectToAction("Login", "Account");

            var sid = RouteData.Values["sid"]?.ToString() ?? string.Empty;
            
            var isValid = await _messageService.ValidateRelationshipAsync(userId, partnerId);
            if (!isValid) return Unauthorized();

            await _messageService.MarkAsReadAsync(userId, partnerId, sid);
            var messages = await _messageService.GetConversationAsync(userId, partnerId, sid);
            
            var partner = await _context.Users.FindAsync(partnerId);
            ViewBag.PartnerId = partnerId;
            ViewBag.PartnerName = partner?.Name ?? "User";
            ViewBag.CurrentUserId = userId;
            ViewBag.Sid = sid;

            return View(messages);
        }

        [HttpGet]
        public async Task<IActionResult> GetMessages(int partnerId)
        {
            var userIdStr = User.FindFirst("UserId")?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
                return Json(new { success = false });

            var sid = RouteData.Values["sid"]?.ToString() ?? string.Empty;
            var messages = await _messageService.GetConversationAsync(userId, partnerId, sid);
            
            return Json(new { success = true, messages });
        }
        [HttpPost]
        public async Task<IActionResult> MarkRead(int partnerId)
        {
            var userIdStr = User.FindFirst("UserId")?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
                return Json(new { success = false });

            var sid = RouteData.Values["sid"]?.ToString() ?? string.Empty;
            await _messageService.MarkAsReadAsync(userId, partnerId, sid);
            return Json(new { success = true });
        }
    }
}
