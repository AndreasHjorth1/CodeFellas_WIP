using System.ComponentModel.DataAnnotations;

namespace Prosjekt.Models.ModelView.ResourceType
{
    public record ToolViewModel : ResourceViewModel
    {
        public enum ToolType
        {
            [Display(Name = "Annet")]
            Other = 0,
            [Display(Name = "Håndverktøy")]
            HandTool = 1,
            [Display(Name = "Elektroverktøy")]
            PowerTool = 2,
            [Display(Name = "Hageverktøy")]
            GardenTool = 3,
            [Display(Name = "Måleverktøy")]
            MeasuringTool = 4
        }

        [Display(Name = "Verktøystype")]
        public ToolType Type { get; init; } = ToolType.HandTool;

        public ToolViewModel()
        {
            Category = ResourceType.Tool;
        }
    }
}