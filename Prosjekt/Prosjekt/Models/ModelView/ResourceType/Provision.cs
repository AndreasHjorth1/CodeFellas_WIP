using System.ComponentModel.DataAnnotations;

namespace Prosjekt.Models.ModelView.ResourceType
{
    public record ProvisionViewModel : ResourceViewModel
    {
        public enum ProvisionType
        {
            [Display(Name = "Ikke valgt")]
            Undefined = 0,
            [Display(Name = "Vann (liter)")]
            Water = 1,
            [Display(Name = "Mat (1 porsjon)")]
            FoodOnePortion = 2
        }

        [Display(Name = "Provisjonstype")]
        public ProvisionType Type { get; init; } = ProvisionType.FoodOnePortion;

        public ProvisionViewModel()
        {
            Category = ResourceType.Provision;
        }
    }
}