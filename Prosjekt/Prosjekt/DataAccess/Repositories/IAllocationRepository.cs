using Prosjekt.Models.Entities;

namespace Prosjekt.DataAccess.Repositories
{
    /// <summary>
    /// Tildelinger kobler et behov til en ressurs med et antall.
    /// </summary>
    public interface IAllocationRepository
    {
        /// <summary>
        /// Henter alle tildelinger.
        /// </summary>
        Task<IEnumerable<Allocation>> GetAllAsync();

        /// <summary>
        /// Henter tildelingene til behovet. Tom liste hvis det ikke har noen.
        /// </summary>
        Task<IEnumerable<Allocation>> GetByNeedAsync(int needId);

        /// <summary>
        /// Lagrer en ny tildeling. Sjekker ikke om ressursen har nok ledig,
        /// og oppdaterer ikke IsAvailable på ressursen. Det må den som kaller gjøre.
        /// </summary>
        Task AddAsync(Allocation allocation);

        /// <summary>
        /// Sletter tildelingen. Gjør ingenting hvis id-en ikke finnes.
        /// </summary>
        Task DeleteAsync(int id);

        /// <summary>
        /// Sletter alle tildelingene til behovet.
        /// </summary>
        Task DeleteByNeedAsync(int needId);

        /// <summary>
        /// Sletter alle tildelingene til ressursen.
        /// </summary>
        Task DeleteByResourceAsync(int resourceId);
    }
}
