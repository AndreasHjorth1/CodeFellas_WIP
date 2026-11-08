using System;
using System.ComponentModel.DataAnnotations;
using Prosjekt.Utils;
using Prosjekt.Models.Enums;

namespace Prosjekt.Models.Entities
{
    public class Need
    {
        public int Id { get; init; }

        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [StringLength(4000)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Category { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string PhoneNumber { get; set; } = string.Empty;

        /// <summary>
        /// Navnet på en ImportanceLevel, altså Acute eller Planned.
        /// </summary>
        [StringLength(50)]
        public string Priority { get; set; } = "Medium";

        public DateTime? NeededBy { get; set; }

        /// <summary>
        /// Hvor i statusflyten behovet er. Ny => Vurderes => Tildelt => Løst
        /// </summary>
        public EntryStatus Status {get; set;} = EntryStatus.New;

        public Coordinates Coords { get; set; } = new Coordinates(0, 0);

        public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

        /// <summary>
        /// Område tegnet i kartet. Null når brukeren bare satte en markør.
        /// </summary>
        public string? AreaGeoJson { get; set; }
        public double? AreaSquareMeters { get; set; }
        public double? PerimeterMeters { get; set; }
        public string? UserId { get; set; }
    }
}