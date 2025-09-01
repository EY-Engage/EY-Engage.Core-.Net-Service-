using System;
using System.Net;
using System.Net.Http;
using System.Net.Mail;
using EYEngage.Core.Application.InterfacesServices;
using Microsoft.Extensions.Configuration;

namespace EYEngage.Core.Application.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendUserCredentials(string email, string password)
        {
            // Récupération de la section SmtpSettings
            var smtpSection = _configuration.GetSection("SmtpSettings");
            var host = smtpSection["Server"]!;
            var port = int.Parse(smtpSection["Port"]!);
            var username = smtpSection["Username"]!;
            var smtpPass = smtpSection["Password"]!;
            var enableSsl = bool.Parse(smtpSection["EnableSsl"]!);
            var timeout = int.Parse(smtpSection["Timeout"]!);
            var fromName = smtpSection["FromName"]!;
            var fromEmail = smtpSection["FromEmail"]!;

            // URL de votre front
            var frontendUrl = _configuration["FrontendUrl"]!;

            using var smtpClient = new SmtpClient(host, port)
            {
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(username, smtpPass),
                EnableSsl = enableSsl,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                Timeout = timeout
            };

            // Construction du mail
            var mail = new MailMessage(
                new MailAddress(fromEmail, fromName),
                new MailAddress(email))
            {
                Subject = "Vos identifiants EY Engage",
                Body = GenerateEmailBody(email, password, frontendUrl),
                IsBodyHtml = true
            };

            try
            {
                await smtpClient.SendMailAsync(mail);
            }
            catch (SmtpException ex)
            {
                throw new EmailException("Erreur d'envoi d'email : " + ex.Message, ex);
            }
        }
        public async Task SendEmailAsync(string to, string subject, string htmlBody)
        {
            // Récupération de la section SmtpSettings
            var smtpSection = _configuration.GetSection("SmtpSettings");
            var host = smtpSection["Server"]!;
            var port = int.Parse(smtpSection["Port"]!);
            var username = smtpSection["Username"]!;
            var smtpPass = smtpSection["Password"]!;
            var enableSsl = bool.Parse(smtpSection["EnableSsl"]!);
            var timeout = int.Parse(smtpSection["Timeout"]!);
            var fromName = smtpSection["FromName"]!;
            var fromEmail = smtpSection["FromEmail"]!;

            using var smtpClient = new SmtpClient(host, port)
            {
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(username, smtpPass),
                EnableSsl = enableSsl,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                Timeout = timeout
            };

            var mail = new MailMessage(
                new MailAddress(fromEmail, fromName),
                new MailAddress(to)
            )
            {
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true
            };

            try
            {
                await smtpClient.SendMailAsync(mail);
            }
            catch (SmtpException ex)
            {
                throw new EmailException($"Erreur d'envoi d'email : {ex.Message}", ex);
            }
        }
        public async Task SendPasswordResetEmailAsync(string email, string resetLink)
        {
            var smtpSection = _configuration.GetSection("SmtpSettings");
            var host = smtpSection["Server"]!;
            var port = int.Parse(smtpSection["Port"]!);
            var username = smtpSection["Username"]!;
            var smtpPass = smtpSection["Password"]!;
            var enableSsl = bool.Parse(smtpSection["EnableSsl"]!);
            var timeout = int.Parse(smtpSection["Timeout"]!);
            var fromName = smtpSection["FromName"]!;
            var fromEmail = smtpSection["FromEmail"]!;

            // URL de votre front
            var frontendUrl = _configuration["FrontendUrl"]!;

            using var smtpClient = new SmtpClient(host, port)
            {
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(username, smtpPass),
                EnableSsl = enableSsl,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                Timeout = timeout
            };

            var mail = new MailMessage(
                new MailAddress(fromEmail, fromName),
                new MailAddress(email))
            {
                Subject = "Réinitialisation de mot de passe - EY Engage",
                Body = GeneratePasswordResetEmailBody(resetLink),
                IsBodyHtml = true
            };

            await smtpClient.SendMailAsync(mail);
        }

        private string GeneratePasswordResetEmailBody(string resetLink)
        {
            return $@"
    <!DOCTYPE html>
    <html>
    <body style=""font-family: Arial, sans-serif;"">
        <h2>Réinitialisation de votre mot de passe</h2>
        <p>Cliquez sur le lien ci-dessous pour réinitialiser votre mot de passe :</p>
        <p><a href=""{resetLink}"">Réinitialiser mon mot de passe</a></p>
        <p><em>Ce lien expirera dans 24 heures.</em></p>
        <hr>
        <p>Si vous n'avez pas demandé cette réinitialisation, veuillez ignorer cet email.</p>
    </body>
    </html>";
        }
        private string GenerateEmailBody(string email, string password, string frontendUrl)
        {
            return $@"
<!DOCTYPE html>
<html>
  <body style=""font-family: Arial, sans-serif; line-height: 1.6; margin:0; padding:0; background-color:#f5f5f5;"">
    <table width=""100%"" cellpadding=""0"" cellspacing=""0"">
      <tr>
        <td align=""center"">
          <table width=""600"" cellpadding=""0"" cellspacing=""0"" style=""background-color:#ffffff; margin:20px 0; border-radius:8px; overflow:hidden; box-shadow:0 2px 8px rgba(0,0,0,0.1);"">
            <tr>
              <td style=""background-color:#000; padding:20px; text-align:center;"">
                <img src=""https://example.com/ey-logo.png"" alt=""EY Engage"" width=""120"" style=""display:block; margin:0 auto;""/>
              </td>
            </tr>
            <tr>
              <td style=""padding:30px; color:#000;"">
                <h1 style=""margin-top:0; color:#f5c500; font-size:24px;"">Bienvenue sur EY Engage</h1>
                <p style=""font-size:16px;"">Voici vos identifiants temporaires :</p>
                <ul style=""font-size:16px; padding-left:20px;"">
                  <li><strong>Email :</strong> {WebUtility.HtmlEncode(email)}</li>
                  <li><strong>Mot de passe :</strong> {WebUtility.HtmlEncode(password)}</li>
                </ul>
                <p style=""font-size:16px;"">
                  Pour des raisons de sécurité, vous devrez changer ce mot de passe lors de votre première connexion.
                </p>
                <p style=""text-align:center; margin:30px 0;"">
                  <a href=""{frontendUrl}/auth"" 
                     style=""display:inline-block; padding:12px 24px; background-color:#f5c500; color:#000; text-decoration:none; font-weight:bold; border-radius:4px;"">
                    Accéder à la plateforme
                  </a>
                </p>
                <hr style=""border:none; border-top:1px solid #eee; margin:30px 0;""/>
                <p style=""font-size:14px; color:#666;"">
                  Cordialement,<br/>
                  L'équipe EY Engage
                </p>
              </td>
            </tr>
          </table>
        </td>
      </tr>
    </table>
  </body>
</html>";
        }

        public class EmailException : Exception
        {
            public EmailException(string message, Exception inner)
                : base(message, inner) { }
        }
    }
}