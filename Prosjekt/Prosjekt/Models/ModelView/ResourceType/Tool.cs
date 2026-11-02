using System.ComponentModel.DataAnnotations;

namespace Prosjekt.Models.ModelView.ResourceType
{
    public class ToolViewModel : ResourceViewModel
    {
        public enum ToolType
        {
            [Display(Name = "Håndverktøy")]
            HandTool,
            [Display(Name = "Elektroverktøy")]
            PowerTool,
            [Display(Name = "Hageverktøy")]
            GardenTool,
            [Display(Name = "Måleverktøy")]
            MeasuringTool,
            [Display(Name = "Annet")]
            Other
        }

        [Display(Name = "Verktøystype")]
        public ToolType Type { get; set; } = ToolType.HandTool;

        public ToolViewModel()
        {
            Category = ResourceType.Tool;
        }
    }
}