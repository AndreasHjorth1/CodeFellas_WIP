using System.ComponentModel.DataAnnotations;

namespace Prosjekt.Models.ModelView.ResourceType
{
    public class ProvisionViewModel : ResourceViewModel
    {
        public enum ProvisionType
        {
            [Display(Name = "Vann")]
            Water,
            [Display(Name = "Mat (1 porsjon)")]
            FoodOnePortion
        }

        [Display(Name = "Provisjonstype")]
        public ProvisionType Type { get; set; } = ProvisionType.FoodOnePortion;

        public ProvisionViewModel()
        {
            Category = ResourceType.Provision;
        }
    }
}