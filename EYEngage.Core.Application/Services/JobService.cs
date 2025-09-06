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
        public JobService(EYEngageDbContext db, IFileStorageService fileStorage,
                         IEmailService mailService, GeminiService geminiService)
        {
            _db = db;
            _fileStorage = fileStorage;
            _mailService = mailService;
            _geminiService = geminiService;        }

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

            var jobOffer = await _db.JobOffers.FindAsync(recommendationDto.JobOfferId);
            if (jobOffer == null) throw new ValidationException("Job offer not found");

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



            // EMAIL AU CANDIDAT RECOMMANDÉ
            try
            {
                var subject = "Vous avez été recommandé pour un poste chez EY";
                var body = $@"
            <html>
            <body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
                <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                    <div style='background: linear-gradient(135deg, #FFE135 0%, #FFC700 100%); padding: 20px; text-align: center; border-radius: 8px 8px 0 0;'>
                        <h1 style='margin: 0; color: #000; font-size: 24px;'>Recommandation EY</h1>
                    </div>
                    
                    <div style='background: #fff; padding: 30px; border: 1px solid #ddd; border-radius: 0 0 8px 8px;'>
                        <h2 style='color: #2C1810; margin-bottom: 20px;'>Bonjour {recommendationDto.CandidateName},</h2>
                        
                        <p style='margin-bottom: 15px;'>
                            Nous avons le plaisir de vous informer que <strong>{recommender.FullName}</strong> 
                            vous a recommandé pour le poste suivant chez EY :
                        </p>
                        
                        <div style='background: #f8f9fa; padding: 20px; border-left: 4px solid #FFE135; margin: 20px 0;'>
                            <h3 style='margin: 0 0 10px 0; color: #2C1810;'>{jobOffer.Title}</h3>
                            <p style='margin: 0; color: #666;'>
                                <strong>Département :</strong> {jobOffer.Department}<br>
                                <strong>Lieu :</strong> {jobOffer.Location}<br>
                                <strong>Niveau d'expérience :</strong> {jobOffer.ExperienceLevel}
                            </p>
                        </div>
                        
                        <p style='margin-bottom: 15px;'>
                            Votre candidature a été automatiquement soumise avec les documents fournis par votre recommandeur. 
                            Nos équipes RH examineront votre profil dans les plus brefs délais.
                        </p>
                        
                        <p style='margin-bottom: 20px;'>
                            Si vous souhaitez modifier ou compléter votre candidature, vous pouvez vous connecter 
                            sur notre plateforme EY Engage ou nous contacter directement.
                        </p>
                        
                        <div style='text-align: center; margin: 30px 0;'>
                            <a href='[LIEN_VERS_PLATEFORME]' 
                               style='background: #2C1810; color: white; padding: 12px 30px; text-decoration: none; border-radius: 5px; font-weight: bold;'>
                                Voir ma candidature
                            </a>
                        </div>
                        
                        <p style='color: #666; font-size: 14px; margin-top: 30px; border-top: 1px solid #eee; padding-top: 20px;'>
                            Nous vous remercions de l'intérêt que vous portez à EY et vous souhaitons 
                            bonne chance dans le processus de recrutement.
                        </p>
                        
                        <p style='margin-bottom: 0;'>
                            Cordialement,<br>
                            <strong>L'équipe Recrutement EY</strong>
                        </p>
                    </div>
                </div>
            </body>
            </html>";

                await _mailService.SendEmailAsync(candidateEmail, subject, body);
            }
            catch (SmtpException emailEx)
            {
                Console.WriteLine($"Erreur lors de l'envoi de l'email au candidat recommandé: {emailEx.Message}");
                // Note: On ne fait pas échouer la recommandation si l'email échoue
            }

            // EMAIL AU RECOMMANDEUR (confirmation)
            try
            {
                var confirmationSubject = "Confirmation de recommandation - EY";
                var confirmationBody = $@"
            <html>
            <body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
                <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                    <div style='background: linear-gradient(135deg, #FFE135 0%, #FFC700 100%); padding: 20px; text-align: center; border-radius: 8px 8px 0 0;'>
                        <h1 style='margin: 0; color: #000; font-size: 24px;'>Recommandation confirmée</h1>
                    </div>
                    
                    <div style='background: #fff; padding: 30px; border: 1px solid #ddd; border-radius: 0 0 8px 8px;'>
                        <h2 style='color: #2C1810; margin-bottom: 20px;'>Bonjour {recommender.FullName},</h2>
                        
                        <p style='margin-bottom: 15px;'>
                            Votre recommandation a été enregistrée avec succès !
                        </p>
                        
                        <div style='background: #f8f9fa; padding: 20px; border-left: 4px solid #28a745; margin: 20px 0;'>
                            <h3 style='margin: 0 0 10px 0; color: #2C1810;'>Détails de la recommandation</h3>
                            <p style='margin: 0; color: #666;'>
                                <strong>Candidat :</strong> {recommendationDto.CandidateName}<br>
                                <strong>Poste :</strong> {jobOffer.Title}<br>
                                <strong>Email du candidat :</strong> {candidateEmail}
                            </p>
                        </div>
                        
                        <p style='margin-bottom: 15px;'>
                            Le candidat a été automatiquement notifié de votre recommandation par email. 
                            Nos équipes RH examineront son profil dans le cadre du processus de recrutement.
                        </p>
                        
                        <p style='color: #666; font-size: 14px; margin-top: 30px; border-top: 1px solid #eee; padding-top: 20px;'>
                            Merci de contribuer au développement des talents chez EY !
                        </p>
                        
                        <p style='margin-bottom: 0;'>
                            Cordialement,<br>
                            <strong>L'équipe EY Engage</strong>
                        </p>
                    </div>
                </div>
            </body>
            </html>";

                await _mailService.SendEmailAsync(recommender.Email, confirmationSubject, confirmationBody);
            }
            catch (SmtpException emailEx)
            {
                Console.WriteLine($"Erreur lors de l'envoi de l'email de confirmation au recommandeur: {emailEx.Message}");
            }
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