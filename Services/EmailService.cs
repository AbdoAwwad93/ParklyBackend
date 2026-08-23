using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using Google.Apis.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using Parkly_Backend.Configuration;
using Parkly_Backend.Interfaces;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Parkly_Backend.Services
{
    public class EmailService : IEmailService
    {
        private readonly ILogger<EmailService> _logger;
        private readonly GmailOptions _gmailOptions;

        public EmailService(ILogger<EmailService> logger, IOptions<GmailOptions> gmailOptions)
        {
            _logger = logger;
            _gmailOptions = gmailOptions.Value;
        }

        public async Task SendEmailAsync(string to, string subject, string body)
        {
            if (string.IsNullOrEmpty(_gmailOptions.ClientId) || 
                string.IsNullOrEmpty(_gmailOptions.ClientSecret) || 
                string.IsNullOrEmpty(_gmailOptions.RefreshToken) || 
                string.IsNullOrEmpty(_gmailOptions.SenderEmail))
            {
                throw new InvalidOperationException("Gmail API settings are not fully configured.");
            }

            var tokenResponse = new TokenResponse
            {
                RefreshToken = _gmailOptions.RefreshToken
            };

            var userCredential = new UserCredential(
                new GoogleAuthorizationCodeFlow(
                    new GoogleAuthorizationCodeFlow.Initializer
                    {
                        ClientSecrets = new ClientSecrets
                        {
                            ClientId = _gmailOptions.ClientId,
                            ClientSecret = _gmailOptions.ClientSecret
                        }
                    }),
                _gmailOptions.SenderEmail,
                tokenResponse);

            var service = new GmailService(new BaseClientService.Initializer
            {
                HttpClientInitializer = userCredential,
                ApplicationName = "Parkly Backend"
            });

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("Parkly", _gmailOptions.SenderEmail));
            message.To.Add(new MailboxAddress("", to));
            message.Subject = subject;
            message.Body = new TextPart(MimeKit.Text.TextFormat.Html) { Text = body };

            using var memoryStream = new MemoryStream();
            await message.WriteToAsync(memoryStream);
            var base64UrlString = Convert.ToBase64String(memoryStream.ToArray())
                .Replace('+', '-')
                .Replace('/', '_')
                .Replace("=", "");

            var gmailMessage = new Message { Raw = base64UrlString };
            
            try
            {
                await service.Users.Messages.Send(gmailMessage, "me").ExecuteAsync();
                _logger.LogInformation("Email sent successfully to {To} via Gmail API.", to);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {To} via Gmail API.", to);
                throw;
            }
        }
    }
}