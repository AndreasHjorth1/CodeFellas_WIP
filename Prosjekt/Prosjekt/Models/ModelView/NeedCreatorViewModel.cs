using System.ComponentModel.DataAnnotations;
using Prosjekt.Models.Entities;

namespace Prosjekt.Models.ModelView
{
    public class NeedCreatorViewModel
    {
        [Display(Name = "Breddegrad")]
        public double? Latitude { get; set; }

        [Display(Name = "Lengdegrad")]
        public double? Longitude { get; set; }

        [Display(Name = "Viktighet")]
        public Importance Importance { get; set; } = Importance.Low;

        [Display(Name = "Behov")]
        [Required(ErrorMessage = "Du må legge inn minst ett behov")]
        public List<NeedViewModel> NeedList { get; set; } = new List<NeedViewModel>();

    }
}
