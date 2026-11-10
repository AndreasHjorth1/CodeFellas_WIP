using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Prosjekt.DataAccess;
using Prosjekt.DataAccess.Repositories;
using Prosjekt.Services;
using NetTopologySuite.IO.Converters;

var builder = WebApplication.CreateBuilder(args);

/// <summary>
/// Kestrel: skjul serverheader og begrens størrelsen på forespørsler (maks 30 MB)
/// </summary>
builder.WebHost.ConfigureKestrel(options =>
{
    options.AddServerHeader = false;
    options.Limits.MaxRequestBodySize = 30 * 1024 * 1024;
});

/// 2. Aspire service defaults (helsesjekker, logging, telemetri)
builder.AddServiceDefaults();

/// 3. MVC med anti-forgery på alle POST-forespørsler, og GeoJSON-støtte til kartdataene
builder.Services.AddControllersWithViews(options =>
        options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()))
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new GeoJsonConverterFactory()));

/// 4. Tilkoblingsstreng: Aspire sender den inn som ConnectionStrings:Mapdb
var connectionString = builder.Configuration.GetConnectionString("Mapdb")
    ?? throw new InvalidOperationException(
        "Mangler tilkoblingsstrengen 'Mapdb'. Start appen via Prosjekt.AppHost.");

/// 5. DbContext for EF Core med MariaDB
/// Versjonen oppgis fast, så appen slipper å koble til databasen for å finne den.
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

/// 6. E-postsender for Identity (skriver e-post til loggen, sender ikke ekte e-post)
builder.Services.AddSingleton<IEmailSender, ConsoleEmailSender>();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<AuditLogService>();
// 7. Identity: innlogging, brukere og roller
var identityBuilder = builder.Services
    .AddIdentity<IdentityUser, IdentityRole>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
        options.User.RequireUniqueEmail = true;
        options.Lockout.AllowedForNewUsers = true;
        options.Lockout.MaxFailedAccessAttempts = 10;
    })
    .AddDefaultTokenProviders();

/// 8. Velg datatilgang (som i instruksen)
var dataAccessProvider = builder.Configuration["DataAccess:Provider"] ?? "EntityFramework";
switch (dataAccessProvider.ToUpperInvariant())
{
    case "ENTITYFRAMEWORK":
        identityBuilder.AddEntityFrameworkStores<ApplicationDbContext>();
        builder.Services.AddScoped<IResourceRepository, ResourceRepository>();
        builder.Services.AddScoped<INeedRepository, NeedRepository>();
        builder.Services.AddScoped<IAllocationRepository, AllocationRepository>();
        break;
    default:
        throw new InvalidOperationException(
            $"DataAccess:Provider '{dataAccessProvider}' er ugyldig. Bruk EntityFramework.");
}

/// 9. Innloggings- og tilgangssider for cookies
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login/";
    options.AccessDeniedPath = "/Account/AccessDenied/";
});

var app = builder.Build();

/// 10. Migrering og seeding ved oppstart
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var dbContext = services.GetRequiredService<ApplicationDbContext>();
        dbContext.Database.Migrate();

        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        await IdentitySeeder.SeedRolesAsync(roleManager);

        var userManager = services.GetRequiredService<UserManager<IdentityUser>>();
        await IdentitySeeder.SeedDevelopmentAdminAsync(userManager);
        await IdentitySeeder.SeedDevelopmentUserAsync(userManager);
        await IdentitySeeder.SeedDevelopmentCoordinatorAsync(userManager);

        // Testdata for ressurser og behov, eid av testbrukerne over
        var admin = await userManager.FindByEmailAsync(IdentitySeeder.DevelopmentAdminEmail);
        var coordinator = await userManager.FindByEmailAsync(IdentitySeeder.DevelopmentCoordinatorEmail);
        var user = await userManager.FindByEmailAsync(IdentitySeeder.DevelopmentUserEmail);
        await DataSeeder.SeedAsync(dbContext, user!.Id, coordinator!.Id, admin!.Id);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        /// Appen starter videre selv om dette feiler, så sjekk loggen hvis databasen ikke virker.
        logger.LogError(ex, "En feil oppstod under database-migrering eller seeding.");
    }
}

app.MapDefaultEndpoints();

/// 11. Feilhåndtering: detaljerte feil bare i Development
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    /// Sender ingen videre hvis appen ikke har HTTPS, altså når sertifikatet mangler (se AppHost.cs).
    app.UseHttpsRedirection();
    /// HSTS-headeren sendes bare over HTTPS, og ikke til localhost.
    app.UseHsts();
}

/// Står etter feilhåndteringen, så også feilsiden får sikkerhetsheaderne
app.UseSecurityHeaders();

app.UseRouting();

/// 12. Sikkerhet: må stå etter UseRouting()
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

/// Bare /Need, /Resource osv. går til Index. Uten denne prøver standardruten FrontPage og gir 404.
app.MapControllerRoute(
    name: "controllerIndex",
    pattern: "{controller}",
    defaults: new { action = "Index" })
    .WithStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=FrontPage}/{id?}")
    .WithStaticAssets();

app.Run();


