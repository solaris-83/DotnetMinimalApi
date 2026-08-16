using DotnetMinimalApi.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace DotnetMinimalApi.Data;

/// <summary>
/// Handles database migration/creation and initial sample data seeding.
/// </summary>
public static class DbInitializer
{
    public static async Task InitializeDatabaseAsync(AppDbContext context, ILogger logger, CancellationToken ct = default)
    {
        try
        {
            logger.LogInformation("Ensuring SQLite database is created...");
            await context.Database.EnsureCreatedAsync(ct);

            if (await context.Categories.AnyAsync(ct))
            {
                logger.LogInformation("Database already contains data. Skipping initial seeding.");
                return;
            }

            logger.LogInformation("Seeding initial catalog data...");
            await SeedDataAsync(context, ct);
            logger.LogInformation("Database seeding completed successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while initializing and seeding the database.");
            throw;
        }
    }

    public static async Task ResetAndSeedDataAsync(AppDbContext context, CancellationToken ct = default)
    {
        await context.Database.EnsureDeletedAsync(ct);
        await context.Database.EnsureCreatedAsync(ct);
        await SeedDataAsync(context, ct);
    }

    private static async Task SeedDataAsync(AppDbContext context, CancellationToken ct)
    {
        var electronics = new Category
        {
            Name = "Electronics & Gadgets",
            Slug = "electronics-gadgets",
            Description = "Cutting-edge consumer tech, audio equipment, and computing accessories."
        };

        var wearables = new Category
        {
            Name = "Wearables & Fitness",
            Slug = "wearables-fitness",
            Description = "Smartwatches, fitness bands, and wearable health monitors."
        };

        var homeOffice = new Category
        {
            Name = "Home Office & Ergonomics",
            Slug = "home-office-ergonomics",
            Description = "Ergonomic furniture, mechanical keyboards, and desk productivity essentials."
        };

        var photography = new Category
        {
            Name = "Photography & Video",
            Slug = "photography-video",
            Description = "Cameras, precision lenses, lighting gear, and creator studio tools."
        };

        context.Categories.AddRange(electronics, wearables, homeOffice, photography);
        await context.SaveChangesAsync(ct);

        var products = new List<Product>
        {
            new()
            {
                Name = "SonicPro Wireless ANC Headphones",
                Sku = "ELEC-HDPH-001",
                Description = "High-fidelity wireless over-ear headphones with adaptive active noise cancellation and 40-hour battery life.",
                Price = 299.99m,
                StockQuantity = 45,
                IsActive = true,
                CategoryId = electronics.Id
            },
            new()
            {
                Name = "NovaBeam 4K Ultra Laser Projector",
                Sku = "ELEC-PROJ-002",
                Description = "True 4K HDR home theater projector with 3000 ANSI lumens, ultra-low input lag, and Android TV integration.",
                Price = 1499.00m,
                StockQuantity = 12,
                IsActive = true,
                CategoryId = electronics.Id
            },
            new()
            {
                Name = "ThunderDock Pro 14-in-1 Docking Station",
                Sku = "ELEC-DOCK-003",
                Description = "Dual 4K 60Hz Thunderbolt 4 hub with 100W Power Delivery, 2.5GbE Ethernet, and SD 4.0 card reader.",
                Price = 189.50m,
                StockQuantity = 3, // Low stock sample
                IsActive = true,
                CategoryId = electronics.Id
            },
            new()
            {
                Name = "PulseFit Elite Titanium Smartwatch",
                Sku = "WEAR-SMWT-001",
                Description = "Aerospace-grade titanium smartwatch with ECG, dual-band GPS, sapphire glass, and 14-day battery reserve.",
                Price = 349.99m,
                StockQuantity = 28,
                IsActive = true,
                CategoryId = wearables.Id
            },
            new()
            {
                Name = "AuraRing Smart Sleep & Recovery Tracker",
                Sku = "WEAR-RING-002",
                Description = "Discreet lightweight titanium smart ring monitoring sleep stages, skin temperature, HRV, and daily strain.",
                Price = 279.00m,
                StockQuantity = 18,
                IsActive = true,
                CategoryId = wearables.Id
            },
            new()
            {
                Name = "ErgoCurve Mesh Office Chair",
                Sku = "HOFF-CHAIR-001",
                Description = "Ergonomic mesh chair with 4D adjustable armrests, adaptive lumbar support system, and pneumatic height control.",
                Price = 429.95m,
                StockQuantity = 15,
                IsActive = true,
                CategoryId = homeOffice.Id
            },
            new()
            {
                Name = "Keystroke 75% Custom Mechanical Keyboard",
                Sku = "HOFF-KBD-002",
                Description = "Gasket-mounted hot-swappable mechanical keyboard with lubricated linear switches and wireless tri-mode connectivity.",
                Price = 165.00m,
                StockQuantity = 4, // Low stock sample
                IsActive = true,
                CategoryId = homeOffice.Id
            },
            new()
            {
                Name = "Lumina Desk Arc Ambient Lightbar",
                Sku = "HOFF-LGT-003",
                Description = "Screen-mounted asymmetric LED lightbar with wireless control dial and auto-dimming ambient light sensor.",
                Price = 79.99m,
                StockQuantity = 60,
                IsActive = true,
                CategoryId = homeOffice.Id
            },
            new()
            {
                Name = "AeroGlide Standing Desk Dual Motor",
                Sku = "HOFF-DESK-004",
                Description = "Solid walnut top motorized height-adjustable desk with anti-collision sensor and memory presets.",
                Price = 649.00m,
                StockQuantity = 8,
                IsActive = true,
                CategoryId = homeOffice.Id
            },
            new()
            {
                Name = "ApexPrime 50mm f/1.2 Mirrorless Lens",
                Sku = "PHOT-LENS-001",
                Description = "Ultra-fast prime lens with exceptional edge-to-edge sharpness, weather sealing, and circular 11-blade aperture.",
                Price = 1199.99m,
                StockQuantity = 6,
                IsActive = true,
                CategoryId = photography.Id
            },
            new()
            {
                Name = "StudioGlow 120W Bi-Color Studio Light",
                Sku = "PHOT-LGT-002",
                Description = "Continuous studio video light with Bowens mount, silent fan mode, and wireless app control.",
                Price = 249.99m,
                StockQuantity = 0, // Out of stock sample
                IsActive = false,
                CategoryId = photography.Id
            }
        };

        context.Products.AddRange(products);
        await context.SaveChangesAsync(ct);

        var reviews = new List<Review>
        {
            new()
            {
                ProductId = products[0].Id,
                AuthorName = "Alex Rivera",
                Rating = 5,
                Comment = "Outstanding noise cancellation on flights and crystal-clear microphone audio for remote work meetings."
            },
            new()
            {
                ProductId = products[0].Id,
                AuthorName = "Morgan Bailey",
                Rating = 4,
                Comment = "Comfortable for long listening sessions. Bass is punchy without distorting highs."
            },
            new()
            {
                ProductId = products[3].Id,
                AuthorName = "Sam Chen",
                Rating = 5,
                Comment = "Battery easily lasts almost two weeks on a single charge. GPS accuracy is pinpoint on marathon training runs."
            },
            new()
            {
                ProductId = products[6].Id,
                AuthorName = "Taylor Swift",
                Rating = 5,
                Comment = "The typing experience is bliss! Deep thocky acoustics and flawless Bluetooth connection."
            },
            new()
            {
                ProductId = products[9].Id,
                AuthorName = "Jordan Reed",
                Rating = 5,
                Comment = "Unbelievable creamy bokeh and contrast at f/1.2. A workhorse for portrait photography."
            }
        };

        context.Reviews.AddRange(reviews);
        await context.SaveChangesAsync(ct);
    }
}
