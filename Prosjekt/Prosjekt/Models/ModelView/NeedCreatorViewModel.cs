using System.ComponentModel.DataAnnotations;

namespace Prosjekt.Models.ModelView
{
    public enum ImportanceLevel
    {
        [Display(Name = "Akutt behov")]
        Acute,
        [Display(Name = "Planlagt behov")]
        Planned,
    }
    public class NeedCreatorViewModel
    {
        [Required(ErrorMessage = "Du må legge til minst ett behov.")]
        public List<NeedViewModel> NeedList { get; set; } = new List<NeedViewModel>();

        public ImportanceLevel Importance { get; init; } = ImportanceLevel.Planned;

        public bool IsFulfilled { get; init; } = false;


        [Display(Name = "Tidsfrist")]
        public DateTime? Deadline { get; init; }


        [Display(Name = "Breddegrad")]
        [Required(ErrorMessage = "Du må legge inn breddegrad")]
        public double? Latitude { get; init; }


        [Display(Name = "Lengdegrad")]
        [Required(ErrorMessage = "Du må legge inn lengdegrad")]
        public double? Longitude { get; init; }

        // Område tegnet i kartet. Null når brukeren bare satte en markør.
        public string? AreaGeoJson { get; init; }

        public double? AreaSquareMeters { get; init; }

        public double? PerimeterMeters { get; init; }

    }
}
