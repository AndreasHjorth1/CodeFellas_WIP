using System.ComponentModel.DataAnnotations;

namespace Prosjekt.Models.ModelView.ResourceType
{
    public record ClothingViewModel : ResourceViewModel
    {
        public enum ClothingSize
        { 
            XS,
            S,
            M,
            L,
            XL,
            XXL
        }

        [Display(Name = "Størrelse")]
        public ClothingSize Size { get; init; } = ClothingSize.M;

        public ClothingViewModel()
        {
            Category = ResourceType.Clothing;
        }
    }
}