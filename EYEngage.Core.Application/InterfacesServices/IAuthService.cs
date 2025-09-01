using EYEngage.Core.Application.Dto;
using EYEngage.Core.Application.Dto.AuthDtos;

namespace EYEngage.Core.Application.InterfacesServices;

public interface IAuthService
{
    Task<LoginResponseDto?> LoginAsync(LoginRequestDto request);
    Task<TokenResponseDto> RefreshTokenAsync(string refreshToken);
    Task<LoginResponseDto> ChangePasswordAsync(ChangePasswordDto dto);
    Task<ValidateResponseDto> ValidateAsync();
     Task<UserDto> GetCurrentUserAsync();
    Task LogoutAsync();
    Task ForgotPasswordAsync(string email);
    Task ResetPasswordAsync(string email, string token, string newPassword);

}
