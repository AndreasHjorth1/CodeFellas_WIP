using Microsoft.EntityFrameworkCore;
using Prosjekt.Models.Entities;

namespace Prosjekt.DataAccess
{
    public class ApplicationDbContext : DbContext
    {
        /// <summary>
        /// Initializes a new ApplicationDbContext with the specified DbContextOptions.
        /// </summary>
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<GeoChange> GeoChanges { get; set; }

        // Her er tabellen for ressurser 
        public DbSet<Resource> Resources { get; set; }

        // Her er tabellen for need / behov
        public DbSet<Need> Needs { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Konfigurasjon for GeoChange
            modelBuilder.Entity<GeoChange>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.ComplexProperty(e => e.Coords, coord =>
                {
                    coord.Property(c => c.Latitude).HasColumnName("Latitude").IsRequired();
                    coord.Property(c => c.Longitude).HasColumnName("Longitude").IsRequired();
                });
                entity.Property(e => e.Description);
                entity.Property(e => e.ChangeTypes);
                entity.Property(e => e.Radius);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            });

            // Konfigurasjon for Resource
            modelBuilder.Entity<Resource>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Description).HasMaxLength(4000);
                entity.Property(e => e.Category).HasMaxLength(100);
                entity.Property(e => e.PhoneNumber).HasMaxLength(20);
                entity.Property(e => e.IsAvailable).HasDefaultValue(true);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

                // Mappet til koordinat-strukturen på samme måte som GeoChange
                entity.ComplexProperty(e => e.Coords, coord =>
                {
                    coord.Property(c => c.Latitude).HasColumnName("Latitude").IsRequired();
                    coord.Property(c => c.Longitude).HasColumnName("Longitude").IsRequired();
                });
            });


            // Konfigurasjon for Need
            modelBuilder.Entity<Need>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Description).HasMaxLength(4000);
                entity.Property(e => e.Category).HasMaxLength(50);
                entity.Property(e => e.PhoneNumber).HasMaxLength(20);
                entity.Property(e => e.Priority).HasMaxLength(50);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.ComplexProperty(e => e.Coords, coord =>
                {
                    coord.Property(c => c.Latitude).HasColumnName("Latitude").IsRequired();
                    coord.Property(c => c.Longitude).HasColumnName("Longitude").IsRequired();
                });
            });
        }
    }
}