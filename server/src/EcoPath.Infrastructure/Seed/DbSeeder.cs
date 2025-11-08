using EcoPath.Domain.Entities;
using EcoPath.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EcoPath.Infrastructure.Seed;

public static class DbSeeder
{
    public static async Task SeedAsync(EcoPathDbContext db)
    {
        await db.Database.EnsureCreatedAsync();

        if (!await db.EmissionFactors.AnyAsync())
        {
            db.EmissionFactors.AddRange(new[]
            {
                new EmissionFactor { Category = FactorCategory.Fuel, Code = "petrol",       Unit = "kg_per_liter", Factor = 2.31 },
                new EmissionFactor { Category = FactorCategory.Fuel, Code = "diesel",       Unit = "kg_per_liter", Factor = 2.68 },
                new EmissionFactor { Category = FactorCategory.Fuel, Code = "natural_gas",  Unit = "kg_per_m3",    Factor = 2.02 },
                new EmissionFactor { Category = FactorCategory.Electricity, Code = "grid", Unit = "kg_per_kwh",   Factor = 0.75 },
                new EmissionFactor { Category = FactorCategory.Methane, Code = "ch4_capture_gwp", Unit = "tco2e_per_ton_ch4", Factor = 27.0 },
                new EmissionFactor { Category = FactorCategory.Methane, Code = "vam_gwp",        Unit = "tco2e_per_ton_ch4", Factor = 20.0 }
            });
        }

        if (!await db.Mines.AnyAsync())
        {
            db.Mines.Add(new Mine { Name = "Sample Mine", Type = MineType.Surface, Degree = MineDegree.Degree1, Location = "Local" });
        }

        await db.SaveChangesAsync();
    }
}
