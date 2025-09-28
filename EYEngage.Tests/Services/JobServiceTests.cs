using EYEngage.Core.Application.Dto.JobDto;
using EYEngage.Core.Application.InterfacesServices;
using EYEngage.Core.Application.Services;
using EYEngage.Core.Domain;
using EYEngage.Infrastructure;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.ComponentModel.DataAnnotations;
using System.Text;
using Xunit;

namespace EYEngage.Tests.Services;

public class JobServiceTestsFixed : IDisposable
{
    private readonly EYEngageDbContext _context;
    private readonly Mock<IFileStorageService> _mockFileStorageService;
    private readonly Mock<IEmailService> _mockEmailService;
    private readonly Mock<GeminiService> _mockGeminiService;
    private readonly JobService _jobService;

    public JobServiceTestsFixed()
    {
        // Setup InMemory Database
        var options = new DbContextOptionsBuilder<EYEngageDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new EYEngageDbContext(options);

        // Setup mocks
        _mockFileStorageService = new Mock<IFileStorageService>();
        _mockEmailService = new Mock<IEmailService>();
        _mockGeminiService = new Mock<GeminiService>(Mock.Of<Microsoft.Extensions.Configuration.IConfiguration>(), Mock.Of<HttpClient>());

        _jobService = new JobService(
            _context,
            _mockFileStorageService.Object,
            _mockEmailService.Object,
            _mockGeminiService.Object);

        // Seed test data
        SeedTestData();
    }

    private void SeedTestData()
    {
        var testUser = new User
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            FullName = "Test Publisher",
            Email = "publisher@ey.com",
            Fonction = "Manager", // Required property
            Sector = "Business", // Required property
            Department = Department.Consulting,
            IsActive = true
        };

        var testUser2 = new User
        {
            Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            FullName = "Test Candidate",
            Email = "candidate@ey.com",
            Fonction = "Developer", // Required property
            Sector = "Technology", // Required property
            Department = Department.Assurance,
            PhoneNumber = "+1234567890",
            IsActive = true
        };

        _context.Users.AddRange(testUser, testUser2);
        _context.SaveChanges();
    }

    [Fact]
    public async Task CreateJobOfferAsync_ValidDto_CreatesJobOffer()
    {
        // Arrange
        var publisherId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var jobOfferDto = new JobOfferDto
        {
            Title = "Senior Developer",
            Description = "We are looking for a senior developer...",
            KeySkills = "C#, .NET, SQL",
            ExperienceLevel = "Senior",
            Location = "Paris, France",
            CloseDate = DateTime.Now.AddMonths(1),
            JobType = JobType.FullTime
        };

        // Act
        var result = await _jobService.CreateJobOfferAsync(jobOfferDto, publisherId, Department.Consulting);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("Senior Developer");
        result.Description.Should().Be("We are looking for a senior developer...");
        result.KeySkills.Should().Be("C#, .NET, SQL");
        result.ExperienceLevel.Should().Be("Senior");
        result.Location.Should().Be("Paris, France");
        result.JobType.Should().Be(JobType.FullTime);
        result.Department.Should().Be(Department.Consulting);
        result.PublisherId.Should().Be(publisherId);
        result.IsActive.Should().BeTrue();

        var jobOfferInDb = await _context.JobOffers.FirstOrDefaultAsync(j => j.Id == result.Id);
        jobOfferInDb.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateJobOfferAsync_NonExistentPublisher_ThrowsValidationException()
    {
        // Arrange
        var nonExistentPublisherId = Guid.NewGuid();
        var jobOfferDto = new JobOfferDto
        {
            Title = "Test Job",
            Description = "Test Description",
            KeySkills = "Test Skills",
            ExperienceLevel = "Junior",
            Location = "Test Location",
            JobType = JobType.FullTime
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => _jobService.CreateJobOfferAsync(jobOfferDto, nonExistentPublisherId, Department.Consulting));

        exception.Message.Should().Be("Publisher not found");
    }

    [Fact]
    public async Task ApplyToJobAsync_ValidApplication_CreatesApplication()
    {
        // Arrange
        var publisherId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var candidateId = Guid.Parse("22222222-2222-2222-2222-222222222222");

        var jobOffer = new JobOffer
        {
            Id = Guid.NewGuid(),
            Title = "Test Job",
            Description = "Test Description",
            KeySkills = "Test Skills",
            ExperienceLevel = "Junior",
            Location = "Test Location",
            PublisherId = publisherId,
            Department = Department.Consulting,
            JobType = JobType.FullTime,
            IsActive = true,
            CloseDate = DateTime.Now.AddMonths(1)
        };

        _context.JobOffers.Add(jobOffer);
        await _context.SaveChangesAsync();

        var applicationDto = new JobApplicationDto
        {
            JobOfferId = jobOffer.Id,
            CoverLetter = "I am very interested in this position..."
        };

        var mockResumeFile = new Mock<IFormFile>();
        mockResumeFile.Setup(f => f.FileName).Returns("resume.pdf");
        mockResumeFile.Setup(f => f.Length).Returns(1000);

        _mockFileStorageService.Setup(x => x.SaveFileAsync(It.IsAny<IFormFile>(), "resumes"))
            .ReturnsAsync("/resumes/resume.pdf");

        // Act
        await _jobService.ApplyToJobAsync(applicationDto, candidateId, mockResumeFile.Object);

        // Assert
        var application = await _context.JobApplications
            .FirstOrDefaultAsync(a => a.JobOfferId == jobOffer.Id && a.UserId == candidateId);

        application.Should().NotBeNull();
        application.CandidateName.Should().Be("Test Candidate");
        application.CandidateEmail.Should().Be("candidate@ey.com");
        application.CoverLetter.Should().Be("I am very interested in this position...");
        application.Status.Should().Be(ApplicationStatus.Pending);
        application.ResumeFilePath.Should().Be("/resumes/resume.pdf");
    }

    [Fact]
    public async Task ApplyToJobAsync_DuplicateApplication_ThrowsValidationException()
    {
        // Arrange
        var publisherId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var candidateId = Guid.Parse("22222222-2222-2222-2222-222222222222");

        var jobOffer = new JobOffer
        {
            Id = Guid.NewGuid(),
            Title = "Test Job",
            Description = "Test Description",
            KeySkills = "Test Skills",
            ExperienceLevel = "Junior",
            Location = "Test Location",
            PublisherId = publisherId,
            Department = Department.Consulting,
            JobType = JobType.FullTime,
            IsActive = true,
            CloseDate = DateTime.Now.AddMonths(1)
        };

        var existingApplication = new JobApplication
        {
            JobOfferId = jobOffer.Id,
            UserId = candidateId,
            CandidateName = "Test Candidate",
            CandidateEmail = "candidate@ey.com"
        };

        _context.JobOffers.Add(jobOffer);
        _context.JobApplications.Add(existingApplication);
        await _context.SaveChangesAsync();

        var applicationDto = new JobApplicationDto
        {
            JobOfferId = jobOffer.Id,
            CoverLetter = "Another application..."
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => _jobService.ApplyToJobAsync(applicationDto, candidateId, null));

        exception.Message.Should().Be("You have already applied to this job");
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}