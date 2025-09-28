using EYEngage.Core.Application.Common.Exceptions;
using EYEngage.Core.Application.Dto;
using EYEngage.Core.Application.Dto.UserDtos;
using EYEngage.Core.Application.InterfacesServices;
using EYEngage.Core.Domain;
using EYEngage.Infrastructure;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.ComponentModel.DataAnnotations;
using Xunit;

namespace EYEngage.Tests.Services;

public class UserServiceTests : IDisposable
{
    private readonly Mock<UserManager<User>> _mockUserManager;
    private readonly Mock<IWebHostEnvironment> _mockWebHostEnvironment;
    private readonly Mock<IEmailService> _mockEmailService;
    private readonly EYEngageDbContext _context;
    private readonly UserService _userService;

    public UserServiceTests()
    {
        // Setup InMemory Database
        var options = new DbContextOptionsBuilder<EYEngageDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new EYEngageDbContext(options);

        // Setup UserManager mock
        var userStoreMock = new Mock<IUserStore<User>>();
        _mockUserManager = new Mock<UserManager<User>>(
            userStoreMock.Object, null, null, null, null, null, null, null, null);

        _mockWebHostEnvironment = new Mock<IWebHostEnvironment>();
        _mockEmailService = new Mock<IEmailService>();

        _mockWebHostEnvironment.Setup(x => x.WebRootPath)
            .Returns("/app/wwwroot");

        _userService = new UserService(
            _mockUserManager.Object,
            _mockWebHostEnvironment.Object,
            _mockEmailService.Object,
            _context);

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
            UserName = "test@ey.com",
            Department = Department.Consulting,
            Fonction = "Developer",
            Sector = "Technology",
            PhoneNumber = "+1234567890",
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddDays(-30),
            UpdatedAt = DateTime.UtcNow
        };

        _context.Users.Add(testUser);
        _context.SaveChanges();
    }

    [Fact]
    public async Task GetAllUsersAsync_ReturnsAllUsers()
    {
        // Act
        var result = await _userService.GetAllUsersAsync();

        // Assert
        result.Should().NotBeEmpty();
        result.Should().HaveCount(1);
        result.First().Email.Should().Be("test@ey.com");
        result.First().FullName.Should().Be("Test User");
    }

    [Fact]
    public async Task GetUserByIdAsync_ExistingUser_ReturnsUser()
    {
        // Arrange
        var userId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        // Act
        var result = await _userService.GetUserByIdAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(userId);
        result.Email.Should().Be("test@ey.com");
        result.FullName.Should().Be("Test User");
        result.Department.Should().Be(Department.Consulting);
    }

    [Fact]
    public async Task GetUserByIdAsync_NonExistentUser_ThrowsNotFoundException()
    {
        // Arrange
        var nonExistentUserId = Guid.NewGuid();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<NotFoundException>(
            () => _userService.GetUserByIdAsync(nonExistentUserId));

        exception.Message.Should().Contain("User");
    }

    [Fact]
    public async Task GetUserPublicProfileAsync_ExistingUser_ReturnsPublicProfile()
    {
        // Arrange
        var userId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var user = await _context.Users.FindAsync(userId);
        var roles = new List<string> { "EmployeeEY" };

        _mockUserManager.Setup(x => x.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(user);
        _mockUserManager.Setup(x => x.GetRolesAsync(user))
            .ReturnsAsync(roles);

        // Act
        var result = await _userService.GetUserPublicProfileAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(userId);
        result.FullName.Should().Be("Test User");
        result.Email.Should().Be("test@ey.com");
        result.Department.Should().Be(Department.Consulting);
        result.Roles.Should().Contain("EmployeeEY");
    }

    [Fact]
    public async Task CreateUserAsync_ValidDto_CreatesUser()
    {
        // Arrange
        var createUserDto = new CreateUserDto
        {
            FullName = "New User",
            Email = "newuser@ey.com",
            Password = "SecurePassword123!",
            Fonction = "Manager",
            Department = Department.Assurance,
            Sector = "Finance",
            PhoneNumber = "+0987654321"
        };

        var createdUser = new User
        {
            Id = Guid.NewGuid(),
            FullName = createUserDto.FullName,
            Email = createUserDto.Email,
            UserName = createUserDto.Email,
            Fonction = createUserDto.Fonction,
            Department = createUserDto.Department,
            Sector = createUserDto.Sector,
            PhoneNumber = createUserDto.PhoneNumber,
            IsActive = false,
            IsFirstLogin = true
        };

        _mockUserManager.Setup(x => x.CreateAsync(It.IsAny<User>(), createUserDto.Password))
            .ReturnsAsync(IdentityResult.Success)
            .Callback<User, string>((user, password) =>
            {
                // Simulate user creation by adding to context
                user.Id = createdUser.Id;
                _context.Users.Add(user);
                _context.SaveChanges();
            });

        _mockUserManager.Setup(x => x.AddToRoleAsync(It.IsAny<User>(), "EmployeeEY"))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _userService.CreateUserAsync(createUserDto);

        // Assert
        result.Should().Be("Utilisateur créé avec succès.");

        _mockUserManager.Verify(x => x.CreateAsync(
            It.Is<User>(u =>
                u.FullName == "New User" &&
                u.Email == "newuser@ey.com" &&
                u.Department == Department.Assurance &&
                u.IsActive == false &&
                u.IsFirstLogin == true),
            createUserDto.Password), Times.Once);

        _mockUserManager.Verify(x => x.AddToRoleAsync(It.IsAny<User>(), "EmployeeEY"), Times.Once);

        _mockEmailService.Verify(x => x.SendUserCredentials(
            createUserDto.Email,
            createUserDto.Password), Times.Once);
    }

    [Fact]
    public async Task CreateUserAsync_InvalidEmail_ThrowsValidationException()
    {
        // Arrange
        var createUserDto = new CreateUserDto
        {
            Email = "", // Invalid email
            Password = "SecurePassword123!",
            FullName = "Test User"
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => _userService.CreateUserAsync(createUserDto));

        exception.Message.Should().Contain("Email & mot de passe obligatoires");
    }

    [Fact]
    public async Task CreateUserAsync_UserCreationFails_ThrowsValidationException()
    {
        // Arrange
        var createUserDto = new CreateUserDto
        {
            FullName = "New User",
            Email = "newuser@ey.com",
            Password = "weak", // This should cause validation to fail
            Department = Department.Consulting,
            Fonction = "Developer",
            Sector = "IT"
        };

        var identityErrors = new[]
        {
            new IdentityError { Description = "Password is too weak" }
        };

        _mockUserManager.Setup(x => x.CreateAsync(It.IsAny<User>(), createUserDto.Password))
            .ReturnsAsync(IdentityResult.Failed(identityErrors));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => _userService.CreateUserAsync(createUserDto));

        exception.Message.Should().Contain("Création échouée");
        exception.Message.Should().Contain("Password is too weak");
    }

    [Fact]
    public async Task UpdateUserProfileAsync_ValidUpdate_UpdatesProfile()
    {
        // Arrange
        var userId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var user = await _context.Users.FindAsync(userId);

        var updateDto = new UpdateUserProfileDto
        {
            FullName = "Updated Name",
            PhoneNumber = "+1111111111",
            Fonction = "Senior Developer",
            Department = Department.Tax,
            Sector = "Technology Updated"
        };

        _mockUserManager.Setup(x => x.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(user);
        _mockUserManager.Setup(x => x.UpdateAsync(It.IsAny<User>()))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _userService.UpdateUserProfileAsync(userId, updateDto);

        // Assert
        result.Should().Be("Profil mis à jour avec succès");

        _mockUserManager.Verify(x => x.UpdateAsync(It.Is<User>(u =>
            u.FullName == "Updated Name" &&
            u.PhoneNumber == "+1111111111" &&
            u.Fonction == "Senior Developer" &&
            u.Department == Department.Tax &&
            u.Sector == "Technology Updated")), Times.Once);
    }

    [Fact]
    public async Task UpdatePasswordAsync_ValidRequest_UpdatesPassword()
    {
        // Arrange
        var userId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var user = await _context.Users.FindAsync(userId);

        var updatePasswordDto = new UpdatePasswordDto
        {
            CurrentPassword = "OldPassword123!",
            NewPassword = "NewPassword123!"
        };

        _mockUserManager.Setup(x => x.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(user);
        _mockUserManager.Setup(x => x.ChangePasswordAsync(user, updatePasswordDto.CurrentPassword, updatePasswordDto.NewPassword))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _userService.UpdatePasswordAsync(userId, updatePasswordDto);

        // Assert
        result.Should().Be("Mot de passe mis à jour avec succès");

        _mockUserManager.Verify(x => x.ChangePasswordAsync(
            user,
            updatePasswordDto.CurrentPassword,
            updatePasswordDto.NewPassword), Times.Once);
    }

    [Fact]
    public async Task UpdatePasswordAsync_InvalidCurrentPassword_ThrowsValidationException()
    {
        // Arrange
        var userId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var user = await _context.Users.FindAsync(userId);

        var updatePasswordDto = new UpdatePasswordDto
        {
            CurrentPassword = "WrongPassword",
            NewPassword = "NewPassword123!"
        };

        var identityErrors = new[]
        {
            new IdentityError { Description = "Incorrect password" }
        };

        _mockUserManager.Setup(x => x.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(user);
        _mockUserManager.Setup(x => x.ChangePasswordAsync(user, updatePasswordDto.CurrentPassword, updatePasswordDto.NewPassword))
            .ReturnsAsync(IdentityResult.Failed(identityErrors));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => _userService.UpdatePasswordAsync(userId, updatePasswordDto));

        exception.Message.Should().Contain("Erreur de changement de mot de passe");
        exception.Message.Should().Contain("Incorrect password");
    }

    [Fact]
    public async Task IsEmailUniqueAsync_UniqueEmail_ReturnsTrue()
    {
        // Arrange
        var uniqueEmail = "unique@ey.com";

        // Act
        var result = await _userService.IsEmailUniqueAsync(uniqueEmail);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsEmailUniqueAsync_ExistingEmail_ReturnsFalse()
    {
        // Arrange
        var existingEmail = "test@ey.com"; // This email exists in our seed data

        // Act
        var result = await _userService.IsEmailUniqueAsync(existingEmail);

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData("invalid-email")]
    [InlineData(null)]
    public async Task IsEmailUniqueAsync_InvalidEmail_ThrowsValidationException(string invalidEmail)
    {
        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => _userService.IsEmailUniqueAsync(invalidEmail));

        exception.Message.Should().Be("Email invalide ou manquant");
    }

    [Fact]
    public async Task UpdateUserAsync_ValidUpdate_UpdatesUser()
    {
        // Arrange
        var userId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var updateDto = new UpdateUserDto
        {
            FullName = "Updated User Name",
            Fonction = "Updated Function",
            Department = Department.StrategyAndTransactions,
            Sector = "Updated Sector",
            PhoneNumber = "+9999999999"
        };

        _mockUserManager.Setup(x => x.UpdateAsync(It.IsAny<User>()))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _userService.UpdateUserAsync(userId, updateDto);

        // Assert
        result.Should().Be("Utilisateur mis à jour avec succès.");

        var updatedUser = await _context.Users.FindAsync(userId);
        updatedUser.FullName.Should().Be("Updated User Name");
        updatedUser.Fonction.Should().Be("Updated Function");
        updatedUser.Department.Should().Be(Department.StrategyAndTransactions);
        updatedUser.Sector.Should().Be("Updated Sector");
        updatedUser.PhoneNumber.Should().Be("+9999999999");
    }

    [Fact]
    public async Task DeleteUserAsync_ExistingUser_DeletesUserAndDependencies()
    {
        // Arrange
        var userId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        // Add some dependencies to test cascade deletion
        var testEvent = new Event
        {
            Id = Guid.NewGuid(),
            Title = "User's Event",
            Description = "Event to be deleted",
            Date = DateTime.Now.AddDays(5),
            Location = "Test Location",
            OrganizerId = userId,
            Status = EventStatus.Approved
        };

        var participation = new EventParticipation
        {
            Id = Guid.NewGuid(),
            EventId = testEvent.Id,
            UserId = userId,
            Status = ParticipationStatus.Approved
        };

        _context.Events.Add(testEvent);
        _context.EventParticipations.Add(participation);
        await _context.SaveChangesAsync();

        // Act
        var result = await _userService.DeleteUserAsync(userId);

        // Assert
        result.Should().Be("Utilisateur supprimé avec succès.");

        var deletedUser = await _context.Users.FindAsync(userId);
        deletedUser.Should().BeNull();

        // Verify dependencies were also deleted
        var deletedEvent = await _context.Events.FindAsync(testEvent.Id);
        deletedEvent.Should().BeNull();

        var deletedParticipation = await _context.EventParticipations.FindAsync(participation.Id);
        deletedParticipation.Should().BeNull();
    }

    [Fact]
    public async Task DeleteUserAsync_NonExistentUser_ThrowsNotFoundException()
    {
        // Arrange
        var nonExistentUserId = Guid.NewGuid();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<NotFoundException>(
            () => _userService.DeleteUserAsync(nonExistentUserId));

        exception.Message.Should().Contain("User");
    }

    [Fact]
    public async Task UpdateUserProfilePictureAsync_ValidFile_UpdatesProfilePicture()
    {
        // Arrange
        var userId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var user = await _context.Users.FindAsync(userId);

        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.FileName).Returns("profile.jpg");
        mockFile.Setup(f => f.Length).Returns(1024);
        mockFile.Setup(f => f.OpenReadStream())
            .Returns(new MemoryStream(new byte[1024]));
        mockFile.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockUserManager.Setup(x => x.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(user);
        _mockUserManager.Setup(x => x.UpdateAsync(It.IsAny<User>()))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _userService.UpdateUserProfilePictureAsync(userId, mockFile.Object);

        // Assert
        result.Should().Be("Photo de profil mise à jour avec succès");

        _mockUserManager.Verify(x => x.UpdateAsync(It.Is<User>(u =>
            !string.IsNullOrEmpty(u.ProfilePicture) &&
            u.ProfilePicture.Contains("/profile-pictures/"))), Times.Once);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}