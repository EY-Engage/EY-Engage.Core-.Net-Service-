using EYEngage.Core.Application.Dto.JobDto;
using EYEngage.Core.Domain;
using Microsoft.AspNetCore.Http;

namespace EYEngage.Core.Application.InterfacesServices;

public interface IJobService
{
    Task<JobOfferDto> CreateJobOfferAsync(JobOfferDto jobOfferDto, Guid publisherId, Department department);
    Task UpdateJobOfferAsync(JobOfferDto jobOfferDto);
    Task DeleteJobOfferAsync(Guid jobOfferId);
    Task<IEnumerable<JobOfferDto>> GetJobOffersAsync(string? department = null, bool activeOnly = true);
    Task<JobOfferDto> GetJobOfferByIdAsync(Guid jobOfferId);
    Task ApplyToJobAsync(JobApplicationDto applicationDto, Guid? userId, IFormFile? resumeFile);
    Task RecommendForJobAsync(JobApplicationDto recommendationDto, Guid recommendedByUserId, IFormFile resumeFile);
    Task<IEnumerable<JobApplicationDto>> GetApplicationsForJobAsync(Guid jobOfferId);
    Task<IEnumerable<JobApplicationDto>> GetTopCandidatesAsync(Guid jobOfferId);
    Task ScheduleInterviewAsync(Guid applicationId, DateTime interviewDate, string location);
    Task<Stream> DownloadResumeAsync(Guid applicationId);
}
