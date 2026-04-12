using System;
using System.Threading.Tasks;
using InternshipManagementSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using InternshipManagementSystem.Data;

namespace InternshipManagementSystem.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly MessageService _messageService;
        private readonly NotificationService _notificationService;
        private readonly ApplicationDbContext _context;

        public ChatHub(MessageService messageService, NotificationService notificationService, ApplicationDbContext context)
        {
            _messageService = messageService;
            _notificationService = notificationService;
            _context = context;
        }

        public async Task SendMessage(int receiverUserId, string content, string sid)
        {
            var senderUserIdStr = Context.UserIdentifier;
            if (string.IsNullOrEmpty(senderUserIdStr) || !int.TryParse(senderUserIdStr, out int senderUserId)) return;

            var result = await _messageService.SendMessageAsync(senderUserId, receiverUserId, content, sid);

            if (result.Success && result.Data != null)
            {
                var timestamp = result.Data.CreatedAt.ToString("HH:mm");
                var messageId = result.Data.Id;

                // Broadcast to receiver (all sessions)
                await Clients.User(receiverUserId.ToString()).SendAsync("ReceiveMessage", senderUserId, receiverUserId, content, timestamp, messageId);

                // Broadcast to sender (all tabs)
                await Clients.User(senderUserId.ToString()).SendAsync("ReceiveMessage", senderUserId, receiverUserId, content, timestamp, messageId);

                var senderName = Context.User?.FindFirst(ClaimTypes.Name)?.Value ?? "Someone";
                await _notificationService.CreateNotificationAsync(receiverUserId, $"New message from {senderName}", "Chat");
            }
            else
            {
                await Clients.Caller.SendAsync("Error", result.Message);
            }
        }

        public async Task DeleteMessage(int messageId, bool deleteForEveryone)
        {
            var userIdStr = Context.UserIdentifier;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId)) return;

            // Get message info before deletion for broadcasting
            var message = await _context.Messages.FindAsync(messageId);
            if (message == null) return;

            var partnerId = message.SenderId == userId ? message.ReceiverId : message.SenderId;

            var result = await _messageService.DeleteMessageAsync(messageId, userId, deleteForEveryone);

            if (result.Success)
            {
                if (deleteForEveryone)
                {
                    // Update both participants
                    await Clients.User(userId.ToString()).SendAsync("MessageDeleted", messageId, true);
                    await Clients.User(partnerId.ToString()).SendAsync("MessageDeleted", messageId, true);
                }
                else
                {
                    // Only update the user who deleted "for me"
                    await Clients.User(userId.ToString()).SendAsync("MessageDeleted", messageId, false);
                }
            }
            else
            {
                await Clients.Caller.SendAsync("Error", result.Message);
            }
        }

        public async Task SendBroadcast(string content, string sid)
        {
            var userIdStr = Context.UserIdentifier;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId)) return;

            var result = await _messageService.BroadcastMessageAsync(userId, content, sid);

            if (result.Success)
            {
                var timestamp = DateTime.UtcNow.ToString("HH:mm");
                // Notify all users in real-time
                await Clients.All.SendAsync("ReceiveBroadcast", userId, content, timestamp);
            }
            else
            {
                await Clients.Caller.SendAsync("Error", result.Message);
            }
        }
    }
}
