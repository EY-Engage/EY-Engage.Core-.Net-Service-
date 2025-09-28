using EYEngage.Core.Application.Dto.AuthDtos;
using EYEngage.Core.Application.Services;
using EYEngage.Core.Domain;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using System.Security.Claims;
using Xunit;

namespace EYEngage.Tests.Services;

public class AuthServiceTestsFixed
{
    private readonly Mock<UserManager<User>> _mockUserManager;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
    private readonly AuthService _authService;
    private readonly Mock<HttpContext> _mockHttpContext;
    private readonly Mock<ClaimsPrincipal> _mockClaimsPrincipal;

    public AuthServiceTestsFixed()
    {
        // Setup UserManager mock
        var userStoreMock = new Mock<IUserStore<User>>();
        _mockUserManager = new Mock<UserManager<User>>(
            userStoreMock.Object, null, null, null, null, null, null, null, null);

        _mockConfiguration = new Mock<IConfiguration>();
        _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        _mockHttpContext = new Mock<HttpContext>();
        _mockClaimsPrincipal = new Mock<ClaimsPrincipal>();

        // Setup JWT configuration
        _mockConfiguration.Setup(x => x["JwtSettings:Secret"])
            .Returns("super-secret-key-for-testing-purposes-minimum-256-bits");
        _mockConfiguration.Setup(x => x["JwtSettings:Issuer"])
            .Returns("http://localhost:5058");
        _mockConfiguration.Setup(x => x["JwtSettings:Audience"])
            .Returns("http://localhost:5058");

        _mockHttpContextAccessor.Setup(x => x.HttpContext)
            .Returns(_mockHttpContext.Object);

        _authService = new AuthService(
            _mockUserManager.Object,
            _mockConfiguration.Object,
            _mockHttpContextAccessor.Object);
    }

    [Fact]
    public async Task LoginAsync_ValidCredentials_ReturnsLoginResponse()
    {
        // Arrange
        var loginRequest = new LoginRequestDto
        {
            Email = "test@ey.com",
            Password = "ValidPassword123!"
        };

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@ey.com",
            FullName = "Test User",
            Fonction = "Developer",
            Sector = "IT",
            IsActive = true,
            IsFirstLogin = false,
            SessionId = Guid.NewGuid()
        };

        var roles = new List<string> { "EmployeeEY" };

        _mockUserManager.Setup(x => x.FindByEmailAsync(loginRequest.Email))
            .ReturnsAsync(user);
        _mockUserManager.Setup(x => x.CheckPasswordAsync(user, loginRequest.Password))
            .ReturnsAsync(true);
        _mockUserManager.Setup(x => x.GetRolesAsync(user))
            .ReturnsAsync(roles);
        _mockUserManager.Setup(x => x.UpdateAsync(user))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _authService.LoginAsync(loginRequest);

        // Assert
        result.Should().NotBeNull();
        result.Email.Should().Be(loginRequest.Email);
        result.FullName.Should().Be(user.FullName);
        result.IsActive.Should().BeTrue();
        result.IsFirstLogin.Should().BeFalse();
        result.Roles.Should().Contain("EmployeeEY");
        result.AccessToken.Should().NotBeNullOrEmpty();
        result.RefreshToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task LoginAsync_InvalidCredentials_ThrowsUnauthorizedException()
    {
        // Arrange
        var loginRequest = new LoginRequestDto
        {
            Email = "invalid@ey.com",
            Password = "wrongpassword"
        };

        _mockUserManager.Setup(x => x.FindByEmailAsync(loginRequest.Email))
            .ReturnsAsync((User)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _authService.LoginAsync(loginRequest));

        exception.Message.Should().Be("Identifiants invalides");
    }

    [Fact]
    public async Task ChangePasswordAsync_ValidRequest_ReturnsLoginResponse()
    {
        // Arrange
        var changePasswordDto = new ChangePasswordDto
        {
            Email = "test@ey.com",
            CurrentPassword = "OldPassword123!",
            NewPassword = "NewPassword123!",
            ConfirmNewPassword = "NewPassword123!"
        };

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@ey.com",
            FullName = "Test User",
            Fonction = "Developer",
            Sector = "IT",
            IsActive = false,
            IsFirstLogin = true
        };

        var roles = new List<string> { "EmployeeEY" };

        _mockUserManager.Setup(x => x.FindByEmailAsync(changePasswordDto.Email))
            .ReturnsAsync(user);
        _mockUserManager.Setup(x => x.CheckPasswordAsync(user, changePasswordDto.CurrentPassword))
            .ReturnsAsync(true);
        _mockUserManager.Setup(x => x.ChangePasswordAsync(user, changePasswordDto.CurrentPassword, changePasswordDto.NewPassword))
            .ReturnsAsync(IdentityResult.Success);
        _mockUserManager.Setup(x => x.UpdateSecurityStampAsync(user))
            .ReturnsAsync(IdentityResult.Success);
        _mockUserManager.Setup(x => x.UpdateAsync(user))
            .ReturnsAsync(IdentityResult.Success);
        _mockUserManager.Setup(x => x.GetRolesAsync(user))
            .ReturnsAsync(roles);

        // Act
        var result = await _authService.ChangePasswordAsync(changePasswordDto);

        // Assert
        result.Should().NotBeNull();
        result.IsActive.Should().BeTrue();
        result.IsFirstLogin.Should().BeFalse();
        result.AccessToken.Should().NotBeNullOrEmpty();
        result.RefreshToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ValidateAsync_ValidUser_ReturnsValidateResponse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Email = "test@ey.com",
            FullName = "Test User",
            Fonction = "Developer",
            Sector = "IT",
            IsActive = true,
            IsFirstLogin = false,
            SessionId = Guid.NewGuid()
        };

        var roles = new List<string> { "EmployeeEY" };

        _mockHttpContext.Setup(x => x.User)
            .Returns(_mockClaimsPrincipal.Object);

        _mockUserManager.Setup(x => x.GetUserAsync(_mockClaimsPrincipal.Object))
            .ReturnsAsync(user);
        _mockUserManager.Setup(x => x.GetRolesAsync(user))
            .ReturnsAsync(roles);

        // Act
        var result = await _authService.ValidateAsync();

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(userId);
        result.Email.Should().Be(user.Email);
        result.FullName.Should().Be(user.FullName);
        result.Roles.Should().Contain("EmployeeEY");
    }


    [Fact]
    public async Task LogoutAsync_ValidUser_ClearsUserSession()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            SessionId = Guid.NewGuid(),
            RefreshToken = "some-token"
        };

        _mockHttpContext.Setup(x => x.User)
            .Returns(_mockClaimsPrincipal.Object);

        _mockUserManager.Setup(x => x.GetUserAsync(_mockClaimsPrincipal.Object))
            .ReturnsAsync(user);
        _mockUserManager.Setup(x => x.UpdateAsync(user))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        await _authService.LogoutAsync();

        // Assert
        _mockUserManager.Verify(x => x.UpdateAsync(It.Is<User>(u =>
            u.SessionId == null &&
            u.RefreshToken == null &&
            u.RefreshTokenExpiry == null)), Times.Once);
    }
}