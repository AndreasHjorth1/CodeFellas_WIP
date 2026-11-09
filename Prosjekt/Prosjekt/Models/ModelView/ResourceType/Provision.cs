using System.ComponentModel.DataAnnotations;

namespace Prosjekt.Models.ModelView.ResourceType
{
    public record ProvisionViewModel : ResourceViewModel
    {
        public enum ProvisionType
        {
            [Display(Name = "Vann (liter)")]
            Water,
            [Display(Name = "Mat (1 porsjon)")]
            FoodOnePortion
        }

        [Display(Name = "Provisjonstype")]
        public ProvisionType Type { get; init; } = ProvisionType.FoodOnePortion;

        public ProvisionViewModel()
        {
            Category = ResourceType.Provision;
        }
    }
}