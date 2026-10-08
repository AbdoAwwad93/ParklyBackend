using FirebaseAdmin.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Parkly_Backend.Data.Repositories;
using Parkly_Backend.Interfaces;

namespace Parkly_Backend.Services
{
    /// <summary>
    /// Sends push notifications via Firebase Cloud Messaging.
    /// Best-effort: logs errors but never throws to avoid blocking the caller.
    /// Automatically cleans up stale/unregistered tokens.
    /// </summary>
    public class FcmPushService : IFcmPushService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<FcmPushService> _logger;

        public FcmPushService(IServiceScopeFactory scopeFactory, ILogger<FcmPushService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        public async Task SendToUserAsync(Guid userId, string title, string body,
            Dictionary<string, string>? data = null)
        {
            try
            {
                if (FirebaseAdmin.FirebaseApp.DefaultInstance == null)
                {
                    _logger.LogWarning("Firebase is not initialized. Skipping push notification for user {UserId}.", userId);
                    return;
                }

                List<string> tokens;
                using (var scope = _scopeFactory.CreateScope())
                {
                    var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                    tokens = await unitOfWork.UserFcmTokens.GetTokensByUserIdAsync(userId);
                }

                if (tokens.Count == 0)
                {
                    _logger.LogDebug("No FCM tokens found for user {UserId}. Skipping push.", userId);
                    return;
                }

                var messages = tokens.Select(token => new Message
                {
                    Token = token,
                    Notification = new Notification
                    {
                        Title = title,
                        Body = body
                    },
                    Data = data?.Where(kv => !string.IsNullOrEmpty(kv.Value))
                            .ToDictionary(kv => kv.Key, kv => kv.Value),
                    Android = new AndroidConfig
                    {
                        Priority = Priority.High,
                        Notification = new AndroidNotification
                        {
                            Sound = "default",
                            ClickAction = "FLUTTER_NOTIFICATION_CLICK"
                        }
                    },
                    Apns = new ApnsConfig
                    {
                        Aps = new Aps
                        {
                            Sound = "default",
                            ContentAvailable = true
                        }
                    }
                }).ToList();

                var response = await FirebaseMessaging.DefaultInstance.SendEachAsync(messages);

                if (response.FailureCount > 0)
                {
                    var staleTokens = new List<string>();
                    for (int i = 0; i < response.Responses.Count; i++)
                    {
                        if (!response.Responses[i].IsSuccess)
                        {
                            var error = response.Responses[i].Exception?.MessagingErrorCode;
                            if (error == MessagingErrorCode.Unregistered ||
                                error == MessagingErrorCode.InvalidArgument)
                            {
                                _logger.LogInformation(
                                    "Removing stale FCM token for user {UserId}: {ErrorCode}",
                                    userId, error);
                                staleTokens.Add(tokens[i]);
                            }
                            else
                            {
                                _logger.LogWarning(
                                    "FCM send failed for user {UserId}, token index {Index}: {Error}",
                                    userId, i, response.Responses[i].Exception?.Message);
                            }
                        }
                    }

                    if (staleTokens.Count > 0)
                    {
                        using var scope = _scopeFactory.CreateScope();
                        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                        foreach (var staleToken in staleTokens)
                        {
                            await unitOfWork.UserFcmTokens.DeleteByTokenAsync(userId, staleToken);
                        }
                    }
                }

                _logger.LogDebug(
                    "FCM push sent to user {UserId}: {SuccessCount} success, {FailureCount} failed out of {Total}.",
                    userId, response.SuccessCount, response.FailureCount, tokens.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error sending FCM push to user {UserId}.", userId);
            }
        }
    }
}
