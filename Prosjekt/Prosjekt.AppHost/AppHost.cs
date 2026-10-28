var builder = DistributedApplication.CreateBuilder(args);

var mariadb = builder.AddMySql("mariadbcontainer", password: builder.AddParameter("password", secret: true))
                     .WithImage("mariadb", "latest");

var kartDb = mariadb.AddDatabase("Mapdb");

builder.AddDockerfile(
    "Prosjekt",
    "..",
    "Prosjekt/Dockerfile")
    .WithHttpEndpoint(
        port: 5027,
        targetPort: 8080)
    .WithReference(kartDb);

builder.Build().Run();