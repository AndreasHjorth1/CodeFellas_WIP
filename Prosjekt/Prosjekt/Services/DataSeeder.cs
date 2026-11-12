using Microsoft.EntityFrameworkCore;
using Prosjekt.DataAccess;
using Prosjekt.Models.Entities;
using Prosjekt.Models.Enums;

namespace Prosjekt.Services
{
    /// <summary>
    /// Legger inn 20 ressurser og 12 behov rundt Kristiansand ved oppstart.
    /// Gjør ingenting hvis det allerede finnes ressurser eller behov.
    /// </summary>
    public static class DataSeeder
    {
        public static async Task SeedAsync(ApplicationDbContext context, string userId, string coordinatorId, string adminId)
        {
            if (await context.Resources.AnyAsync() || await context.Needs.AnyAsync())
            {
                return;
            }

            context.Resources.AddRange(CreateResources(userId, coordinatorId));
            context.Needs.AddRange(CreateNeeds(coordinatorId, adminId));
            await context.SaveChangesAsync();
        }

        private static List<Resource> CreateResources(string userId, string coordinatorId)
        {
            const string userPhone = IdentitySeeder.DevelopmentUserPhone;
            const string coordinatorPhone = IdentitySeeder.DevelopmentCoordinatorPhone;

            /// <summary>
            /// Testbrukeren har 14 ressurser, koordinatoren har 6 ressurser. Alle er plassert rundt Kristiansand.
            /// </summary>
            return new List<Resource>
              {
                  NewResource("Personbil", "Toyota Corolla stasjonsvogn [Reg.nr: EB12345]", "Vehicle", "Car", 1, 58.1462, 7.9946, userId, userPhone),
                  NewResource("Lastebil", "Lastebil med kran [Reg.nr: EL45678]", "Vehicle", "Truck", 1, 58.1810, 8.1240, userId, userPhone),
                  NewResource("Traktor med snøfreser", "Kan brøyte og frese snø", "Vehicle", "Tractor", 1, 58.0930, 7.8140, userId, userPhone),
                  NewResource("Gravemaskin", "Mellomstor gravemaskin, 14 tonn", "Vehicle", "Excavator", 1, 58.2690, 7.9730, userId, userPhone),
                  NewResource("Aggregat 5 kW", "Bensindrevet strømaggregat", "Tool", "PowerTool", 2, 58.1555, 8.0180, userId, userPhone),
                  NewResource("Motorsag", "Motorsag med ekstra kjede", "Tool", "PowerTool", 1, 58.3330, 8.2310, userId, userPhone),
                  NewResource("Snøskuffer", "Vanlige snøskuffer", "Tool", "HandTool", 10, 58.1220, 7.9450, userId, userPhone),
                  NewResource("Spader og river", "Til rydding etter flom og ras", "Tool", "GardenTool", 6, 58.0700, 7.9970, userId, userPhone),
                  NewResource("Strøsand", "Sand i sekker", "Materials", "Sand", 500, 58.1670, 8.0640, userId, userPhone),
                  NewResource("Grus", "Grus til vei og fylling", "Materials", "Gravel", 1000, 58.4400, 7.9200, userId, userPhone),
                  NewResource("Flaskevann", "1,5 liter flasker", "Provision", "Water", 200, 58.1290, 8.0950, userId, userPhone),
                  NewResource("Feltrasjoner", "Tørrmat, en porsjon per pakke", "Provision", "FoodOnePortion", 150, 58.3405, 8.5934, userId, userPhone),
                  NewResource("Vinterjakker", "Varme jakker", "Clothing", "L", 15, 58.0290, 7.4580, userId, userPhone),
                  NewResource("Sambandsradioer", "VHF-radioer med lader", "Tool", "Other", 4, 58.1910, 8.0280, userId, userPhone),

                  NewResource("Minibuss", "16 seter [Reg.nr: EF98765]", "Vehicle", "Bus", 1, 58.2490, 8.3770, coordinatorId, coordinatorPhone),
                  NewResource("Pickup", "Firehjulstrekk [Reg.nr: EV24680]", "Vehicle", "Car", 1, 58.1590, 8.0000, coordinatorId, coordinatorPhone),
                  NewResource("Presenninger", "4 x 6 meter", "Materials", "Plastic", 30, 58.1460, 8.0300, coordinatorId, coordinatorPhone),
                  NewResource("Arbeidsklær", "Kjeledresser", "Clothing", "XL", 20, 58.1370, 7.9600, coordinatorId, coordinatorPhone),
                  NewResource("Frivillige mannskaper", "Kan hjelpe med bæring og rydding", "Other", null, 8, 58.2030, 8.0850, coordinatorId, coordinatorPhone),
                  NewResource("Førstehjelpsutstyr", "Førstehjelpsskrin og vanlige medisiner", "Provision", null, 12, 58.1880, 8.0700, coordinatorId, coordinatorPhone),
              };
        }

        private static List<Need> CreateNeeds(string coordinatorId, string adminId)
        {
            const string coordinatorPhone = IdentitySeeder.DevelopmentCoordinatorPhone;
            const string adminPhone = IdentitySeeder.DevelopmentAdminPhone;

            return new List<Need>
              {
                  NewNeed("Mat til evakuerte", "Ca. 40 personer i Kvadraturen trenger mat", "Food", "Acute", EntryStatus.New, 1, 58.1462, 7.9946, coordinatorId, coordinatorPhone),
                  NewNeed("Medisiner til sykehjem", "Sykehjemmet mangler vanlige medisiner", "Medicine", "Acute", EntryStatus.UnderReview, 1, 58.1220, 7.9450, adminId, adminPhone),
                  NewNeed("Transport av eldre", "Eldre må fraktes til legekontor", "Transport", "Planned", EntryStatus.New, 3, 58.1555, 8.0180, coordinatorId, coordinatorPhone),
                  NewNeed("Strøm til legekontor", "Strømbrudd, trenger aggregat", "PowerGenerator", "Acute", EntryStatus.New, 1, 58.0930, 7.8140, adminId, adminPhone),
                  NewNeed("Snørydding ved skole", "Skolegården og innkjørselen må ryddes", "SnowRemoval", "Planned", EntryStatus.UnderReview, 2, 58.2690, 7.9730, coordinatorId, coordinatorPhone),
                  NewNeed("Strøsand til bakker", "Glatte bakker i boligfeltet", "SandGravel", "Planned", EntryStatus.New, 4, 58.1670, 8.0640, adminId, adminPhone),
                  NewNeed("Gravemaskin etter jordras", "Jordras over veien", "Machinery", "Acute", EntryStatus.New, 1, 58.3330, 8.2310, coordinatorId, coordinatorPhone),
                  NewNeed("Evakuering av boligfelt", "Flomfare, ca. 25 personer må flyttes", "Evacuation", "Acute", EntryStatus.UnderReview, 1, 58.1290, 8.0950, adminId, adminPhone),
                  NewNeed("Samband til innsatsleder", "Mobilnettet er nede i området", "Communication", "Acute", EntryStatus.New, 1, 58.2490, 8.3770, coordinatorId, coordinatorPhone),
                  NewNeed("Mannskap til sandsekker", "Trenger folk til å fylle og legge sandsekker", "Personnel", "Planned", EntryStatus.New, 2, 58.0290, 7.4580, adminId, adminPhone),
                  NewNeed("Vann til hytteområde", "Vannforsyningen er brutt", "Food", "Planned", EntryStatus.UnderReview, 5, 58.4400, 7.9200, coordinatorId, coordinatorPhone),
                  NewNeed("Presenninger til tak", "Tak blåst av etter storm", "Other", "Planned", EntryStatus.New, 3, 58.3405, 8.5934, adminId, adminPhone),
              };
        }

        private static Resource NewResource(string name, string description, string category, string? subCategory,
            int quantity, double lat, double lng, string userId, string phone)
        {
            return new Resource
            {
                Name = name,
                Description = description,
                Category = category,
                SubCategory = subCategory,
                Quantity = quantity,
                PhoneNumber = phone,
                Coords = new Coordinates(lat, lng),
                UserId = userId
            };
        }

        private static Need NewNeed(string name, string description, string category, string priority,
            EntryStatus status, int daysUntilNeeded, double lat, double lng, string userId, string phone)
        {
            return new Need
            {
                Name = name,
                Description = description,
                Category = category,
                Priority = priority,
                Status = status,
                NeededBy = DateTime.UtcNow.AddDays(daysUntilNeeded),
                PhoneNumber = phone,
                Coords = new Coordinates(lat, lng),
                UserId = userId
            };
        }
    }
}