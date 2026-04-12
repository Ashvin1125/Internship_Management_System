using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InternshipManagementSystem.Data;
using InternshipManagementSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace InternshipManagementSystem.Services
{
    public class MessageService
    {
        private readonly ApplicationDbContext _context;

        public MessageService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<(bool Success, string Message, Message? Data)> SendMessageAsync(int senderUserId, int receiverUserId, string content, string sid)
        {
            // 1. Validate relationship
            var isValid = await ValidateRelationshipAsync(senderUserId, receiverUserId);
            if (!isValid)
            {
                return (false, "You are not authorized to message this user.", null);
            }

            // 2. Create message
            var message = new Message
            {
                SenderId = senderUserId,
                ReceiverId = receiverUserId,
                Content = content,
                Sid = sid,
                IsRead = false
            };

            _context.Messages.Add(message);
            await _context.SaveChangesAsync();

            return (true, "Message sent.", message);
        }

        public async Task<List<Message>> GetConversationAsync(int currentUserId, int partnerUserId, string sid)
        {
            // Validate relationship
            var isValid = await ValidateRelationshipAsync(currentUserId, partnerUserId);
            if (!isValid) return new List<Message>();

            // Query filtering:
            // 1. Participant check (standard)
            // 2. Hide if deleted by the current user
            // 3. Show "This message was deleted" content if IsDeletedForEveryone is true
            var messages = await _context.Messages
                .Where(m => ((m.SenderId == currentUserId && m.ReceiverId == partnerUserId && !m.IsDeletedBySender) || 
                            (m.SenderId == partnerUserId && m.ReceiverId == currentUserId && !m.IsDeletedByReceiver)))
                .OrderBy(m => m.CreatedAt)
                .ToListAsync();

            // Handle "Delete for Everyone" content replacement
            foreach (var m in messages)
            {
                if (m.IsDeletedForEveryone)
                {
                    m.Content = "This message was deleted";
                }
            }

            return messages;
        }

        public async Task<(bool Success, string Message)> DeleteMessageAsync(int messageId, int userId, bool deleteForEveryone)
        {
            var message = await _context.Messages.FindAsync(messageId);
            if (message == null) return (false, "Message not found.");

            // 1. Security check: Only participants can delete
            if (message.SenderId != userId && message.ReceiverId != userId)
                return (false, "Unauthorized.");

            if (deleteForEveryone)
            {
                // 2. Only sender can delete for everyone
                if (message.SenderId != userId)
                    return (false, "Only the sender can delete a message for everyone.");

                message.IsDeletedForEveryone = true;
                message.Content = "This message was deleted"; // Also persist text change
            }
            else
            {
                // 3. Delete for me (soft delete)
                if (message.SenderId == userId)
                    message.IsDeletedBySender = true;
                else
                    message.IsDeletedByReceiver = true;
            }

            await _context.SaveChangesAsync();
            return (true, "Message deleted successfully.");
        }

        public async Task MarkAsReadAsync(int currentUserId, int partnerUserId, string sid)
        {
            var unreadMessages = await _context.Messages
                .Where(m => m.SenderId == partnerUserId && m.ReceiverId == currentUserId && !m.IsRead)
                .ToListAsync();

            if (unreadMessages.Any())
            {
                foreach (var msg in unreadMessages)
                {
                    msg.IsRead = true;
                }
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> ValidateRelationshipAsync(int userId1, int userId2)
        {
            var user1 = await _context.Users.FindAsync(userId1);
            var user2 = await _context.Users.FindAsync(userId2);

            if (user1 == null || user2 == null) return false;

            if (user1.Role == "Admin" || user2.Role == "Admin") return true;

            if (user1.Role == "Student" && user2.Role == "Guide")
            {
                var student = await _context.Students.FirstOrDefaultAsync(s => s.UserId == userId1);
                var guide = await _context.Guides.FirstOrDefaultAsync(g => g.UserId == userId2);
                if (student == null || guide == null) return false;

                return await _context.GuideAssignments.AnyAsync(ga => ga.StudentId == student.StudentId && ga.GuideId == guide.GuideId);
            }

            if (user1.Role == "Guide" && user2.Role == "Student")
            {
                var guide = await _context.Guides.FirstOrDefaultAsync(g => g.UserId == userId1);
                var student = await _context.Students.FirstOrDefaultAsync(s => s.UserId == userId2);
                if (guide == null || student == null) return false;

                return await _context.GuideAssignments.AnyAsync(ga => ga.GuideId == guide.GuideId && ga.StudentId == student.StudentId);
            }

            return false;
        }

        public async Task<List<User>> GetChatPartnersAsync(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return new List<User>();

            if (user.Role == "Student")
            {
                var student = await _context.Students.FirstOrDefaultAsync(s => s.UserId == userId);
                if (student == null) return new List<User>();

                return await _context.GuideAssignments
                    .Where(ga => ga.StudentId == student.StudentId)
                    .Select(ga => ga.Guide.User)
                    .ToListAsync();
            }

            if (user.Role == "Guide")
            {
                var guide = await _context.Guides.FirstOrDefaultAsync(g => g.UserId == userId);
                if (guide == null) return new List<User>();

                return await _context.GuideAssignments
                    .Where(ga => ga.GuideId == guide.GuideId)
                    .Select(ga => ga.Student.User)
                    .ToListAsync();
            }

            if (user.Role == "Admin")
            {
                return await _context.Users.Where(u => u.UserId != userId).ToListAsync();
            }

            return new List<User>();
        }

        public async Task<(bool Success, string Message, List<Message> Data)> BroadcastMessageAsync(int adminUserId, string content, string sid)
        {
            var admin = await _context.Users.FindAsync(adminUserId);
            if (admin == null || admin.Role != "Admin")
                return (false, "Only admins can broadcast messages.", new List<Message>());

            var allOtherUsers = await _context.Users
                .Where(u => u.UserId != adminUserId)
                .ToListAsync();

            var messages = new List<Message>();
            foreach (var user in allOtherUsers)
            {
                messages.Add(new Message
                {
                    SenderId = adminUserId,
                    ReceiverId = user.UserId,
                    Content = content,
                    Sid = sid,
                    IsRead = false
                });
            }

            _context.Messages.AddRange(messages);
            await _context.SaveChangesAsync();

            return (true, $"Broadcast sent to {messages.Count} users.", messages);
        }
    }
}
