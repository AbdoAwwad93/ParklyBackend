using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using MimeKit;
using Parkly_Backend.Services.Interfaces;
using System;
using System.Threading.Tasks;

namespace Parkly_Backend.Services.Implemention
{
    public class EmailService : IEmailService
    {
        private readonly ILogger<EmailService> _logger;

        public EmailService(ILogger<EmailService> logger)
        {
            _logger = logger;
        }

        public async Task SendEmailAsync(string to, string subject, string body)
        {
            var host = Environment.GetEnvironmentVariable("SmtpHost");
            var port = int.TryParse(Environment.GetEnvironmentVariable("SmtpPort"), out var p) ? p : 587;
            var email = Environment.GetEnvironmentVariable("Email");
            var password = Environment.GetEnvironmentVariable("EmailPassword");

            if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                throw new InvalidOperationException("SMTP settings (SmtpHost, Email, EmailPassword) are not configured.");
            }

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("Parkly", email));
            message.To.Add(new MailboxAddress("", to));
            message.Subject = subject;
            message.Body = new TextPart(MimeKit.Text.TextFormat.Html) { Text = body };

            using var client = new SmtpClient();
            await client.ConnectAsync(host, port, SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(email, password);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }
    }
}