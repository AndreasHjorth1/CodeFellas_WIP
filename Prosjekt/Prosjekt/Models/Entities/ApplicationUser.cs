using Microsoft.AspNetCore.Identity;

namespace Prosjekt.Models.Entities
{
    /// <summary>
    /// Brukeren i appen. Arver e-post, telefonnummer, passord osv. fra IdentityUser,
    /// og legger til fullt navn. Alt ligger i tabellen AspNetUsers.
    /// </summary>
    public class ApplicationUser : IdentityUser
    {
        /// <summary>
        /// Navnet brukeren oppga ved registrering. Kan ikke endres av brukeren selv.
        /// </summary>
        public string FullName { get; set; } = string.Empty;
    }
}
