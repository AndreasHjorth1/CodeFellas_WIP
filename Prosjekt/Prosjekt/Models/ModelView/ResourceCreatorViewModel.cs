using System.ComponentModel.DataAnnotations;

namespace Prosjekt.Models.ModelView
{
    public class ResourceCreatorViewModel
    {
        [Display(Name = "Breddegrad")]
        [Required(ErrorMessage = "Du må legge inn en breddegrad")]
        public double? Latitude { get; init; }


        [Display(Name = "Lengdegrad")]
        [Required(ErrorMessage = "Du må legge inn en lengdegrad")]
        public double? Longitude { get; init; }
        

        [Display(Name = "Ressurser")]
        [Required(ErrorMessage = "Du må legge inn minst en ressurs")]
        public List<ResourceViewModel> ResourceList { get; init; } = new List<ResourceViewModel>();
    }
}