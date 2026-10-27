using System.ComponentModel.DataAnnotations;

namespace Prosjekt.Models.ModelView.ResourceType
{
    public class MaterialsViewModel : ResourceViewModel
    {
        public enum MaterialType
        {
            [Display(Name = "Treverk")]
            Wood,
            [Display(Name = "Metall")]
            Metal,
            [Display(Name = "Plast")]
            Plastic,
            [Display(Name = "Glass")]
            Glass,
            [Display(Name = "Grus")]
            Gravel,
            [Display(Name = "Stein")]
            Stone,
            [Display(Name = "Jord")]
            Soil,
            [Display(Name = "Sand")]
            Sand,
            [Display(Name = "Annet")]
            Other
        }

        [Display(Name = "Materialtype")]
        public MaterialType Type { get; set; } = MaterialType.Other;

        public MaterialsViewModel()
        {
            Category = ResourceType.Materials;
        }
    }
}