
namespace EYEngage.Core.Application.InterfacesServices;

    public interface IEmailService
{
    Task SendUserCredentials(string email, string password);
    Task SendEmailAsync(string to, string subject, string htmlBody);
    Task SendPasswordResetEmailAsync(string email, string resetLink);
    }

