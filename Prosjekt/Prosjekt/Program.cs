using DotNetEnv;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Prosjekt.DataAccess;
using Prosjekt.DataAccess.Repositories;
using Prosjekt.Services; // Husk å ha med mappen der IdentitySeeder og ConsoleEmailSender ligger

// 1. Leser .env-filen dersom den finnes lokalt
Env.TraversePath().Load();

var builder = WebApplication.CreateBuilder(args);

// Sikrer Kestrel-konfigurasjon
builder.WebHost.ConfigureKestrel(options => options.AddServerHeader = false);

// 2. Aspire service defaults
builder.AddServiceDefaults();

// Add services to the container
builder.Services.AddControllersWithViews(options =>
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()));

// 3. Hent tilkoblingsstreng for MariaDB 
var connectionString = builder.Configuration["DB_CONNECTION_STRING"]
    ?? builder.Configuration.GetConnectionString("heimevernetdb")
    ?? builder.Configuration.GetConnectionString("Mapdb")
    ?? builder.Configuration.GetConnectionString("kartdb")
    ?? builder.Configuration.GetConnectionString("mariadbcontainer")
    ?? builder.Configuration.GetConnectionString("mysql")
    ?? builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Missing database connection string.");

// 4. Registrer DbContext for EF Core med MariaDB
var serverVersion = new MariaDbServerVersion(new Version(11, 4, 0));
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(
        connectionString,
        serverVersion,
        mySqlOptions => mySqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorNumbersToAdd: null
        )
    )
);

// PÅKREVD FOR IDENTITY: Registrer e-postsenderen
builder.Services.AddSingleton<IEmailSender, ConsoleEmailSender>();

// 5. Registrer Identity (Oppdelt i en var identityBuilder akkurat som instruksen viser)
var identityBuilder = builder.Services
    .AddIdentity<IdentityUser, IdentityRole>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
        options.User.RequireUniqueEmail = true;
        options.Lockout.AllowedForNewUsers = true;
        options.Lockout.MaxFailedAccessAttempts = 10;

        // Passordkrav (valgfritt å ha med, men greit for testing)
        options.Password.RequireDigit = false;
        options.Password.RequireLowercase = false;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = false;
        options.Password.RequiredLength = 6;
    })
    .AddDefaultTokenProviders();

// 6. Switch-statement fra instruksen for å dynamisk velge Entity Framework
var dataAccessProvider = builder.Configuration["DataAccess:Provider"] ?? "EntityFramework";
switch (dataAccessProvider.ToUpperInvariant())
{
    case "ENTITYFRAMEWORK":
        identityBuilder.AddEntityFrameworkStores<ApplicationDbContext>();

        // Registrerer repositoriene dine her inne i switchen
        builder.Services.AddScoped<IResourceRepository, ResourceRepository>();
        builder.Services.AddScoped<INeedRepository, NeedRepository>();
        break;
}

// Konfigurer innloggings- og tilgangs-stier for Cookies
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login/";
    options.AccessDeniedPath = "/Account/AccessDenied/";
});

var app = builder.Build();

// 7. Automatisk databasemigrering og seeding ved oppstart (Nøyaktig som bildet viser)
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var dbContext = services.GetRequiredService<ApplicationDbContext>();
        dbContext.Database.Migrate();

        // Har du en egen seeder for ressurser, brukes den her:
        // ResourceDbSeeder.Seed(dbContext);

        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        await IdentitySeeder.SeedRolesAsync(roleManager);

        var userManager = services.GetRequiredService<UserManager<IdentityUser>>();
        await IdentitySeeder.SeedDevelopmentAdminAsync(userManager);
        await IdentitySeeder.SeedDevelopmentUserAsync(userManager);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "En feil oppstod under database-migrering eller seeding.");
    }
}

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHttpsRedirection();
    app.UseHsts();
}

app.UseDeveloperExceptionPage();
app.UseRouting();

// 8. SIKKERHETS-MIDDLEWARE (Må stå etter app.UseRouting() akkurat som bildet presiserer!)
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=FrontPage}/{id?}")
    .WithStaticAssets();

app.Run();