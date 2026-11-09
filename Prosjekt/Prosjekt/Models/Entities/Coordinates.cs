using System.ComponentModel.DataAnnotations.Schema;

namespace Prosjekt.Models.Entities;

/// <summary>
/// 1. CRS er fjernet fra parentesen her
/// </summary>
public readonly record struct Coordinates(double Latitude, double Longitude)
{
    /// <summary>
    /// 2. CRS ligger nå som en egen egenskap på innsiden
    /// </summary>
    [NotMapped]
    public string CRS { get; } = "EPSG:4326";

    /// <summary>
    /// Indikerer om koordinatene er gyldige. Gyldige koordinater har lat mellom -90 og 90, og lng mellom -180 og 180.
    /// </summary>
    /// <returns></returns>
    public bool IsValid() =>
        Latitude is >= -90.0 and <= 90.0 &&
        Longitude is >= -180.0 and <= 180.0;

    public override string ToString() => $"Lat: {Latitude}, Lng: {Longitude}, CRS: {CRS}";
}