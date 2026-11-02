using System.ComponentModel.DataAnnotations;

namespace Prosjekt.Models.ModelView
{
    public class ResourceCreatorViewModel
    {
        [Display(Name = "Breddegrad")]
        public double? Latitude { get; init; }


        [Display(Name = "Lengdegrad")]
        public double? Longitude { get; init; }
        

        [Display(Name = "Ressurser")]
        [Required(ErrorMessage = "Du må legge inn minst en ressurs")]
        public List<ResourceViewModel> ResourceList { get; init; } = new List<ResourceViewModel>();
    }
}