var builder = DistributedApplication.CreateBuilder(args);

var mariadb = builder.AddMySql("mariadbcontainer", password: builder.AddParameter("password", secret: true))
                     .WithImage("mariadb", "latest")
                     .WithDataVolume();

var Mapdb = mariadb.AddDatabase("Mapdb");

builder.AddDockerfile(
    "Prosjekt",
    "..",
    "Prosjekt/Dockerfile")
    .WithHttpEndpoint(
        port: 5027,
        targetPort: 8080)
    .WithReference(Mapdb);

builder.Build().Run();