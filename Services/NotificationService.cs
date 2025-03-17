using System;
using System.Threading.Tasks;
using Cold_Storage_GO.Models;
using Microsoft.EntityFrameworkCore;

namespace Cold_Storage_GO.Services
{
    public class NotificationService
    {
        private readonly DbContexts _context;
        private readonly EmailService _emailService;

        public NotificationService(DbContexts context, EmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        // Function to create and save a notification
        public async Task<Notification> CreateNotification(Guid userId, string type, string title, string content)
        {
            var notification = new Notification
            {
                NotificationId = Guid.NewGuid(),
                UserId = userId,
                Type = type,
                Title = title,
                Content = content,
                IsRead = false, // Default to unread
                CreatedAt = DateTime.UtcNow
            };

            await _context.Notifications.AddAsync(notification);
            await _context.SaveChangesAsync();

            return notification;
        }

        // Function to send an email for the notification using EmailService
        public async Task SendNotificationEmail(Guid userId, string title, string content)
        {
            var userAdmin = await _context.UserAdministration
.FirstOrDefaultAsync(ua => ua.UserId == userId);

            if (userAdmin == null)
            {
                throw new Exception("User administration record not found.");
            }

            // Only proceed with sending the email if push notifications are enabled
            if (!userAdmin.PushNotifications)
            {
                // If notifications are disabled, just return without sending the email
                return;
            }
            // Retrieve user email
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
            if (user == null || string.IsNullOrEmpty(user.Email))
            {
                throw new Exception("User not found or email is not set.");
            }

            // Use EmailService to send the email
            var emailSent = await _emailService.SendEmailAsync(user.Email, title, content);

            if (!emailSent)
            {
                throw new Exception($"Failed to send email to {user.Email}");
            }
        }
    }
}
