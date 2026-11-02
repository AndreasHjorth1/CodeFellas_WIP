using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using DotNetEnv;

namespace Prosjekt.DataAccess
{
    public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext(string[] args)
        {
            var basePath = Directory.GetCurrentDirectory();

            Env.TraversePath().Load();

            var configuration = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("appsettings.json", optional: true)
                .AddJsonFile("appsettings.Development.json", optional: true)
                .Build();

            var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING")
                ?? configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Missing database connection string. Make sure .env file exists or environment variables are set.");

            var builder = new DbContextOptionsBuilder<ApplicationDbContext>();
            var serverVersion = new MariaDbServerVersion(new Version(11, 4, 0));

            builder.UseMySql(connectionString, serverVersion);

            return new ApplicationDbContext(builder.Options);
        }
    }
}