using Prosjekt.Models.ModelView;

namespace Prosjekt.Utils
{
    /// <summary>
    /// Oversetter mellom behovskategorier og ressurskategorier.
    /// Midlertidig løsning til de to listene eventuelt slås sammen.
    /// </summary>
    public static class CategoryMatching
    {
        private static readonly Dictionary<NeedViewModel.NeedType, ResourceViewModel.ResourceType[]> Map = new()
        {
            [NeedViewModel.NeedType.Food] = [ResourceViewModel.ResourceType.Provision],
            [NeedViewModel.NeedType.Medicine] = [ResourceViewModel.ResourceType.Provision],
            [NeedViewModel.NeedType.Transport] = [ResourceViewModel.ResourceType.Vehicle],
            [NeedViewModel.NeedType.PowerGenerator] = [ResourceViewModel.ResourceType.Tool],
            [NeedViewModel.NeedType.SnowRemoval] = [ResourceViewModel.ResourceType.Vehicle, ResourceViewModel.ResourceType.Tool],
            [NeedViewModel.NeedType.SandGravel] = [ResourceViewModel.ResourceType.Materials],
            [NeedViewModel.NeedType.Machinery] = [ResourceViewModel.ResourceType.Tool, ResourceViewModel.ResourceType.Vehicle],
            [NeedViewModel.NeedType.Evacuation] = [ResourceViewModel.ResourceType.Vehicle],
            [NeedViewModel.NeedType.Communication] = [ResourceViewModel.ResourceType.Tool],
            [NeedViewModel.NeedType.Personnel] = [ResourceViewModel.ResourceType.Competence],
        };

        /// <summary>
        /// Ressurskategoriene som kan dekke behovstypen. Typer uten oppføring, som Annet, kan dekkes av alle.
        /// </summary>
        public static ResourceViewModel.ResourceType[] MatchingResources(NeedViewModel.NeedType needType)
            => Map.TryGetValue(needType, out var types)
                ? types
                : Enum.GetValues<ResourceViewModel.ResourceType>();
    }
}
