using Prosjekt.Services;

namespace Prosjekt.Xunit.Test.Fakes
{
    public class FakeStedsoppslag : IStedsoppslag
    {
        public Sted? Svar { get; set; } = new Sted("Agder", "Kristiansand");

        public Task<Sted?> FinnStedAsync(double latitude, double longitude)
        {
            return Task.FromResult(Svar);
        }
    }
}
