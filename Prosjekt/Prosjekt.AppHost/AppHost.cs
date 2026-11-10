var builder = DistributedApplication.CreateBuilder(args);

var mariadb = builder.AddMySql("mariadbcontainer")
                     .WithImage("mariadb", "latest")
                     .WithDataVolume();

var Mapdb = mariadb.AddDatabase("Mapdb");

var prosjekt = builder.AddDockerfile(
    "Prosjekt",
    "..",
    "Prosjekt/Dockerfile")
    .WithHttpEndpoint(
        port: 5027,
        targetPort: 8080)
    .WithReference(Mapdb);

/// <summary>
/// HTTPS brukes bare når sertifikatet finnes, så appen også starter uten.
/// Lag sertifikatet og lagre passordet én gang:
/// dotnet dev-certs https -ep "$HOME/.aspnet/https/prosjekt.pfx" -p "<passord>" --trust
/// dotnet user-secrets set "Parameters:https-passord" "<passord>" --project Prosjekt/Prosjekt.AppHost
/// </summary>
var sertifikatMappe = builder.Configuration["Https:Sertifikatmappe"]
    ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".aspnet", "https");

if (File.Exists(Path.Combine(sertifikatMappe, "prosjekt.pfx")))
{
    var sertifikatPassord = builder.AddParameter("https-passord", secret: true);

    prosjekt
        .WithHttpsEndpoint(
            port: 5028,
            targetPort: 8443)
        .WithBindMount(sertifikatMappe, "/https", isReadOnly: true)
        .WithEnvironment("ASPNETCORE_HTTPS_PORTS", "8443")
        /// <summary>
        /// Porten UseHttpsRedirection sender nettleseren til, sett utenfra containeren
        /// </summary>
        .WithEnvironment("ASPNETCORE_HTTPS_PORT", "5028")
        .WithEnvironment("Kestrel__Certificates__Default__Path", "/https/prosjekt.pfx")
        .WithEnvironment("Kestrel__Certificates__Default__Password", sertifikatPassord);
}

builder.Build().Run();
