using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using Parkly_Backend.Configuration;
using Parkly_Backend.Interfaces;
using System;
using System.Threading.Tasks;

namespace Parkly_Backend.Services
{
    public class EmailService : IEmailService
    {
        private readonly ILogger<EmailService> _logger;
        private readonly SmtpOptions _smtpOptions;

        public EmailService(ILogger<EmailService> logger, IOptions<SmtpOptions> smtpOptions)
        {
            _logger = logger;
            _smtpOptions = smtpOptions.Value;
        }

        public async Task SendEmailAsync(string to, string subject, string body)
        {
            var host = _smtpOptions.Host;
            var port = _smtpOptions.Port;
            var email = _smtpOptions.Email;
            var password = _smtpOptions.Password;

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