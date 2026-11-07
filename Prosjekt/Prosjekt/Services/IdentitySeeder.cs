using Microsoft.AspNetCore.Identity;
using Prosjekt.Models.Authorization;

namespace Prosjekt.Services
{
    /// <summary>
    /// Oppretter admin-rollen og to testbrukere ved oppstart, hvis de ikke finnes.
    /// Testbrukerne har ikke passord før det settes med «Glemt passord».
    /// </summary>
    public class IdentitySeeder
    {
        public const string DevelopmentAdminEmail = "admin@heimevernet.local";
        public const string DevelopmentUserEmail = "user@heimevernet.local";
        public const string DevelopmentCoordinatorEmail = "koordinator@heimevernet.local";

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

        public static async Task SeedDevelopmentAdminAsync(UserManager<IdentityUser> userManager)
        {
            var user = await FindOrCreateDevelopmentUserAsync(userManager, DevelopmentAdminEmail);

            if (!await userManager.IsInRoleAsync(user, RoleNames.Admin))
            {
                await EnsureSucceededAsync(
                    userManager.AddToRoleAsync(user, RoleNames.Admin),
                    $"Could not add the development administrator to the {RoleNames.Admin} role.");
            }
        }

        public static async Task SeedDevelopmentCoordinatorAsync(UserManager<IdentityUser> userManager)
        {
            var user = await FindOrCreateDevelopmentUserAsync(userManager, DevelopmentCoordinatorEmail);

            if (!await userManager.IsInRoleAsync(user, RoleNames.Coordinator))
            {
                await EnsureSucceededAsync(
                    userManager.AddToRoleAsync(user, RoleNames.Coordinator),
                    $"Could not add the development coordinator to the {RoleNames.Coordinator} role.");
            }
        }

        public static async Task<IdentityUser> SeedDevelopmentUserAsync(UserManager<IdentityUser> userManager)
        {
            return await FindOrCreateDevelopmentUserAsync(userManager, DevelopmentUserEmail);
        }

        private static async Task<IdentityUser> FindOrCreateDevelopmentUserAsync(
            UserManager<IdentityUser> userManager,
            string email)
        {
            var user = await userManager.FindByEmailAsync(email);
            if (user is null)
            {
                user = new IdentityUser
                {
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true
                };

                await EnsureSucceededAsync(
                    userManager.CreateAsync(user),
                    "Could not create the development user.");
            }

            return user;
        }

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
