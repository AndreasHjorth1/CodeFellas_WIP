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

            var resources = CreateResources(userId, coordinatorId);
            var needs = CreateNeeds(coordinatorId, adminId);
            context.Resources.AddRange(resources);
            context.Needs.AddRange(needs);
            await context.SaveChangesAsync();

            /// <summary>
            /// Tildelingene trenger Id-ene, så de legges inn etter at ressursene og behovene er lagret
            /// </summary>
            AddAllocations(context, resources, needs);
            await context.SaveChangesAsync();
        }

        /// <summary>
        /// Tre behov har fått tildelt ressurser, og to er løst. Eierne av ressursene får de samme
        /// varslene som i appen, så bjella har uleste varsler når man logger inn.
        /// </summary>
        private static void AddAllocations(ApplicationDbContext context, List<Resource> resources, List<Need> needs)
        {
            var snorydding = needs.Single(n => n.Name == "Snørydding ved skole");
            var medisiner = needs.Single(n => n.Name == "Medisiner til sykehjem");
            var vann = needs.Single(n => n.Name == "Vann til hytteområde");
            var tak = needs.Single(n => n.Name == "Presenninger til tak");
            var samband = needs.Single(n => n.Name == "Samband til innsatsleder");

            var traktor = resources.Single(r => r.Name == "Traktor med snøfreser");
            var forstehjelp = resources.Single(r => r.Name == "Førstehjelpsutstyr");
            var flaskevann = resources.Single(r => r.Name == "Flaskevann");
            var presenninger = resources.Single(r => r.Name == "Presenninger");
            var radioer = resources.Single(r => r.Name == "Sambandsradioer");

            Allocate(context, snorydding, traktor, 1, At(-2, 10));
            Allocate(context, medisiner, forstehjelp, 4, At(-1, 13));
            Allocate(context, vann, flaskevann, 100, At(-1, 14));

            AllocateAndReturn(context, tak, presenninger, 10, At(-2, 11), At(-1, 16));
            AllocateAndReturn(context, samband, radioer, 2, At(-1, 10), At(-1, 18));
        }

        /// <summary>
        /// Tildeler ressursen til behovet og varsler eieren. Varselet er ulest.
        /// </summary>
        private static void Allocate(ApplicationDbContext context, Need need, Resource resource, int quantity, DateTime when)
        {
            context.Allocations.Add(new Allocation
            {
                NeedId = need.Id,
                ResourceId = resource.Id,
                Quantity = quantity,
                CreatedAt = when
            });

            context.Notifications.Add(new Notification
            {
                UserId = resource.UserId!,
                Message = $"«{resource.Name}» ble tildelt behovet «{need.Name}» ({quantity} stk).",
                CreatedAt = when
            });
        }

        /// <summary>
        /// For løste behov. Ressursen ble tildelt og så levert tilbake da behovet ble løst,
        /// så det er ingen tildeling igjen, bare to varsler som eieren allerede har lest.
        /// </summary>
        private static void AllocateAndReturn(ApplicationDbContext context, Need need, Resource resource, int quantity,
            DateTime allocated, DateTime returned)
        {
            var tildelt = new Notification
            {
                UserId = resource.UserId!,
                Message = $"«{resource.Name}» ble tildelt behovet «{need.Name}» ({quantity} stk).",
                CreatedAt = allocated
            };
            var tilbake = new Notification
            {
                UserId = resource.UserId!,
                Message = $"«{resource.Name}» er levert tilbake. Behovet «{need.Name}» er løst.",
                CreatedAt = returned
            };

            tildelt.MarkAsRead();
            tilbake.MarkAsRead();
            context.Notifications.AddRange(tildelt, tilbake);
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
            /// Hvert behov har en ressurs som passer like i nærheten, så den kommer øverst på Match-siden.
            /// De to siste tidene er når ressursen ble registrert og hvor lenge den er tilgjengelig.
            /// </summary>
            return new List<Resource>
                  {
                      NewResource("Personbil", "Toyota Corolla stasjonsvogn [Reg.nr: EB12345]", "Vehicle", "Car", 1,
                          58.1585, 8.0110, userId, userPhone, At(-6, 9), At(30, 20)),
                      NewResource("Lastebil", "Lastebil med kran [Reg.nr: EL45678]", "Vehicle", "Truck", 1,
                          58.1810, 8.1240, userId, userPhone, At(-6, 14), At(30, 20)),
                      NewResource("Traktor med snøfreser", "Kan brøyte og frese snø", "Vehicle", "Tractor", 1,
                          58.2730, 7.9790, userId, userPhone, At(-5, 8), At(21, 16)),
                      NewResource("Gravemaskin", "Mellomstor gravemaskin, 14 tonn", "Vehicle", "Excavator", 1,
                          58.3360, 8.2240, userId, userPhone, At(-5, 11), At(14, 16)),
                      NewResource("Aggregat 5 kW", "Bensindrevet strømaggregat", "Tool", "PowerTool", 2,
                          58.0890, 7.8090, userId, userPhone, At(-4, 10), At(21, 20)),
                      NewResource("Motorsag", "Motorsag med ekstra kjede", "Tool", "PowerTool", 1,
                          58.3010, 8.1520, userId, userPhone, At(-4, 18), At(14, 20)),
                      NewResource("Snøskuffer", "Vanlige snøskuffer", "Tool", "HandTool", 10,
                          58.1250, 7.9560, userId, userPhone, At(-4, 19), At(30, 20)),
                      NewResource("Spader og river", "Til rydding etter flom og ras", "Tool", "GardenTool", 6,
                          58.0700, 7.9970, userId, userPhone, At(-3, 12), At(30, 20)),
                      NewResource("Strøsand", "Sand i sekker", "Materials", "Sand", 500,
                          58.1620, 8.0680, userId, userPhone, At(-3, 15), At(21, 16)),
                      NewResource("Grus", "Grus til vei og fylling", "Materials", "Gravel", 1000,
                          58.3720, 7.8850, userId, userPhone, At(-3, 15), At(21, 16)),
                      NewResource("Flaskevann", "1,5 liter flasker", "Provision", "Water", 200,
                          58.4430, 7.9130, userId, userPhone, At(-2, 9), At(14, 20)),
                      NewResource("Feltrasjoner", "Tørrmat, en porsjon per pakke", "Provision", "FoodOnePortion", 150,
                          58.1502, 8.0006, userId, userPhone, At(-2, 9), At(14, 20)),
                      NewResource("Vinterjakker", "Varme jakker", "Clothing", "L", 15,
                          58.0780, 7.6020, userId, userPhone, At(-2, 17), At(30, 20)),
                      NewResource("Sambandsradioer", "VHF-radioer med lader", "Tool", "Other", 4,
                          58.2530, 8.3830, userId, userPhone, At(-1, 8), At(21, 20)),

                      NewResource("Minibuss", "16 seter [Reg.nr: EF98765]", "Vehicle", "Bus", 1,
                          58.1250, 8.0900, coordinatorId, coordinatorPhone, At(-6, 13), At(21, 18)),
                      NewResource("Pickup", "Firehjulstrekk [Reg.nr: EV24680]", "Vehicle", "Car", 1,
                          58.1590, 8.0000, coordinatorId, coordinatorPhone, At(-5, 16), At(30, 20)),
                      NewResource("Presenninger", "4 x 6 meter", "Materials", "Plastic", 30,
                          58.3365, 8.5884, coordinatorId, coordinatorPhone, At(-4, 13), At(30, 20)),
                      NewResource("Arbeidsklær", "Kjeledresser", "Clothing", "XL", 20,
                          58.1370, 7.9600, coordinatorId, coordinatorPhone, At(-3, 10), At(30, 20)),
                      NewResource("Frivillige mannskaper", "Kan hjelpe med bæring og rydding [Yrke: Construction]", "Competence", "Construction", 8,
                          58.0240, 7.4620, coordinatorId, coordinatorPhone, At(-2, 20), At(14, 22)),
                      NewResource("Førstehjelpsutstyr", "Førstehjelpsskrin og vanlige medisiner", "Provision", null, 12,
                          58.1170, 7.9490, coordinatorId, coordinatorPhone, At(-1, 11), At(21, 20)),
                  };
        }

        private static List<Need> CreateNeeds(string coordinatorId, string adminId)
        {
            const string coordinatorPhone = IdentitySeeder.DevelopmentCoordinatorPhone;
            const string adminPhone = IdentitySeeder.DevelopmentAdminPhone;

            /// <summary>
            /// De to siste tidene er når behovet ble meldt inn og når det trengs.
            /// Akutte behov trengs i løpet av 1-2 dager, planlagte om 2-5 dager.
            /// Sju behov er åpne og klare til å matches, tre er tildelt og to er løst.
            /// </summary>
            return new List<Need>
                {
                    NewNeed("Mat til evakuerte", "Ca. 40 personer i Kvadraturen trenger mat", "Food", "Acute", EntryStatus.New,
                        58.1462, 7.9946, coordinatorId, coordinatorPhone, At(-1, 7), At(1, 12)),
                    NewNeed("Medisiner til sykehjem", "Sykehjemmet mangler vanlige medisiner", "Medicine", "Acute", EntryStatus.Assigned,
                        58.1220, 7.9450, adminId, adminPhone, At(-2, 10), At(1, 18)),
                    NewNeed("Transport av eldre", "Eldre må fraktes til legekontor", "Transport", "Planned", EntryStatus.New,
                        58.1555, 8.0180, coordinatorId, coordinatorPhone, At(-3, 9), At(3, 10)),
                    NewNeed("Strøm til legekontor", "Strømbrudd, trenger aggregat", "PowerGenerator", "Acute", EntryStatus.New,
                        58.0930, 7.8140, adminId, adminPhone, At(-1, 6), At(1, 8)),
                    NewNeed("Snørydding ved skole", "Skolegården og innkjørselen må ryddes", "SnowRemoval", "Planned", EntryStatus.Assigned,
                        58.2690, 7.9730, coordinatorId, coordinatorPhone, At(-3, 14), At(2, 7)),
                    NewNeed("Strøsand til bakker", "Glatte bakker i boligfeltet", "SandGravel", "Planned", EntryStatus.New,
                        58.1670, 8.0640, adminId, adminPhone, At(-2, 15), At(4, 7)),
                    NewNeed("Gravemaskin etter jordras", "Jordras over veien", "Machinery", "Acute", EntryStatus.New,
                        58.3330, 8.2310, coordinatorId, coordinatorPhone, At(-1, 13), At(1, 16)),
                    NewNeed("Evakuering av boligfelt", "Flomfare, ca. 15 personer må flyttes", "Evacuation", "Acute", EntryStatus.UnderReview,
                        58.1290, 8.0950, adminId, adminPhone, At(-1, 21), At(2, 12)),
                    NewNeed("Samband til innsatsleder", "Mobilnettet er nede i området", "Communication", "Acute", EntryStatus.Resolved,
                        58.2490, 8.3770, coordinatorId, coordinatorPhone, At(-2, 19), At(1, 9)),
                    NewNeed("Mannskap til sandsekker", "Trenger folk til å fylle og legge sandsekker", "Personnel", "Planned", EntryStatus.New,
                        58.0290, 7.4580, adminId, adminPhone, At(-3, 11), At(3, 8)),
                    NewNeed("Vann til hytteområde", "Vannforsyningen er brutt", "Food", "Planned", EntryStatus.Assigned,
                        58.4400, 7.9200, coordinatorId, coordinatorPhone, At(-2, 8), At(5, 12)),
                    NewNeed("Presenninger til tak", "Tak blåst av etter storm", "Other", "Planned", EntryStatus.Resolved,
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