using System.ComponentModel.DataAnnotations;

namespace Prosjekt.Models.ModelView.ResourceType
{
    public record MaterialsViewModel : ResourceViewModel
    {
        public enum MaterialType
        {
            [Display(Name = "Annet")]
            Other = 0,
            [Display(Name = "Treverk (meter)")]
            Wood = 1,
            [Display(Name = "Metall (kg)")]
            Metal = 2,
            [Display(Name = "Plast (kg)")]
            Plastic = 3,
            [Display(Name = "Glass (m*2)")]
            Glass = 4,
            [Display(Name = "Grus (Kg)")]
            Gravel = 5,
            [Display(Name = "Stein (Kg)")]
            Stone = 6,
            [Display(Name = "Jord (Kg)")]
            Soil = 7,
            [Display(Name = "Sand (Kg)")]
            Sand = 8
        }

        [Display(Name = "Materialtype")]
        public MaterialType Type { get; init; } = MaterialType.Other;

        public MaterialsViewModel()
        {
            Category = ResourceType.Materials;
        }
    }
}