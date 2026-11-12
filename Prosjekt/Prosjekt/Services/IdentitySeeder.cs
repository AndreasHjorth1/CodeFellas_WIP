using Microsoft.AspNetCore.Identity;
using Prosjekt.Models.Authorization;
using Prosjekt.Models.Entities;

namespace Prosjekt.Services
{
    /// <summary>
    /// Oppretter rollene og tre testbrukere ved oppstart, hvis de ikke finnes.
    /// Testbrukerne får passordet i <see cref="DevelopmentPassword"/>, og seedes bare ved lokal kjøring.
    /// </summary>
    public class IdentitySeeder
    {
        public const string DevelopmentAdminPhone = "98765432";
        public const string DevelopmentUserPhone = "41234567";
        public const string DevelopmentCoordinatorPhone = "92345678";
        public const string DevelopmentAdminEmail = "admin@beredt.local";
        public const string DevelopmentUserEmail = "user@beredt.local";
        public const string DevelopmentCoordinatorEmail = "koordinator@beredt.local";

        /// <summary>
        /// Felles passord for alle testbrukerne. Brukes bare ved lokal kjøring.
        /// </summary>
        public const string DevelopmentPassword = "Demo2026!";

        public static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
        {
            foreach (var role in new[] { RoleNames.Admin, RoleNames.Coordinator })
            {
                if (await roleManager.RoleExistsAsync(role))
                {
                    continue;
                }

                var result = await roleManager.CreateAsync(new IdentityRole(role));
                if (!result.Succeeded)
                {
                    throw new InvalidOperationException(
                        $"Could not create the {role} role: {string.Join(", ", result.Errors.Select(error =>
        error.Description))}");
                }
            }
        }

        public static async Task SeedDevelopmentAdminAsync(UserManager<ApplicationUser> userManager)
        {
            var user = await FindOrCreateDevelopmentUserAsync(userManager, DevelopmentAdminEmail);

            if (!await userManager.IsInRoleAsync(user, RoleNames.Admin))
            {
                await EnsureSucceededAsync(
                    userManager.AddToRoleAsync(user, RoleNames.Admin),
                    $"Could not add the development administrator to the {RoleNames.Admin} role.");
            }
        }

        public static async Task SeedDevelopmentCoordinatorAsync(UserManager<ApplicationUser> userManager)
        {
            var user = await FindOrCreateDevelopmentUserAsync(userManager, DevelopmentCoordinatorEmail);

            if (!await userManager.IsInRoleAsync(user, RoleNames.Coordinator))
            {
                await EnsureSucceededAsync(
                    userManager.AddToRoleAsync(user, RoleNames.Coordinator),
                    $"Could not add the development coordinator to the {RoleNames.Coordinator} role.");
            }
        }

        public static async Task<ApplicationUser> SeedDevelopmentUserAsync(UserManager<ApplicationUser> userManager)
        {
            return await FindOrCreateDevelopmentUserAsync(userManager, DevelopmentUserEmail);
        }

        private static async Task<ApplicationUser> FindOrCreateDevelopmentUserAsync(
            UserManager<ApplicationUser> userManager,
            string email)
        {
            var user = await userManager.FindByEmailAsync(email);

            if (user is null)
            {
                var (name, phone) = DevelopmentProfile(email);

                user = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    FullName = name,
                    PhoneNumber = phone,
                    EmailConfirmed = true
                };

                await EnsureSucceededAsync(
                    userManager.CreateAsync(user, DevelopmentPassword),
                    "Could not create the development user.");
            }
            else if (!await userManager.HasPasswordAsync(user))
            {
                await EnsureSucceededAsync(
                    userManager.AddPasswordAsync(user, DevelopmentPassword),
                    "Could not set a password on the development user.");
            }

            return user;
        }

        /// <summary>
        /// Navn og telefonnummer til testbrukerne.
        /// </summary>
        private static (string Name, string Phone) DevelopmentProfile(string email) => email switch
        {
            DevelopmentAdminEmail => ("Demo Admin", DevelopmentAdminPhone),
            DevelopmentCoordinatorEmail => ("Demo Koordinator", DevelopmentCoordinatorPhone),
            _ => ("Demo Bruker", DevelopmentUserPhone)
        };

        private static async Task EnsureSucceededAsync(Task<IdentityResult> operation, string failureMessage)
        {
            var result = await operation;
            if (!result.Succeeded)
            {
                throw new InvalidOperationException(
                    $"{failureMessage} {string.Join(", ", result.Errors.Select(error => error.Description))}");
            }
        }
    }
}
