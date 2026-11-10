using Prosjekt.Models.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Prosjekt.DataAccess.Repositories
{
    /// <summary>
    /// Lagrer og henter ressurser. Tildelinger håndteres av IAllocationRepository.
    /// </summary>
    public interface IResourceRepository
    {
        /// <summary>
        /// Henter alle ressurser, uansett eier og tilgjengelighet.
        /// </summary>
        Task<IEnumerable<Resource>> GetAllAsync();

        /// <summary>
        /// Henter ressursen med gitt id, eller null hvis den ikke finnes.
        /// </summary>
        Task<Resource?> GetByIdAsync(int id);

        /// <summary>
        /// Henter ressursene som eies av brukeren. Tom liste hvis brukeren ikke har noen.
        /// </summary>
        Task<IEnumerable<Resource>> GetByUserIdAsync(string userId);

        /// <summary>
        /// Lagrer en ny ressurs. Id-en settes av databasen og finnes på objektet etterpå.
        /// </summary>
        Task AddAsync(Resource resource);

        /// <summary>
        /// Lagrer endringene på en eksisterende ressurs.
        /// </summary>
        Task UpdateAsync(Resource resource);

        /// <summary>
        /// Sletter ressursen. Gjør ingenting hvis id-en ikke finnes.
        /// Tildelingene slettes ikke, så kall IAllocationRepository.DeleteByResourceAsync først.
        /// </summary>
        Task DeleteAsync(int id);
    }
}
