using Prosjekt.Models.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Prosjekt.DataAccess.Repositories
{
    /// <summary>
    /// Lagrer og henter behov. Oppretting, statusendring og sletting skrives til revisjonsloggen.
    /// </summary>
    public interface INeedRepository
    {
        /// <summary>
        /// Henter alle behov, også løste.
        /// </summary>
        Task<IEnumerable<Need>> GetAllAsync();

        /// <summary>
        /// Henter behovet med gitt id, eller null hvis det ikke finnes.
        /// </summary>
        Task<Need?> GetByIdAsync(int id);

        /// <summary>
        /// Lagrer et nytt behov og logger det i revisjonsloggen, i samme transaksjon.
        /// Id-en settes av databasen og finnes på objektet etterpå.
        /// </summary>
        Task AddAsync(Need need);

        /// <summary>
        /// Lagrer endringene på et eksisterende behov. En statusendring logges med gammel og ny verdi.
        /// Kaster InvalidOperationException hvis behovet ikke finnes.
        /// </summary>
        Task UpdateAsync(Need need);

        /// <summary>
        /// Sletter behovet og logger det. Gjør ingenting hvis id-en ikke finnes.
        /// Tildelingene slettes ikke, så kall IAllocationRepository.DeleteByNeedAsync først.
        /// </summary>
        Task DeleteAsync(int id);
    }
}
