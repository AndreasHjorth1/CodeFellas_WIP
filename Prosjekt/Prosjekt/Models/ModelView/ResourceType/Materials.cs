using System.ComponentModel.DataAnnotations;

namespace Prosjekt.Models.ModelView.ResourceType
{
    public class MaterialsViewModel : ResourceViewModel
    {
        public enum MaterialType
        {
            [Display(Name = "Treverk (meter)")]
            Wood,
            [Display(Name = "Metall (kg)")]
            Metal,
            [Display(Name = "Plast (kg)")]
            Plastic,
            [Display(Name = "Glass (m*2)")]
            Glass,
            [Display(Name = "Grus (Kg)")]
            Gravel,
            [Display(Name = "Stein (Kg)")]
            Stone,
            [Display(Name = "Jord (Kg)")]
            Soil,
            [Display(Name = "Sand (Kg)")]
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