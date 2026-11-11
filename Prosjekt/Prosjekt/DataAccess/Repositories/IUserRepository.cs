namespace Prosjekt.DataAccess.Repositories
{
    /// <summary>
    /// Slår opp navn på brukere, så admin og koordinator kan se hvem som eier en ressurs eller meldte et behov.
    /// </summary>
    public interface IUserRepository
    {
        /// <summary>
        /// Fullt navn for hver bruker-ID. Ukjente eller tomme ID-er er ikke med i svaret.
        /// </summary>
        Task<IReadOnlyDictionary<string, string>> GetFullNamesAsync(IEnumerable<string?> userIds);
    }
}
