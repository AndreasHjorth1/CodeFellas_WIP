using Microsoft.EntityFrameworkCore;
using Prosjekt.DataAccess;
using Prosjekt.Models.Entities;
using Prosjekt.Models.Enums;
using Prosjekt.Utils;

namespace Prosjekt.Services
{
    /// <summary>
    /// Legger inn 20 ressurser og 12 behov rundt Kristiansand ved oppstart.
    /// Gjør ingenting hvis det allerede finnes ressurser eller behov.
    /// Alle tider regnes fra dagen det seedes, så dataene ser ferske ut på en demo.
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

        /// <summary>
        /// Et helt klokkeslett i norsk tid, et antall dager fra i dag, gjort om til UTC.
        /// At(-2, 9) er klokka 09:00 i forgårs. At(1, 12) er klokka 12:00 i morgen.
        /// </summary>
        private static DateTime At(int daysFromToday, int hour)
        {
            var today = NorwegianTime.FromUtc(DateTime.UtcNow).Date;
            return NorwegianTime.ToUtc(today.AddDays(daysFromToday).AddHours(hour));
        }

        private static List<Resource> CreateResources(string userId, string coordinatorId)
        {
            const string userPhone = IdentitySeeder.DevelopmentUserPhone;
            const string coordinatorPhone = IdentitySeeder.DevelopmentCoordinatorPhone;

            /// <summary>
            /// Testbrukeren har 14 ressurser, koordinatoren har 6 ressurser. Alle er plassert rundt Kristiansand.
            /// De to siste tidene er når ressursen ble registrert og hvor lenge den er tilgjengelig.
            /// </summary>
            return new List<Resource>
                {
                    NewResource("Personbil", "Toyota Corolla stasjonsvogn [Reg.nr: EB12345]", "Vehicle", "Car", 1,
                        58.1462, 7.9946, userId, userPhone, At(-6, 9), At(30, 20)),
                    NewResource("Lastebil", "Lastebil med kran [Reg.nr: EL45678]", "Vehicle", "Truck", 1,
                        58.1810, 8.1240, userId, userPhone, At(-6, 14), At(30, 20)),
                    NewResource("Traktor med snøfreser", "Kan brøyte og frese snø", "Vehicle", "Tractor", 1,
                        58.0930, 7.8140, userId, userPhone, At(-5, 8), At(21, 16)),
                    NewResource("Gravemaskin", "Mellomstor gravemaskin, 14 tonn", "Vehicle", "Excavator", 1,
                        58.2690, 7.9730, userId, userPhone, At(-5, 11), At(14, 16)),
                    NewResource("Aggregat 5 kW", "Bensindrevet strømaggregat", "Tool", "PowerTool", 2,
                        58.1555, 8.0180, userId, userPhone, At(-4, 10), At(21, 20)),
                    NewResource("Motorsag", "Motorsag med ekstra kjede", "Tool", "PowerTool", 1,
                        58.3330, 8.2310, userId, userPhone, At(-4, 18), At(14, 20)),
                    NewResource("Snøskuffer", "Vanlige snøskuffer", "Tool", "HandTool", 10,
                        58.1220, 7.9450, userId, userPhone, At(-4, 19), At(30, 20)),
                    NewResource("Spader og river", "Til rydding etter flom og ras", "Tool", "GardenTool", 6,
                        58.0700, 7.9970, userId, userPhone, At(-3, 12), At(30, 20)),
                    NewResource("Strøsand", "Sand i sekker", "Materials", "Sand", 500,
                        58.1670, 8.0640, userId, userPhone, At(-3, 15), At(21, 16)),
                    NewResource("Grus", "Grus til vei og fylling", "Materials", "Gravel", 1000,
                        58.4400, 7.9200, userId, userPhone, At(-3, 15), At(21, 16)),
                    NewResource("Flaskevann", "1,5 liter flasker", "Provision", "Water", 200,
                        58.1290, 8.0950, userId, userPhone, At(-2, 9), At(14, 20)),
                    NewResource("Feltrasjoner", "Tørrmat, en porsjon per pakke", "Provision", "FoodOnePortion", 150,
                        58.3405, 8.5934, userId, userPhone, At(-2, 9), At(14, 20)),
                    NewResource("Vinterjakker", "Varme jakker", "Clothing", "L", 15,
                        58.0290, 7.4580, userId, userPhone, At(-2, 17), At(30, 20)),
                    NewResource("Sambandsradioer", "VHF-radioer med lader", "Tool", "Other", 4,
                        58.1910, 8.0280, userId, userPhone, At(-1, 8), At(21, 20)),

                    NewResource("Minibuss", "16 seter [Reg.nr: EF98765]", "Vehicle", "Bus", 1,
                        58.2490, 8.3770, coordinatorId, coordinatorPhone, At(-6, 13), At(21, 18)),
                    NewResource("Pickup", "Firehjulstrekk [Reg.nr: EV24680]", "Vehicle", "Car", 1,
                        58.1590, 8.0000, coordinatorId, coordinatorPhone, At(-5, 16), At(30, 20)),
                    NewResource("Presenninger", "4 x 6 meter", "Materials", "Plastic", 30,
                        58.1460, 8.0300, coordinatorId, coordinatorPhone, At(-4, 13), At(30, 20)),
                    NewResource("Arbeidsklær", "Kjeledresser", "Clothing", "XL", 20,
                        58.1370, 7.9600, coordinatorId, coordinatorPhone, At(-3, 10), At(30, 20)),
                    NewResource("Frivillige mannskaper", "Kan hjelpe med bæring og rydding", "Other", null, 8,
                        58.2030, 8.0850, coordinatorId, coordinatorPhone, At(-2, 20), At(14, 22)),
                    NewResource("Førstehjelpsutstyr", "Førstehjelpsskrin og vanlige medisiner", "Provision", null, 12,
                        58.1880, 8.0700, coordinatorId, coordinatorPhone, At(-1, 11), At(21, 20)),
                };
        }

        private static List<Need> CreateNeeds(string coordinatorId, string adminId)
        {
            const string coordinatorPhone = IdentitySeeder.DevelopmentCoordinatorPhone;
            const string adminPhone = IdentitySeeder.DevelopmentAdminPhone;

            /// <summary>
            /// De to siste tidene er når behovet ble meldt inn og når det trengs.
            /// Akutte behov trengs i løpet av 1-2 dager, planlagte om 2-5 dager.
            /// </summary>
            return new List<Need>
                {
                    NewNeed("Mat til evakuerte", "Ca. 40 personer i Kvadraturen trenger mat", "Food", "Acute", EntryStatus.New,
                        58.1462, 7.9946, coordinatorId, coordinatorPhone, At(-1, 7), At(1, 12)),
                    NewNeed("Medisiner til sykehjem", "Sykehjemmet mangler vanlige medisiner", "Medicine", "Acute", EntryStatus.UnderReview,
                        58.1220, 7.9450, adminId, adminPhone, At(-2, 10), At(1, 18)),
                    NewNeed("Transport av eldre", "Eldre må fraktes til legekontor", "Transport", "Planned", EntryStatus.New,
                        58.1555, 8.0180, coordinatorId, coordinatorPhone, At(-3, 9), At(3, 10)),
                    NewNeed("Strøm til legekontor", "Strømbrudd, trenger aggregat", "PowerGenerator", "Acute", EntryStatus.New,
                        58.0930, 7.8140, adminId, adminPhone, At(-1, 6), At(1, 8)),
                    NewNeed("Snørydding ved skole", "Skolegården og innkjørselen må ryddes", "SnowRemoval", "Planned", EntryStatus.UnderReview,
                        58.2690, 7.9730, coordinatorId, coordinatorPhone, At(-3, 14), At(2, 7)),
                    NewNeed("Strøsand til bakker", "Glatte bakker i boligfeltet", "SandGravel", "Planned", EntryStatus.New,
                        58.1670, 8.0640, adminId, adminPhone, At(-2, 15), At(4, 7)),
                    NewNeed("Gravemaskin etter jordras", "Jordras over veien", "Machinery", "Acute", EntryStatus.New,
                        58.3330, 8.2310, coordinatorId, coordinatorPhone, At(-1, 13), At(1, 16)),
                    NewNeed("Evakuering av boligfelt", "Flomfare, ca. 25 personer må flyttes", "Evacuation", "Acute", EntryStatus.UnderReview,
                        58.1290, 8.0950, adminId, adminPhone, At(-1, 21), At(2, 12)),
                    NewNeed("Samband til innsatsleder", "Mobilnettet er nede i området", "Communication", "Acute", EntryStatus.New,
                        58.2490, 8.3770, coordinatorId, coordinatorPhone, At(-2, 19), At(1, 9)),
                    NewNeed("Mannskap til sandsekker", "Trenger folk til å fylle og legge sandsekker", "Personnel", "Planned", EntryStatus.New,
                        58.0290, 7.4580, adminId, adminPhone, At(-3, 11), At(3, 8)),
                    NewNeed("Vann til hytteområde", "Vannforsyningen er brutt", "Food", "Planned", EntryStatus.UnderReview,
                        58.4400, 7.9200, coordinatorId, coordinatorPhone, At(-2, 8), At(5, 12)),
                    NewNeed("Presenninger til tak", "Tak blåst av etter storm", "Other", "Planned", EntryStatus.New,
                        58.3405, 8.5934, adminId, adminPhone, At(-3, 16), At(3, 14)),
                };
        }

        private static Resource NewResource(string name, string description, string category, string? subCategory,
            int quantity, double lat, double lng, string userId, string phone, DateTime registered, DateTime availableTo)
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
                UserId = userId,
                CreatedAt = registered,
                AvailableFrom = registered,
                AvailableTo = availableTo
            };
        }

        private static Need NewNeed(string name, string description, string category, string priority,
            EntryStatus status, double lat, double lng, string userId, string phone, DateTime created, DateTime neededBy)
        {
            return new Need
            {
                Name = name,
                Description = description,
                Category = category,
                Priority = priority,
                Status = status,
                PhoneNumber = phone,
                Coords = new Coordinates(lat, lng),
                UserId = userId,
                CreatedAt = created,
                NeededBy = neededBy
            };
        }
    }
}