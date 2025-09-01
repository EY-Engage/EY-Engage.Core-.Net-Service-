using System.Text.Json;
using EYEngage.Core.Domain;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EYEngage.Core.Application.Services
{
    public class SocialNotificationService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<SocialNotificationService> _logger;

        public SocialNotificationService(
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<SocialNotificationService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
        }

        // WEBHOOKS VERS NESTJS

        public async Task NotifyEventCreated(Event eventEntity)
        {
            try
            {
                var payload = new
                {
                    id = eventEntity.Id,
                    title = eventEntity.Title,
                    description = eventEntity.Description,
                    date = eventEntity.Date,
                    location = eventEntity.Location,
                    organizerId = eventEntity.OrganizerId,
                    organizerName = eventEntity.Organizer?.FullName,
                    organizerDepartment = eventEntity.Organizer?.Department.ToString(),
                    status = eventEntity.Status.ToString(),
                    imagePath = eventEntity.ImagePath
                };

                await SendWebhook("integration/events/created", payload);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to notify NestJS about event creation: {EventId}", eventEntity.Id);
            }
        }

        public async Task NotifyEventApproved(Event eventEntity)
        {
            try
            {
                var payload = new
                {
                    id = eventEntity.Id,
                    title = eventEntity.Title,
                    description = eventEntity.Description,
                    date = eventEntity.Date,
                    location = eventEntity.Location,
                    organizerId = eventEntity.OrganizerId,
                    organizerName = eventEntity.Organizer?.FullName,
                    organizerDepartment = eventEntity.Organizer?.Department.ToString(),
                    status = eventEntity.Status.ToString(),
                    approvedById = eventEntity.ApprovedById,
                    approvedByName = eventEntity.ApprovedBy?.FullName,
                    imagePath = eventEntity.ImagePath
                };

                await SendWebhook("integration/events/approved", payload);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to notify NestJS about event approval: {EventId}", eventEntity.Id);
            }
        }

        public async Task NotifyEventRejected(Event eventEntity)
        {
            try
            {
                var payload = new
                {
                    id = eventEntity.Id,
                    title = eventEntity.Title,
                    organizerId = eventEntity.OrganizerId,
                    organizerName = eventEntity.Organizer?.FullName,
                    organizerDepartment = eventEntity.Organizer?.Department.ToString(),
                    status = eventEntity.Status.ToString(),
                    approvedById = eventEntity.ApprovedById,
                    approvedByName = eventEntity.ApprovedBy?.FullName
                };

                await SendWebhook("integration/events/rejected", payload);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to notify NestJS about event rejection: {EventId}", eventEntity.Id);
            }
        }

        public async Task NotifyParticipationRequested(EventParticipation participation)
        {
            try
            {
                var payload = new
                {
                    id = participation.Id,
                    eventId = participation.EventId,
                    eventTitle = participation.Event?.Title,
                    userId = participation.UserId,
                    userName = participation.User?.FullName,
                    userEmail = participation.User?.Email,
                    status = participation.Status.ToString(),
                    requestedAt = participation.RequestedAt,
                    eventDepartment = participation.Event?.Organizer?.Department.ToString()
                };

                await SendWebhook("integration/participations/requested", payload);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to notify NestJS about participation request: {ParticipationId}", participation.Id);
            }
        }

        public async Task NotifyParticipationApproved(EventParticipation participation)
        {
            try
            {
                var payload = new
                {
                    id = participation.Id,
                    eventId = participation.EventId,
                    eventTitle = participation.Event?.Title,
                    eventDate = participation.Event?.Date,
                    eventLocation = participation.Event?.Location,
                    userId = participation.UserId,
                    userName = participation.User?.FullName,
                    userEmail = participation.User?.Email,
                    status = participation.Status.ToString(),
                    requestedAt = participation.RequestedAt,
                    decidedAt = participation.DecidedAt,
                    approvedById = participation.ApprovedById,
                    approvedByName = participation.ApprovedBy?.FullName
                };

                await SendWebhook("integration/participations/approved", payload);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to notify NestJS about participation approval: {ParticipationId}", participation.Id);
            }
        }

        public async Task NotifyParticipationRejected(EventParticipation participation)
        {
            try
            {
                var payload = new
                {
                    id = participation.Id,
                    eventId = participation.EventId,
                    eventTitle = participation.Event?.Title,
                    userId = participation.UserId,
                    userName = participation.User?.FullName,
                    userEmail = participation.User?.Email,
                    status = participation.Status.ToString(),
                    requestedAt = participation.RequestedAt,
                    decidedAt = participation.DecidedAt,
                    approvedById = participation.ApprovedById,
                    approvedByName = participation.ApprovedBy?.FullName
                };

                await SendWebhook("integration/participations/rejected", payload);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to notify NestJS about participation rejection: {ParticipationId}", participation.Id);
            }
        }

        public async Task NotifyJobCreated(JobOffer jobOffer)
        {
            try
            {
                var payload = new
                {
                    id = jobOffer.Id,
                    title = jobOffer.Title,
                    description = jobOffer.Description,
                    keySkills = jobOffer.KeySkills,
                    experienceLevel = jobOffer.ExperienceLevel,
                    location = jobOffer.Location,
                    publisherId = jobOffer.PublisherId,
                    publisherName = jobOffer.Publisher?.FullName,
                    department = jobOffer.Department.ToString(),
                    jobType = jobOffer.JobType.ToString(),
                    isActive = jobOffer.IsActive,
                    publishDate = jobOffer.PublishDate,
                    closeDate = jobOffer.CloseDate
                };

                await SendWebhook("integration/jobs/created", payload);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to notify NestJS about job creation: {JobId}", jobOffer.Id);
            }
        }

        public async Task NotifyJobApplication(JobApplication application)
        {
            try
            {
                var payload = new
                {
                    id = application.Id,
                    jobOfferId = application.JobOfferId,
                    jobTitle = application.JobOffer?.Title,
                    jobPublisherId = application.JobOffer?.PublisherId,
                    candidateId = application.UserId,
                    candidateName = application.CandidateName,
                    candidateEmail = application.CandidateEmail,
                    status = application.Status.ToString(),
                    appliedAt = application.AppliedAt,
                    recommendedByUserId = application.RecommendedByUserId,
                    recommendedByName = application.RecommendedBy?.FullName,
                    jobDepartment = application.JobOffer?.Department.ToString()
                };

                await SendWebhook("integration/jobs/applied", payload);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to notify NestJS about job application: {ApplicationId}", application.Id);
            }
        }

        public async Task NotifyInterviewScheduled(JobApplication application, DateTime interviewDate, string location)
        {
            try
            {
                var payload = new
                {
                    applicationId = application.Id,
                    jobId = application.JobOfferId,
                    jobTitle = application.JobOffer?.Title,
                    candidateId = application.UserId,
                    candidateName = application.CandidateName,
                    candidateEmail = application.CandidateEmail,
                    interviewDate = interviewDate,
                    location = location
                };

                await SendWebhook("integration/jobs/interview-scheduled", payload);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to notify NestJS about interview scheduling: {ApplicationId}", application.Id);
            }
        }

        public async Task NotifyUserCreated(User user)
        {
            try
            {
                var payload = new
                {
                    id = user.Id,
                    fullName = user.FullName,
                    email = user.Email,
                    profilePicture = user.ProfilePicture,
                    phoneNumber = user.PhoneNumber,
                    fonction = user.Fonction,
                    department = user.Department.ToString(),
                    sector = user.Sector,
                    isActive = user.IsActive,
                    isFirstLogin = user.IsFirstLogin,
                    createdAt = user.CreatedAt,
                    updatedAt = user.UpdatedAt,
                    sessionId = user.SessionId
                };

                await SendWebhook("integration/users/created", payload);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to notify NestJS about user creation: {UserId}", user.Id);
            }
        }

        public async Task NotifyUserActivated(User user)
        {
            try
            {
                var payload = new
                {
                    id = user.Id,
                    fullName = user.FullName,
                    email = user.Email,
                    isActive = user.IsActive,
                    isFirstLogin = user.IsFirstLogin,
                    updatedAt = user.UpdatedAt
                };

                await SendWebhook("integration/users/activated", payload);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to notify NestJS about user activation: {UserId}", user.Id);
            }
        }

        public async Task NotifyCommentCreated(Comment comment)
        {
            try
            {
                var payload = new
                {
                    id = comment.Id,
                    eventId = comment.EventId,
                    eventTitle = comment.Event?.Title,
                    authorId = comment.AuthorId,
                    authorName = comment.Author?.FullName,
                    content = comment.Content,
                    createdAt = comment.CreatedAt,
                    eventOrganizerId = comment.Event?.OrganizerId,
                    eventDepartment = comment.Event?.Organizer?.Department.ToString()
                };

                await SendWebhook("integration/comments/created", payload);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to notify NestJS about comment creation: {CommentId}", comment.Id);
            }
        }

        private async Task SendWebhook(string endpoint, object payload)
        {
            var nestjsUrl = _configuration["NestJSService:BaseUrl"];
            var apiKey = _configuration["NestJSService:ApiKey"];

            if (string.IsNullOrEmpty(nestjsUrl))
            {
                _logger.LogWarning("NestJS service URL not configured");
                return;
            }

            var url = $"{nestjsUrl}/api/{endpoint}";
            var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
            };

            if (!string.IsNullOrEmpty(apiKey))
            {
                request.Headers.Add("x-api-key", apiKey);
            }

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Failed to send webhook to NestJS: {StatusCode} {ReasonPhrase} for endpoint {Endpoint}",
                    response.StatusCode,
                    response.ReasonPhrase,
                    endpoint);
            }
            else
            {
                _logger.LogInformation("Successfully sent webhook to NestJS: {Endpoint}", endpoint);
            }
        }
    }
}