using System;
using System.Net;
using System.Net.Http;
using System.Net.Mail;
using EYEngage.Core.Application.InterfacesServices;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace EYEngage.Core.Application.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration, IWebHostEnvironment env)
        {
            _configuration = configuration;
        }
        public async Task SendUserCredentials(string email, string password)
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

            // Wrapper le contenu dans le template email avec logo
            var wrappedBody = WrapEmailContent(htmlBody);

            var mail = new MailMessage(
                new MailAddress(fromEmail, fromName),
                new MailAddress(to))
            {
                Subject = subject,
                Body = wrappedBody,
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
        private string WrapEmailContent(string content)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
</head>
<body style=""font-family: Arial, sans-serif; line-height: 1.6; margin:0; padding:0; background-color:#f5f5f5;"">
    <table width=""100%"" cellpadding=""0"" cellspacing=""0"">
        <tr>
            <td align=""center"">
                <table width=""600"" cellpadding=""0"" cellspacing=""0"" style=""background-color:#ffffff; margin:20px 0; border-radius:8px; overflow:hidden; box-shadow:0 2px 8px rgba(0,0,0,0.1);"">
                    <!-- Contenu -->
                    <tr>
                        <td style=""padding:30px;"">
                            {content}
                        </td>
                    </tr>
                    <!-- Footer -->
                    <tr>
                        <td style=""background-color:#f8f9fa; padding:20px; text-align:center; border-top:2px solid #FFE135;"">
                            <p style=""margin:0; color:#666; font-size:12px;"">
                                © {DateTime.Now.Year} EY. Tous droits réservés.<br>
                                Building a better working world
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

        private string GeneratePasswordResetEmailBody(string resetLink)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
</head>
<body style=""font-family: Arial, sans-serif; line-height: 1.6; margin:0; padding:0; background-color:#f5f5f5;"">
    <table width=""100%"" cellpadding=""0"" cellspacing=""0"">
        <tr>
            <td align=""center"">
                <table width=""600"" cellpadding=""0"" cellspacing=""0"" style=""background-color:#ffffff; margin:20px 0; border-radius:8px; overflow:hidden; box-shadow:0 2px 8px rgba(0,0,0,0.1);"">
                    <!-- Bande jaune EY -->
                    <tr>
                        <td style=""background-color:#FFE135; height:4px;""></td>
                    </tr>
                    <!-- Contenu -->
                    <tr>
                        <td style=""padding:30px;"">
                            <h2 style=""color:#2C1810; margin-top:0;"">Réinitialisation de votre mot de passe</h2>
                            <p>Vous avez demandé la réinitialisation de votre mot de passe pour votre compte EY Engage.</p>
                            <p>Cliquez sur le bouton ci-dessous pour créer un nouveau mot de passe :</p>
                            
                            <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""margin:30px 0;"">
                                <tr>
                                    <td align=""center"">
                                        <a href=""{resetLink}"" 
                                           style=""display:inline-block; padding:15px 30px; background-color:#FFE135; color:#2C1810; text-decoration:none; font-weight:bold; border-radius:4px; font-size:16px;"">
                                            Réinitialiser mon mot de passe
                                        </a>
                                    </td>
                                </tr>
                            </table>
                            
                            <p style=""color:#666; font-size:14px;"">
                                <em>Ce lien expirera dans 24 heures pour des raisons de sécurité.</em>
                            </p>
                            
                            <hr style=""border:none; border-top:1px solid #eee; margin:30px 0;""/>
                            
                            <p style=""color:#666; font-size:14px;"">
                                Si vous n'avez pas demandé cette réinitialisation, veuillez ignorer cet email. 
                                Votre mot de passe restera inchangé.
                            </p>
                            
                            <p style=""color:#666; font-size:14px;"">
                                Si le bouton ne fonctionne pas, copiez et collez ce lien dans votre navigateur :<br>
                                <a href=""{resetLink}"" style=""color:#FFE135; word-break:break-all;"">{resetLink}</a>
                            </p>
                        </td>
                    </tr>
                    <!-- Footer -->
                    <tr>
                        <td style=""background-color:#f8f9fa; padding:20px; text-align:center; border-top:2px solid #FFE135;"">
                            <p style=""margin:0; color:#666; font-size:12px;"">
                                © {DateTime.Now.Year} EY. Tous droits réservés.<br>
                                Building a better working world
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

        private string GenerateEmailBody(string email, string password, string frontendUrl)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
</head>
<body style=""font-family: Arial, sans-serif; line-height: 1.6; margin:0; padding:0; background-color:#f5f5f5;"">
    <table width=""100%"" cellpadding=""0"" cellspacing=""0"">
        <tr>
            <td align=""center"">
                <table width=""600"" cellpadding=""0"" cellspacing=""0"" style=""background-color:#ffffff; margin:20px 0; border-radius:8px; overflow:hidden; box-shadow:0 2px 8px rgba(0,0,0,0.1);"">
                    <!-- Bande jaune EY -->
                    <tr>
                        <td style=""background-color:#FFE135; height:4px;""></td>
                    </tr>
                    <!-- Contenu -->
                    <tr>
                        <td style=""padding:30px;"">
                            <h1 style=""margin-top:0; color:#2C1810; font-size:24px;"">Bienvenue sur EY Engage</h1>
                            <p style=""font-size:16px; color:#333;"">
                                Votre compte a été créé avec succès. Voici vos identifiants temporaires pour accéder à la plateforme :
                            </p>
                            
                            <div style=""background-color:#f8f9fa; padding:20px; border-left:4px solid #FFE135; margin:20px 0;"">
                                <p style=""margin:0 0 10px 0; font-size:16px;"">
                                    <strong style=""color:#2C1810;"">Email :</strong> {WebUtility.HtmlEncode(email)}
                                </p>
                                <p style=""margin:0; font-size:16px;"">
                                    <strong style=""color:#2C1810;"">Mot de passe :</strong> {WebUtility.HtmlEncode(password)}
                                </p>
                            </div>
                            
                            <div style=""background-color:#fff3cd; border:1px solid #ffc107; padding:15px; border-radius:4px; margin:20px 0;"">
                                <p style=""margin:0; color:#856404; font-size:14px;"">
                                    <strong>⚠️ Important :</strong> Pour des raisons de sécurité, vous devrez changer ce mot de passe lors de votre première connexion.
                                </p>
                            </div>
                            
                            <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""margin:30px 0;"">
                                <tr>
                                    <td align=""center"">
                                        <a href=""{frontendUrl}/auth"" 
                                           style=""display:inline-block; padding:15px 30px; background-color:#FFE135; color:#2C1810; text-decoration:none; font-weight:bold; border-radius:4px; font-size:16px;"">
                                            Accéder à la plateforme
                                        </a>
                                    </td>
                                </tr>
                            </table>
                            
                            <hr style=""border:none; border-top:1px solid #eee; margin:30px 0;""/>
                            
                            <p style=""font-size:14px; color:#666;"">
                                Si vous avez des questions ou rencontrez des difficultés pour vous connecter, 
                                n'hésitez pas à contacter notre équipe support.
                            </p>
                            
                            <p style=""font-size:14px; color:#666; margin-bottom:0;"">
                                Cordialement,<br/>
                                <strong>L'équipe EY Engage</strong>
                            </p>
                        </td>
                    </tr>
                    <!-- Footer -->
                    <tr>
                        <td style=""background-color:#f8f9fa; padding:20px; text-align:center; border-top:2px solid #FFE135;"">
                            <p style=""margin:0; color:#666; font-size:12px;"">
                                © {DateTime.Now.Year} EY. Tous droits réservés.<br>
                                Building a better working world
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