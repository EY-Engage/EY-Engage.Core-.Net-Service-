// EYEngage.Core.Application/Services/NotificationService.cs
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using EYEngage.Core.Application.InterfacesServices;
using EYEngage.Core.Domain;
using Microsoft.AspNetCore.Identity;

namespace EYEngage.Core.Application.Services;

public class NotificationService : INotificationService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly string _nestJsUrl;
    private readonly string _apiKey;

    public NotificationService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _nestJsUrl = configuration["NestJS:BaseUrl"] ?? "http://localhost:3001";
        _apiKey = configuration["NestJS:ApiKey"] ?? "ca905aeecc4ed43d605182455d7ecec09b03c64ec5eb1f57963a044a467f452d";

        // Set API key header once in constructor
        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("x-api-key", _apiKey);
    }

    public async Task SendNotificationAsync(NotificationDto notification)
    {
        var json = JsonSerializer.Serialize(notification, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(
            $"{_nestJsUrl}/api/notifications/webhook",
            content
        );

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new Exception($"Failed to send notification: {error}");
        }
    }

    public async Task SendBulkNotificationsAsync(BulkNotificationDto notifications)
    {
        var json = JsonSerializer.Serialize(notifications, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(
            $"{_nestJsUrl}/api/notifications/webhook/bulk",
            content
        );

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new Exception($"Failed to send bulk notifications: {error}");
        }
    }
}

// DTOs with proper validation and enums
public class NotificationDto
{
    public string RecipientId { get; set; } = string.Empty;
    public string RecipientName { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Priority { get; set; } = "medium";
    public NotificationMetadata? Metadata { get; set; }
}

public class NotificationMetadata
{
    public string? EntityId { get; set; }
    public string? EntityType { get; set; }
    public string? ActionUrl { get; set; }
    public string? ActorId { get; set; }
    public string? ActorName { get; set; }
    public string? Department { get; set; }
    public object? AdditionalData { get; set; }
}

public class BulkNotificationDto
{
    public List<RecipientInfo> Recipients { get; set; } = new();
    public NotificationData Notification { get; set; } = new();
}

public class RecipientInfo
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

public class NotificationData
{
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Priority { get; set; } = "medium";
    public NotificationMetadata? Metadata { get; set; }
}

// Enums pour assurer la cohérence avec NestJS
public static class NotificationTypes
{
    public const string EVENT_CREATED = "event_created";
    public const string EVENT_APPROVED = "event_approved";
    public const string EVENT_REJECTED = "event_rejected";
    public const string EVENT_PARTICIPATION_REQUEST = "event_participation_request";
    public const string EVENT_PARTICIPATION_APPROVED = "event_participation_approved";
    public const string EVENT_PARTICIPATION_REJECTED = "event_participation_rejected";
    public const string EVENT_COMMENT = "event_comment";
    public const string EVENT_REMINDER = "event_reminder";
    public const string JOB_POSTED = "job_posted";
    public const string JOB_APPLICATION = "job_application";
    public const string JOB_RECOMMENDATION = "job_recommendation";
    public const string JOB_INTERVIEW_SCHEDULED = "job_interview_scheduled";
    public const string JOB_STATUS_CHANGED = "job_status_changed";
    public const string POST_MENTION = "post_mention";
    public const string POST_COMMENT = "post_comment";
    public const string POST_REACTION = "post_reaction";
    public const string POST_SHARE = "post_share";
    public const string POST_FLAGGED = "post_flagged";
    public const string CONTENT_FLAGGED = "content_flagged";
    public const string MODERATION_ACTION = "moderation_action";
    public const string USER_WARNING = "user_warning";
    public const string WELCOME = "welcome";
    public const string PASSWORD_CHANGED = "password_changed";
    public const string PROFILE_UPDATED = "profile_updated";
}

public static class NotificationPriorities
{
    public const string LOW = "low";
    public const string MEDIUM = "medium";
    public const string HIGH = "high";
    public const string URGENT = "urgent";
}