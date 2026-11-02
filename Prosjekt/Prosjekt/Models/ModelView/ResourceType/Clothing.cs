using System.ComponentModel.DataAnnotations;

namespace Prosjekt.Models.ModelView.ResourceType
{
    public class ClothingViewModel : ResourceViewModel
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
        public ClothingSize Size { get; set; } = ClothingSize.M;

        public ClothingViewModel()
        {
            Category = ResourceType.Clothing;
        }
    }
}