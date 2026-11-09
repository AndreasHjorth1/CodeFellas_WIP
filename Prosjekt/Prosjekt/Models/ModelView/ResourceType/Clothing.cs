using System.ComponentModel.DataAnnotations;

namespace Prosjekt.Models.ModelView.ResourceType
{
    public record ClothingViewModel : ResourceViewModel
    {
        public enum ClothingSize
        {
            [Display(Name = "Ikke valgt")]
            Undefined = 0,
            XS = 1,
            S = 2,
            M = 3,
            L = 4,
            XL = 5,
            XXL = 6
        }

        [Display(Name = "Størrelse")]
        public ClothingSize Size { get; init; } = ClothingSize.M;

        public ClothingViewModel()
        {
            Category = ResourceType.Clothing;
        }
    }
}