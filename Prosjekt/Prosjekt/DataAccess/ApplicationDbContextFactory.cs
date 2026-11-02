using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Prosjekt.DataAccess
{
    public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext(string[] args)
        {

            var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Mapdb")
                ?? "Server=localhost;Database=Mapdb;User=root;Password=;";

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
               .UseMySql(connectionString, ServerVersion.Parse("11.4.0-mariadb"))
               .Options;

            return new ApplicationDbContext(options);
        }
    }
}