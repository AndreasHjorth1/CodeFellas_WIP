using System.ComponentModel.DataAnnotations;
using Prosjekt.Models.Enums;

namespace Prosjekt.Models.ModelView
{
    public class NeedViewModel
    {

        public int Id { get; set; }

        [Display(Name = "Navn")]
        [Required(ErrorMessage = "Navn må fylles ut.")]
        public string Name { get; init; } = string.Empty;

        [Display(Name = "Telefonnummer")]
        [Required(ErrorMessage = "Telefonnummer må fylles ut.")]
        public string Phone { get; init; } = string.Empty;

        [Display(Name = "Beskrivelse")]
        [Required(ErrorMessage = "Beskrivelse må fylles ut.")]
        public string Description { get; init; } = string.Empty;

        public enum NeedType
        {
            [Display(Name = "Mat/Vann")]
            Food,

            [Display(Name = "Medisiner")]
            Medicine,

            [Display(Name = "Transport")]
            Transport,

            [Display(Name = "Strøm/aggregat")]
            PowerGenerator,

            [Display(Name = "Snørydding")]
            SnowRemoval,

            [Display(Name = "Sand/grus")]
            SandGravel,

            [Display(Name = "Maskiner")]
            Machinery,

            [Display(Name = "Evakuering")]
            Evacuation,

            [Display(Name = "Samband")]
            Communication,

            [Display(Name = "Mannskap")]
            Personnel,

            [Display(Name = "Annet")]
            Other
        }

        [Display(Name = "Behovstype")]
        public NeedType Type { get; init; } = NeedType.Other;

        [Display(Name = "Tidsfrist")]
        [Required(ErrorMessage = "Et tidspunkt må registreres")]
        public DateTime? Deadline { get; init; }

        [Display(Name = "Viktighet")]
        public ImportanceLevel Importance { get; init; }

        [Display(Name = "Status")]
        public EntryStatus Status {get; init;} = EntryStatus.New;

        public double? Latitude { get; set; }

        public double? Longitude { get; set; }

        public string? AreaGeoJson { get; init; }

        public double? AreaSquareMeters { get; init; }

        public double? PerimeterMeters { get; init; }
    }
}