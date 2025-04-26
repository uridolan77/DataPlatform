using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Net.Mail;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using GenericDataPlatform.Security.Models.Alerting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GenericDataPlatform.Security.Services
{
    /// <summary>
    /// Service for sending notifications
    /// </summary>
    public class NotificationService : INotificationService
    {
        private readonly SecurityOptions _options;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(
            IOptions<SecurityOptions> options,
            IHttpClientFactory httpClientFactory,
            ILogger<NotificationService> logger)
        {
            _options = options.Value;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        /// <summary>
        /// Sends a notification
        /// </summary>
        public async Task SendNotificationAsync(Notification notification)
        {
            try
            {
                _logger.LogInformation("Sending {NotificationType} notification: {Subject}", notification.Type, notification.Subject);
                
                switch (notification.Type)
                {
                    case NotificationType.Email:
                        await SendEmailNotificationAsync(notification);
                        break;
                    
                    case NotificationType.Sms:
                        await SendSmsNotificationAsync(notification);
                        break;
                    
                    case NotificationType.Webhook:
                        await SendWebhookNotificationAsync(notification);
                        break;
                    
                    case NotificationType.Slack:
                        await SendSlackNotificationAsync(notification);
                        break;
                    
                    case NotificationType.Teams:
                        await SendTeamsNotificationAsync(notification);
                        break;
                    
                    default:
                        _logger.LogWarning("Unsupported notification type: {NotificationType}", notification.Type);
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending {NotificationType} notification: {Subject}", notification.Type, notification.Subject);
                throw;
            }
        }

        /// <summary>
        /// Sends an email notification
        /// </summary>
        private async Task SendEmailNotificationAsync(Notification notification)
        {
            try
            {
                // Get email settings
                var smtpServer = _options.SmtpServer;
                var smtpPort = _options.SmtpPort;
                var smtpUsername = _options.SmtpUsername;
                var smtpPassword = _options.SmtpPassword;
                var smtpFromAddress = _options.SmtpFromAddress;
                
                if (string.IsNullOrEmpty(smtpServer) || smtpPort == 0 || string.IsNullOrEmpty(smtpFromAddress))
                {
                    _logger.LogWarning("SMTP settings not configured");
                    return;
                }
                
                // Create mail message
                using var message = new MailMessage
                {
                    From = new MailAddress(smtpFromAddress),
                    Subject = notification.Subject,
                    Body = notification.Message,
                    IsBodyHtml = false
                };
                
                // Add recipients
                foreach (var recipient in notification.Recipients)
                {
                    message.To.Add(recipient);
                }
                
                // Send email
                using var client = new SmtpClient(smtpServer, smtpPort)
                {
                    EnableSsl = true,
                    Credentials = new NetworkCredential(smtpUsername, smtpPassword)
                };
                
                await client.SendMailAsync(message);
                
                _logger.LogInformation("Sent email notification to {RecipientCount} recipients", notification.Recipients.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending email notification");
                throw;
            }
        }

        /// <summary>
        /// Sends an SMS notification
        /// </summary>
        private async Task SendSmsNotificationAsync(Notification notification)
        {
            try
            {
                // Get SMS settings
                var smsApiUrl = _options.SmsApiUrl;
                var smsApiKey = _options.SmsApiKey;
                var smsFromNumber = _options.SmsFromNumber;
                
                if (string.IsNullOrEmpty(smsApiUrl) || string.IsNullOrEmpty(smsApiKey) || string.IsNullOrEmpty(smsFromNumber))
                {
                    _logger.LogWarning("SMS settings not configured");
                    return;
                }
                
                // Create HTTP client
                var client = _httpClientFactory.CreateClient("SmsClient");
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {smsApiKey}");
                
                // Send SMS to each recipient
                foreach (var recipient in notification.Recipients)
                {
                    var requestBody = new
                    {
                        from = smsFromNumber,
                        to = recipient,
                        message = $"{notification.Subject}: {notification.Message}"
                    };
                    
                    var response = await client.PostAsJsonAsync(smsApiUrl, requestBody);
                    
                    if (!response.IsSuccessStatusCode)
                    {
                        _logger.LogWarning("Error sending SMS to {Recipient}: {StatusCode}", recipient, response.StatusCode);
                    }
                }
                
                _logger.LogInformation("Sent SMS notification to {RecipientCount} recipients", notification.Recipients.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending SMS notification");
                throw;
            }
        }

        /// <summary>
        /// Sends a webhook notification
        /// </summary>
        private async Task SendWebhookNotificationAsync(Notification notification)
        {
            try
            {
                // Get webhook URL from the first recipient
                if (notification.Recipients.Count == 0)
                {
                    _logger.LogWarning("No webhook URL provided");
                    return;
                }
                
                var webhookUrl = notification.Recipients[0];
                
                // Create HTTP client
                var client = _httpClientFactory.CreateClient("WebhookClient");
                
                // Create payload
                var payload = new
                {
                    subject = notification.Subject,
                    message = notification.Message,
                    timestamp = DateTime.UtcNow,
                    properties = notification.Properties
                };
                
                // Send webhook
                var response = await client.PostAsJsonAsync(webhookUrl, payload);
                
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Error sending webhook: {StatusCode}", response.StatusCode);
                }
                
                _logger.LogInformation("Sent webhook notification to {WebhookUrl}", webhookUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending webhook notification");
                throw;
            }
        }

        /// <summary>
        /// Sends a Slack notification
        /// </summary>
        private async Task SendSlackNotificationAsync(Notification notification)
        {
            try
            {
                // Get Slack webhook URL from the first recipient
                if (notification.Recipients.Count == 0)
                {
                    _logger.LogWarning("No Slack webhook URL provided");
                    return;
                }
                
                var webhookUrl = notification.Recipients[0];
                
                // Create HTTP client
                var client = _httpClientFactory.CreateClient("SlackClient");
                
                // Create payload
                var payload = new
                {
                    text = $"*{notification.Subject}*\n\n{notification.Message}",
                    attachments = new[]
                    {
                        new
                        {
                            color = GetSlackColor(notification),
                            fields = GetSlackFields(notification)
                        }
                    }
                };
                
                // Send Slack webhook
                var response = await client.PostAsJsonAsync(webhookUrl, payload);
                
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Error sending Slack notification: {StatusCode}", response.StatusCode);
                }
                
                _logger.LogInformation("Sent Slack notification to {WebhookUrl}", webhookUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending Slack notification");
                throw;
            }
        }

        /// <summary>
        /// Sends a Microsoft Teams notification
        /// </summary>
        private async Task SendTeamsNotificationAsync(Notification notification)
        {
            try
            {
                // Get Teams webhook URL from the first recipient
                if (notification.Recipients.Count == 0)
                {
                    _logger.LogWarning("No Teams webhook URL provided");
                    return;
                }
                
                var webhookUrl = notification.Recipients[0];
                
                // Create HTTP client
                var client = _httpClientFactory.CreateClient("TeamsClient");
                
                // Create payload
                var payload = new
                {
                    title = notification.Subject,
                    text = notification.Message,
                    themeColor = GetTeamsColor(notification),
                    sections = new[]
                    {
                        new
                        {
                            facts = GetTeamsFacts(notification)
                        }
                    }
                };
                
                // Send Teams webhook
                var response = await client.PostAsJsonAsync(webhookUrl, payload);
                
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Error sending Teams notification: {StatusCode}", response.StatusCode);
                }
                
                _logger.LogInformation("Sent Teams notification to {WebhookUrl}", webhookUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending Teams notification");
                throw;
            }
        }

        /// <summary>
        /// Gets the color for Slack notifications
        /// </summary>
        private string GetSlackColor(Notification notification)
        {
            if (notification.Properties.TryGetValue("AlertSeverity", out var severityObj) && 
                severityObj is string severity)
            {
                return severity.ToLowerInvariant() switch
                {
                    "critical" => "#FF0000", // Red
                    "high" => "#FFA500", // Orange
                    "medium" => "#FFFF00", // Yellow
                    "low" => "#00FF00", // Green
                    _ => "#808080" // Gray
                };
            }
            
            return "#808080"; // Gray
        }

        /// <summary>
        /// Gets fields for Slack notifications
        /// </summary>
        private object[] GetSlackFields(Notification notification)
        {
            var fields = new List<object>();
            
            foreach (var property in notification.Properties)
            {
                fields.Add(new
                {
                    title = property.Key,
                    value = property.Value?.ToString(),
                    @short = true
                });
            }
            
            return fields.ToArray();
        }

        /// <summary>
        /// Gets the color for Teams notifications
        /// </summary>
        private string GetTeamsColor(Notification notification)
        {
            if (notification.Properties.TryGetValue("AlertSeverity", out var severityObj) && 
                severityObj is string severity)
            {
                return severity.ToLowerInvariant() switch
                {
                    "critical" => "#FF0000", // Red
                    "high" => "#FFA500", // Orange
                    "medium" => "#FFFF00", // Yellow
                    "low" => "#00FF00", // Green
                    _ => "#808080" // Gray
                };
            }
            
            return "#808080"; // Gray
        }

        /// <summary>
        /// Gets facts for Teams notifications
        /// </summary>
        private object[] GetTeamsFacts(Notification notification)
        {
            var facts = new List<object>();
            
            foreach (var property in notification.Properties)
            {
                facts.Add(new
                {
                    name = property.Key,
                    value = property.Value?.ToString()
                });
            }
            
            return facts.ToArray();
        }
    }

    /// <summary>
    /// Interface for notification service
    /// </summary>
    public interface INotificationService
    {
        Task SendNotificationAsync(Notification notification);
    }
}
