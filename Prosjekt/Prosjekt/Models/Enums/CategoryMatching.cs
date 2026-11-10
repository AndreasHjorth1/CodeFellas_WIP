using Prosjekt.Models.ModelView;

namespace Prosjekt.Models.Enums
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

    public static class Distance
    {
        /// <summary>
        /// Kalkulerer avstanden mellom to koordinater i meter. Bruker Haversine-formelen.
        /// </summary>
        public static double InMeters(double lat1, double lng1, double lat2, double lng2)
        {
            const double R = 6371000;
            double f1 = lat1 * Math.PI / 180;
            double f2 = lat2 * Math.PI / 180;
            double dF = (lat2 - lat1) * Math.PI / 180;
            double dL = (lng2 - lng1) * Math.PI / 180;

            double a = Math.Sin(dF / 2) * Math.Sin(dF / 2)
                     + Math.Cos(f1) * Math.Cos(f2) * Math.Sin(dL / 2) * Math.Sin(dL / 2);

            return R * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        }
    }
}
