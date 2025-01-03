using System.Net.Mail;
using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration; // For accessing configuration

namespace Cold_Storage_GO.Services
{
    
    public class EmailService
    {
        private readonly string _smtpHost;
        private readonly int _smtpPort;
        private readonly string _smtpUser;
        private readonly string _smtpPass;
        private readonly ILogger<EmailService> _logger;

        // Constructor accepts an IConfiguration instance for accessing settings from appsettings.json
        public EmailService(ILogger<EmailService> logger, IConfiguration configuration)
        {
            _logger = logger;
            _smtpHost = configuration["EmailSettings:SmtpHost"] ?? "smtp.gmail.com"; // Default to Gmail if not set
            _smtpPort = int.TryParse(configuration["EmailSettings:SmtpPort"], out var port) ? port : 587; // Default to 587
            _smtpUser = configuration["EmailSettings:SmtpUser"] ?? Environment.GetEnvironmentVariable("SMTP_USER");
            _smtpPass = configuration["EmailSettings:SmtpPass"] ?? Environment.GetEnvironmentVariable("SMTP_PASS");
        }

        public async Task<bool> SendEmailAsync(string toEmail, string subject, string body)
        {
            try
            {
                var mailMessage = new MailMessage
                {
                    From = new MailAddress(_smtpUser),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                };
                mailMessage.To.Add(toEmail);

                using var smtpClient = new SmtpClient(_smtpHost, _smtpPort)
                {
                    Credentials = new NetworkCredential(_smtpUser, _smtpPass),
                    EnableSsl = true
                };
                await smtpClient.SendMailAsync(mailMessage);
                return true;
            }
            catch (Exception ex)
            {
                // Log the error with a more descriptive message
                _logger.LogError(ex, "Failed to send email to {EmailAddress} with subject {Subject}", toEmail, subject);
                return false;
            }
        }
    }
}
