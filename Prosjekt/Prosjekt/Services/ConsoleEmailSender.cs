using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using System.Net;

namespace Prosjekt.Services
{
    /// <summary>
    /// Skriver e-post til loggen i stedet for å sende den, så alle som kan lese loggen kan bruke passordlenkene.
    /// </summary>
    public sealed class ConsoleEmailSender(ILogger<ConsoleEmailSender> logger) : IEmailSender
    {
        public Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            logger.LogInformation(
                "Development email to {Email}. Subject: {Subject}. Body: {Body}",
                email,
                subject,
                WebUtility.HtmlDecode(htmlMessage));
            return Task.CompletedTask;
        }
    }
}
