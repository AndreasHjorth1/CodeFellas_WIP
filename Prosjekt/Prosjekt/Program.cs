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

/// <summary>
/// Aspire service defaults (helsesjekker, logging, telemetri)
/// </summary>
builder.AddServiceDefaults();

/// <summary>
/// MVC med anti-forgery på alle POST-forespørsler, og GeoJSON-støtte til kartdataene
/// </summary>
builder.Services.AddControllersWithViews(options =>
        options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()))
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new GeoJsonConverterFactory()));

/// <summary>
/// Tilkoblingsstreng: Aspire sender den inn som ConnectionStrings:Mapdb
/// </summary>
var connectionString = builder.Configuration.GetConnectionString("Mapdb")
    ?? throw new InvalidOperationException(
        "Mangler tilkoblingsstrengen 'Mapdb'. Start appen via Prosjekt.AppHost.");

/// <summary>
/// DbContext for EF Core med MariaDB
/// Versjonen oppgis fast, så appen slipper å koble til databasen for å finne den.
/// </summary>
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

/// <summary>
/// E-postsender for Identity (skriver e-post til loggen, sender ikke ekte e-post)
/// </summary>
builder.Services.AddSingleton<IEmailSender, ConsoleEmailSender>();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<AuditLogService>();

/// <summary>
/// Identity: innlogging, brukere og roller
/// </summary>
var identityBuilder = builder.Services
    .AddIdentity<IdentityUser, IdentityRole>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
        options.User.RequireUniqueEmail = true;
        options.Lockout.AllowedForNewUsers = true;
        options.Lockout.MaxFailedAccessAttempts = 10;
    })
    .AddDefaultTokenProviders();

/// <summary>
/// Velg datatilgang (som i instruksen)
/// </summary>
var dataAccessProvider = builder.Configuration["DataAccess:Provider"] ?? "EntityFramework";
switch (dataAccessProvider.ToUpperInvariant())
{
    case "ENTITYFRAMEWORK":
        identityBuilder.AddEntityFrameworkStores<ApplicationDbContext>();
        builder.Services.AddScoped<IResourceRepository, ResourceRepository>();
        builder.Services.AddScoped<INeedRepository, NeedRepository>();
        builder.Services.AddScoped<IAllocationRepository, AllocationRepository>();
        builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
        break;
    default:
        throw new InvalidOperationException(
            $"DataAccess:Provider '{dataAccessProvider}' er ugyldig. Bruk EntityFramework.");
}

/// <summary>
/// Innloggings- og tilgangssider for cookies
/// </summary>
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login/";
    options.AccessDeniedPath = "/Account/AccessDenied/";
});

var app = builder.Build();

/// <summary>
/// Migrering og seeding ved oppstart
/// </summary>
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

        /// <summary>
        /// Testdata for ressurser og behov, eid av testbrukerne over
        /// </summary>
        var admin = await userManager.FindByEmailAsync(IdentitySeeder.DevelopmentAdminEmail);
        var coordinator = await userManager.FindByEmailAsync(IdentitySeeder.DevelopmentCoordinatorEmail);
        var user = await userManager.FindByEmailAsync(IdentitySeeder.DevelopmentUserEmail);
        await DataSeeder.SeedAsync(dbContext, user!.Id, coordinator!.Id, admin!.Id);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        /// <summary>
        /// Appen starter videre selv om dette feiler, så sjekk loggen hvis databasen ikke virker.
        /// </summary>
        logger.LogError(ex, "En feil oppstod under database-migrering eller seeding.");
    }
}

app.MapDefaultEndpoints();

/// <summary>
/// Feilhåndtering: detaljerte feil bare i Development
/// </summary>
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    /// <summary>
    /// Sender ingen videre hvis appen ikke har HTTPS, altså når sertifikatet mangler (se AppHost.cs).
    /// </summary>
    app.UseHttpsRedirection();
    /// <summary>
    /// HSTS-headeren sendes bare over HTTPS, og ikke til localhost.
    /// </summary>
    app.UseHsts();
}

/// <summary>
/// Står etter feilhåndteringen, så også feilsiden får sikkerhetsheaderne
/// </summary>
app.UseSecurityHeaders();

app.UseRouting();

/// <summary>
/// Sikkerhet: må stå etter UseRouting()
/// </summary>
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

/// <summary>
/// Bare /Need, /Resource osv. går til Index. Uten denne prøver standardruten FrontPage og gir 404.
/// </summary>
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


