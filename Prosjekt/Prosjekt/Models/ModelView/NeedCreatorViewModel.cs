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

        public ImportanceLevel Importance { get; set; } = ImportanceLevel.Planned;

        public bool IsFulfilled { get; set; } = false;


        [Display(Name = "Tidsfrist")]
        public DateTime? Deadline { get; set; }


        [Display(Name = "Breddegrad")]
        [Required(ErrorMessage = "Du må legge inn breddegrad")]
        public double? Latitude { get; set; }


        [Display(Name = "Lengdegrad")]
        [Required(ErrorMessage = "Du må legge inn lengdegrad")]
        public double? Longitude { get; set; }

        // Område tegnet i kartet. Null når brukeren bare satte en markør.
        public string? AreaGeoJson { get; set; }

        public double? AreaSquareMeters { get; set; }

        public double? PerimeterMeters { get; set; }

    }
}
