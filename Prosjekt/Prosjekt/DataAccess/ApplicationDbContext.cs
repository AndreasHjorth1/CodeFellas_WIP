using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Prosjekt.Models.Entities;

namespace Prosjekt.DataAccess
{
    public class ApplicationDbContext : IdentityDbContext<IdentityUser>
    {

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<Resource> Resources { get; set; }

        public DbSet<Need> Needs { get; set; }

        public DbSet<Allocation> Allocations { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Resource>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Description).HasMaxLength(4000);
                entity.Property(e => e.Category).HasMaxLength(100);
                entity.Property(e => e.PhoneNumber).HasMaxLength(20);
                entity.Property(e => e.IsAvailable).HasDefaultValue(true);

                entity.HasOne<IdentityUser>()
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.ComplexProperty(e => e.Coords, coord =>
                {
                    coord.Property(c => c.Latitude).HasColumnName("Latitude").IsRequired();
                    coord.Property(c => c.Longitude).HasColumnName("Longitude").IsRequired();
                });

            });

            modelBuilder.Entity<Need>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Description).HasMaxLength(4000);
                entity.Property(e => e.Category).HasMaxLength(50);
                entity.Property(e => e.PhoneNumber).HasMaxLength(20);
                entity.Property(e => e.Priority).HasMaxLength(50);

                entity.HasOne<IdentityUser>()
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.ComplexProperty(e => e.Coords, coord =>
                {
                    coord.Property(c => c.Latitude).HasColumnName("Latitude").IsRequired();
                    coord.Property(c => c.Longitude).HasColumnName("Longitude").IsRequired();
                });
            });


        }

    }
}


