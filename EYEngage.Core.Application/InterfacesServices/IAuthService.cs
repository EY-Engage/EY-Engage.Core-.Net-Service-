using EYEngage.Core.Application.Dto.AuthDtos;

namespace EYEngage.Core.Application.InterfacesServices;

public interface IAuthService
{
    Task<string> RegisterAsync(RegisterRequestDto request);
    Task<LoginResponseDto?> LoginAsync(LoginRequestDto request);

}
