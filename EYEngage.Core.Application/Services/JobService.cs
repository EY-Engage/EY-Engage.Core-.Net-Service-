using EYEngage.Core.Application.Dto.JobDto;
using EYEngage.Core.Application.InterfacesServices;
using EYEngage.Core.Domain;
using EYEngage.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Net.Mail;

namespace EYEngage.Core.Application.Services
{
    public class JobService : IJobService
    {
        private readonly EYEngageDbContext _db;
        private readonly IFileStorageService _fileStorage;
        private readonly IEmailService _mailService;
        private readonly GeminiService _geminiService;
        private readonly SocialNotificationService _socialNotificationService;
        public JobService(EYEngageDbContext db, IFileStorageService fileStorage,
                         IEmailService mailService, GeminiService geminiService, SocialNotificationService socialNotificationService)
        {
            _db = db;
            _fileStorage = fileStorage;
            _mailService = mailService;
            _geminiService = geminiService;
            _socialNotificationService = socialNotificationService;
        }

        public async Task<JobOfferDto> CreateJobOfferAsync(JobOfferDto jobOfferDto, Guid publisherId, Department department)
        {
            var publisher = await _db.Users.FindAsync(publisherId);
            if (publisher == null)
                throw new ValidationException("Publisher not found");

            var jobOffer = new JobOffer
            {
                Title = jobOfferDto.Title,
                Description = jobOfferDto.Description,
                KeySkills = jobOfferDto.KeySkills,
                ExperienceLevel = jobOfferDto.ExperienceLevel,
                Location = jobOfferDto.Location,
                PublishDate = DateTime.UtcNow,
                CloseDate = jobOfferDto.CloseDate,
                IsActive = true,
                PublisherId = publisherId,
                Department = department,
                JobType = jobOfferDto.JobType
            };

            _db.JobOffers.Add(jobOffer);
            await _db.SaveChangesAsync();

            // NOTIFICATION: Offre d'emploi créée
            await _socialNotificationService.NotifyJobCreated(jobOffer);

            return MapToDto(jobOffer);
        }

        public async Task ApplyToJobAsync(JobApplicationDto applicationDto, Guid? userId, IFormFile? resumeFile)
        {
            if (userId == null) throw new ValidationException("User not authenticated");

            var user = await _db.Users.FindAsync(userId);
            if (user == null) throw new ValidationException("User not found");

            var jobOffer = await _db.JobOffers
                .Include(j => j.Publisher)
                .FirstOrDefaultAsync(j => j.Id == applicationDto.JobOfferId);

            if (jobOffer == null || !jobOffer.IsActive)
                throw new ValidationException("Job offer not available");

            var existingApplication = await _db.JobApplications
                .FirstOrDefaultAsync(a => a.JobOfferId == applicationDto.JobOfferId && a.UserId == userId);

            if (existingApplication != null)
                throw new ValidationException("You have already applied to this job");

            string resumePath = null;
            if (resumeFile != null)
            {
                resumePath = await _fileStorage.SaveFileAsync(resumeFile, "resumes");
            }

            var application = new JobApplication
            {
                JobOfferId = applicationDto.JobOfferId,
                UserId = userId,
                CandidateName = user.FullName,
                CandidateEmail = user.Email,
                CandidatePhone = user.PhoneNumber,
                CoverLetter = applicationDto.CoverLetter,
                ResumeFilePath = resumePath,
                AppliedAt = DateTime.UtcNow,
                Status = ApplicationStatus.Pending
            };

            _db.JobApplications.Add(application);
            await _db.SaveChangesAsync();

            // NOTIFICATION: Candidature à un emploi
            await _socialNotificationService.NotifyJobApplication(application);
        }
        public async Task UpdateJobOfferAsync(JobOfferDto jobOfferDto)
        {
            var jobOffer = await _db.JobOffers.FindAsync(jobOfferDto.Id);
            if (jobOffer == null) throw new ValidationException("Job offer not found");

            jobOffer.Title = jobOfferDto.Title;
            jobOffer.Description = jobOfferDto.Description;
            jobOffer.KeySkills = jobOfferDto.KeySkills;
            jobOffer.ExperienceLevel = jobOfferDto.ExperienceLevel;
            jobOffer.Location = jobOfferDto.Location;
            jobOffer.CloseDate = jobOfferDto.CloseDate;
            jobOffer.JobType = jobOfferDto.JobType;

            await _db.SaveChangesAsync();
        }

        public async Task DeleteJobOfferAsync(Guid jobOfferId)
        {
            var jobOffer = await _db.JobOffers.FindAsync(jobOfferId);
            if (jobOffer == null) throw new ValidationException("Job offer not found");

            _db.JobOffers.Remove(jobOffer);
            await _db.SaveChangesAsync();
        }

        public async Task<IEnumerable<JobOfferDto>> GetJobOffersAsync(string? department = null, bool activeOnly = true)
        {
            var query = _db.JobOffers
                .Include(j => j.Applications)
                .AsQueryable();

            if (!string.IsNullOrEmpty(department))
            {
                if (Enum.TryParse<Department>(department, true, out Department dept))
                {
                    query = query.Where(j => j.Department == dept);
                }
            }
            if (activeOnly)
                query = query.Where(j => j.IsActive && j.CloseDate > DateTime.UtcNow);

            return await query
                .Select(j => new JobOfferDto
                {
                    Id = j.Id,
                    Title = j.Title,
                    Description = j.Description,
                    KeySkills = j.KeySkills,
                    ExperienceLevel = j.ExperienceLevel,
                    Location = j.Location,
                    PublishDate = j.PublishDate,
                    CloseDate = j.CloseDate,
                    IsActive = j.IsActive,
                    PublisherId = j.PublisherId,
                    Department = j.Department,
                    JobType = j.JobType,
                    ApplicationsCount = j.Applications.Count
                })
                .ToListAsync();
        }

        public async Task<JobOfferDto> GetJobOfferByIdAsync(Guid jobOfferId)
        {
            var jobOffer = await _db.JobOffers
                .Include(j => j.Applications)
                .FirstOrDefaultAsync(j => j.Id == jobOfferId);

            if (jobOffer == null) throw new ValidationException("Job offer not found");

            return MapToDto(jobOffer);
        }

        public async Task RecommendForJobAsync(JobApplicationDto recommendationDto, Guid recommendedByUserId, IFormFile resumeFile)
        {
            var recommender = await _db.Users.FindAsync(recommendedByUserId);
            if (recommender == null) throw new ValidationException("Recommender not found");

            var candidateEmail = recommendationDto.CandidateEmail;
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == candidateEmail);
            Guid? userId = user?.Id;

            string resumePath = await _fileStorage.SaveFileAsync(resumeFile, "resumes");

            var application = new JobApplication
            {
                JobOfferId = recommendationDto.JobOfferId,
                UserId = userId,
                CandidateName = recommendationDto.CandidateName,
                CandidateEmail = candidateEmail,
                CandidatePhone = recommendationDto.CandidatePhone,
                CoverLetter = recommendationDto.CoverLetter,
                ResumeFilePath = resumePath,
                AppliedAt = DateTime.UtcNow,
                Status = ApplicationStatus.Pending,
                RecommendedByUserId = recommendedByUserId
            };

            _db.JobApplications.Add(application);
            await _db.SaveChangesAsync();
        }

        public async Task<IEnumerable<JobApplicationDto>> GetApplicationsForJobAsync(Guid jobOfferId)
        {
            return await _db.JobApplications
                .Where(a => a.JobOfferId == jobOfferId)
                .Include(a => a.User)
                .Include(a => a.RecommendedBy)
                .Select(a => new JobApplicationDto
                {
                    Id = a.Id,
                    JobOfferId = a.JobOfferId,
                    CandidateName = a.CandidateName,
                    CandidateEmail = a.CandidateEmail,
                    CandidatePhone = a.CandidatePhone,
                    CoverLetter = a.CoverLetter,
                    ResumeFilePath = a.ResumeFilePath,
                    AppliedAt = a.AppliedAt,
                    Status = a.Status,
                    IsPreSelected = a.IsPreSelected,
                    RecommendedByUserId = a.RecommendedByUserId,
                    RecommendedByFullName = a.RecommendedBy != null ? a.RecommendedBy.FullName : null,
                    Score = a.Score,
                    Justification = a.Justification
                })
                .ToListAsync();
        }

        public async Task<IEnumerable<JobApplicationDto>> GetTopCandidatesAsync(Guid jobOfferId)
        {
            var jobOffer = await _db.JobOffers
                .Include(j => j.Applications)
                .ThenInclude(a => a.User)
                .FirstOrDefaultAsync(j => j.Id == jobOfferId);

            if (jobOffer == null) throw new ValidationException("Job offer not found");

            var applicationsData = new List<GeminiService.ApplicationData>();
            foreach (var app in jobOffer.Applications)
            {
                string resumeText = "";
                if (!string.IsNullOrEmpty(app.ResumeFilePath))
                {
                    try
                    {
                        var stream = await _fileStorage.GetFileAsync(app.ResumeFilePath);
                        if (stream != null)
                        {
                            resumeText = _geminiService.ExtractTextFromPdf(stream);
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error extracting PDF text: {ex.Message}");
                        resumeText = "";
                    }
                }

                applicationsData.Add(new GeminiService.ApplicationData
                {
                    ApplicationId = app.Id,
                    CandidateName = app.CandidateName,
                    ResumeText = resumeText,
                    CoverLetter = app.CoverLetter
                });
            }

            var topCandidates = await _geminiService.GetTopCandidatesAsync(jobOffer, applicationsData);

            if (topCandidates?.Any() == true)
            {
                foreach (var candidate in topCandidates)
                {
                    var application = jobOffer.Applications.FirstOrDefault(a => a.Id == candidate.ApplicationId);
                    if (application != null)
                    {
                        application.IsPreSelected = true;
                    }
                }

                await _db.SaveChangesAsync();
            }

            return topCandidates?.Select(c =>
            {
                var application = jobOffer.Applications.FirstOrDefault(a => a.Id == c.ApplicationId);
                return new JobApplicationDto
                {
                    Id = c.ApplicationId,
                    JobOfferId = jobOfferId,
                    CandidateName = c.CandidateName,
                    CandidateEmail = application?.CandidateEmail,
                    CandidatePhone = application?.CandidatePhone,
                    CoverLetter = application?.CoverLetter,
                    ResumeFilePath = application?.ResumeFilePath,
                    AppliedAt = application?.AppliedAt,
                    Status = application?.Status,
                    Score = c.Score,
                    Justification = c.Justification,
                    IsPreSelected = true
                };
            }) ?? Enumerable.Empty<JobApplicationDto>();
        }

        public async Task ScheduleInterviewAsync(Guid applicationId, DateTime interviewDate, string location)
        {
            try
            {
                if (applicationId == Guid.Empty)
                    throw new ValidationException("Invalid application ID");

                if (string.IsNullOrWhiteSpace(location))
                    throw new ValidationException("Location is required");

                if (interviewDate <= DateTime.Now)
                    throw new ValidationException("Interview date must be in the future");

                var application = await _db.JobApplications
                    .Include(a => a.JobOffer)
                    .Include(a => a.User)
                    .FirstOrDefaultAsync(a => a.Id == applicationId);

                if (application == null)
                {
                    throw new ValidationException($"Application with ID {applicationId} not found");
                }

                if (!application.JobOffer.IsActive)
                {
                    throw new ValidationException("Cannot schedule interview for inactive job offer");
                }

                application.Status = ApplicationStatus.Selected;
                await _db.SaveChangesAsync();

                // NOTIFICATION: Entretien programmé
                await _socialNotificationService.NotifyInterviewScheduled(application, interviewDate, location);

                try
                {
                    var candidateEmail = application.CandidateEmail;
                    var candidateName = application.CandidateName;
                    var subject = "Entretien chez EY";
                    var body = $@"
                Bonjour {candidateName},<br/>
                Vous êtes convié à un entretien pour le poste: {application.JobOffer.Title}.<br/>
                Date: {interviewDate:dd/MM/yyyy HH:mm}<br/>
                Lieu: {location}<br/>
                Cordialement,<br/>L'équipe EY Engage";

                    await _mailService.SendEmailAsync(candidateEmail, subject, body);
                }
                catch (SmtpException emailEx)
                {
                    Console.WriteLine($"Failed to send email notification: {emailEx.Message}");
                }
            }
            catch (ValidationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new ValidationException($"Failed to schedule interview: {ex.Message}");
            }
        }

        public async Task<Stream> DownloadResumeAsync(Guid applicationId)
        {
            var application = await _db.JobApplications
                .Include(a => a.JobOffer)
                .FirstOrDefaultAsync(a => a.Id == applicationId);

            if (application == null || string.IsNullOrEmpty(application.ResumeFilePath))
                throw new ValidationException("Resume not found");

            return await _fileStorage.GetFileAsync(application.ResumeFilePath);
        }

        private JobOfferDto MapToDto(JobOffer job)
        {
            return new JobOfferDto
            {
                Id = job.Id,
                Title = job.Title,
                Description = job.Description,
                KeySkills = job.KeySkills,
                ExperienceLevel = job.ExperienceLevel,
                Location = job.Location,
                PublishDate = job.PublishDate,
                CloseDate = job.CloseDate,
                IsActive = job.IsActive,
                PublisherId = job.PublisherId,
                Department = job.Department,
                JobType = job.JobType,
                ApplicationsCount = job.Applications?.Count ?? 0
            };
        }
    }
}