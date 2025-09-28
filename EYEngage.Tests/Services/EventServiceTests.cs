using EYEngage.Core.Application.Dto.EventDto;
using EYEngage.Core.Application.InterfacesServices;
using EYEngage.Core.Application.Services;
using EYEngage.Core.Domain;
using EYEngage.Infrastructure;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace EYEngage.Tests.Services;

public class EventServiceTestsFixed : IDisposable
{
    private readonly EYEngageDbContext _context;
    private readonly Mock<IEmailService> _mockEmailService;
    private readonly Mock<IWebHostEnvironment> _mockWebHostEnvironment;
    private readonly Mock<UserManager<User>> _mockUserManager;
    private readonly Mock<ILogger<EventService>> _mockLogger;
    private readonly EventService _eventService;

    public EventServiceTestsFixed()
    {
        // Setup InMemory Database
        var options = new DbContextOptionsBuilder<EYEngageDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new EYEngageDbContext(options);

        // Setup mocks
        _mockEmailService = new Mock<IEmailService>();
        _mockWebHostEnvironment = new Mock<IWebHostEnvironment>();
        _mockLogger = new Mock<ILogger<EventService>>();

        var userStoreMock = new Mock<IUserStore<User>>();
        _mockUserManager = new Mock<UserManager<User>>(
            userStoreMock.Object, null, null, null, null, null, null, null, null);

        _mockWebHostEnvironment.Setup(x => x.WebRootPath)
            .Returns("/app/wwwroot");

        _eventService = new EventService(
            _context,
            _mockEmailService.Object,
            _mockWebHostEnvironment.Object,
            _mockUserManager.Object,
            _mockLogger.Object);

        // Seed test data
        SeedTestData();
    }

    private void SeedTestData()
    {
        var testUser = new User
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            FullName = "Test User",
            Email = "test@ey.com",
            Fonction = "Developer", // Required property
            Sector = "Technology", // Required property
            Department = Department.Consulting,
            IsActive = true
        };

        var testUser2 = new User
        {
            Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            FullName = "Test User 2",
            Email = "test2@ey.com",
            Fonction = "Manager", // Required property
            Sector = "Business", // Required property
            Department = Department.Assurance,
            IsActive = true
        };

        _context.Users.AddRange(testUser, testUser2);
        _context.SaveChanges();
    }

    [Fact]
    public async Task CreateEventAsync_ValidDto_CreatesEvent()
    {
        // Arrange
        var organizerId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var createEventDto = new CreateEventDto(
            "Test Event",
            "Test Description",
            DateTime.Now.AddDays(7),
            "Test Location",
            null
        );

        var organizer = await _context.Users.FindAsync(organizerId);
        _mockUserManager.Setup(x => x.FindByIdAsync(organizerId.ToString()))
            .ReturnsAsync(organizer);

        // Act
        var result = await _eventService.CreateEventAsync(organizerId, createEventDto);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("Test Event");
        result.Description.Should().Be("Test Description");
        result.Location.Should().Be("Test Location");
        result.Status.Should().Be(EventStatus.Pending);
        result.OrganizerName.Should().Be("Test User");

        var eventInDb = await _context.Events.FirstOrDefaultAsync(e => e.Id == result.Id);
        eventInDb.Should().NotBeNull();
    }

    [Fact]
    public async Task GetEventsByStatusAsync_ValidStatus_ReturnsFilteredEvents()
    {
        // Arrange
        var organizerId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var userId = Guid.Parse("22222222-2222-2222-2222-222222222222");

        var approvedEvent = new Event
        {
            Id = Guid.NewGuid(),
            Title = "Approved Event",
            Description = "Test",
            Date = DateTime.Now.AddDays(5),
            Location = "Location",
            Status = EventStatus.Approved,
            OrganizerId = organizerId
        };

        var pendingEvent = new Event
        {
            Id = Guid.NewGuid(),
            Title = "Pending Event",
            Description = "Test",
            Date = DateTime.Now.AddDays(10),
            Location = "Location",
            Status = EventStatus.Pending,
            OrganizerId = organizerId
        };

        _context.Events.AddRange(approvedEvent, pendingEvent);
        await _context.SaveChangesAsync();

        // Act
        var result = await _eventService.GetEventsByStatusAsync(EventStatus.Approved, userId);

        // Assert
        result.Should().HaveCount(1);
        result.First().Title.Should().Be("Approved Event");
        result.First().Status.Should().Be(EventStatus.Approved);
    }

    [Fact]
    public async Task RequestParticipationAsync_ValidRequest_CreatesParticipation()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var userId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        var testEvent = new Event
        {
            Id = eventId,
            Title = "Test Event",
            Description = "Test",
            Date = DateTime.Now.AddDays(5),
            Location = "Location",
            Status = EventStatus.Approved,
            OrganizerId = userId
        };

        _context.Events.Add(testEvent);
        await _context.SaveChangesAsync();

        // Act
        await _eventService.RequestParticipationAsync(eventId, userId);

        // Assert
        var participation = await _context.EventParticipations
            .FirstOrDefaultAsync(p => p.EventId == eventId && p.UserId == userId);

        participation.Should().NotBeNull();
        participation.Status.Should().Be(ParticipationStatus.Pending);
    }

    [Fact]
    public async Task ApproveParticipationAsync_ValidParticipation_ApprovesAndSendsEmail()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var userId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var approverId = Guid.Parse("22222222-2222-2222-2222-222222222222");

        var testEvent = new Event
        {
            Id = eventId,
            Title = "Test Event",
            Description = "Test",
            Date = DateTime.Now.AddDays(5),
            Location = "Test Location",
            Status = EventStatus.Approved,
            OrganizerId = approverId
        };

        var participation = new EventParticipation
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            UserId = userId,
            Status = ParticipationStatus.Pending
        };

        _context.Events.Add(testEvent);
        _context.EventParticipations.Add(participation);
        await _context.SaveChangesAsync();

        // Act
        await _eventService.ApproveParticipationAsync(participation.Id, approverId);

        // Assert
        var updatedParticipation = await _context.EventParticipations
            .Include(p => p.Event)
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.Id == participation.Id);

        updatedParticipation.Should().NotBeNull();
        updatedParticipation.Status.Should().Be(ParticipationStatus.Approved);
        updatedParticipation.ApprovedById.Should().Be(approverId);
        updatedParticipation.DecidedAt.Should().NotBeNull();

        _mockEmailService.Verify(x => x.SendEmailAsync(
            It.IsAny<string>(),
            It.Is<string>(s => s.Contains("Participation confirmée")),
            It.IsAny<string>()),
            Times.Once);
    }

    [Fact]
    public async Task DeleteEventAsync_ValidEvent_DeletesEventAndDependencies()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var organizerId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        var testEvent = new Event
        {
            Id = eventId,
            Title = "Event to Delete",
            Description = "Test",
            Date = DateTime.Now.AddDays(5),
            Location = "Location",
            Status = EventStatus.Approved,
            OrganizerId = organizerId
        };

        var comment = new Comment
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            AuthorId = organizerId,
            Content = "Test comment"
        };

        _context.Events.Add(testEvent);
        _context.Comments.Add(comment);
        await _context.SaveChangesAsync();

        // Act
        await _eventService.DeleteEventAsync(eventId, organizerId);

        // Assert
        var deletedEvent = await _context.Events.FindAsync(eventId);
        var deletedComment = await _context.Comments
            .FirstOrDefaultAsync(c => c.EventId == eventId);

        deletedEvent.Should().BeNull();
        deletedComment.Should().BeNull();
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}