namespace Prosjekt.Services;

public record Sted(string Fylke, string Kommune);

public interface IStedsoppslag
{
    // Returnerer null hvis oppslaget feiler (f.eks. punkt i havet eller utenfor Norge)
    Task<Sted?> FinnStedAsync(double latitude, double longitude);
}
