using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InternshipManagementSystem.Data;
using InternshipManagementSystem.Hubs;
using InternshipManagementSystem.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace InternshipManagementSystem.Services
{
    public class NotificationService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHubContext<ChatHub> _hubContext;

        public NotificationService(ApplicationDbContext context, IHubContext<ChatHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        public async Task CreateNotificationAsync(int userId, string message, string type)
        {
            var notification = new Notification
            {
                UserId = userId,
                Message = message,
                Type = type,
                IsRead = false
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            // Broadcast real-time notification to all sessions of the user
            await _hubContext.Clients.User(userId.ToString()).SendAsync("ReceiveNotification", message);
        }

        public async Task<List<Notification>> GetUserNotificationsAsync(int userId, int limit = 10)
        {
            return await _context.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Take(limit)
                .ToListAsync();
        }

        public async Task<int> GetUnreadCountAsync(int userId)
        {
            return await _context.Notifications
                .CountAsync(n => n.UserId == userId && !n.IsRead);
        }

        public async Task<(List<Notification> Notifications, int UnreadCount)> GetNotificationSummaryAsync(int userId, int limit = 10)
        {
            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Take(limit)
                .ToListAsync();

            var unreadCount = await _context.Notifications
                .CountAsync(n => n.UserId == userId && !n.IsRead);

            return (notifications, unreadCount);
        }

        public async Task MarkAsReadAsync(int notificationId)
        {
            var notification = await _context.Notifications.FindAsync(notificationId);
            if (notification != null)
            {
                notification.IsRead = true;
                await _context.SaveChangesAsync();
            }
        }

        public async Task MarkAllAsReadAsync(int userId)
        {
            var unread = await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync();

            if (unread.Any())
            {
                foreach (var n in unread)
                {
                    n.IsRead = true;
                }
                await _context.SaveChangesAsync();
            }
        }
    }
}
