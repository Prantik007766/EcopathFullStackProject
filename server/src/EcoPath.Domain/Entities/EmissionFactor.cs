namespace EcoPath.Domain.Entities;

public enum FactorCategory { Fuel = 1, Electricity = 2, Methane = 3 }

public class EmissionFactor
{
    public int Id { get; set; }
    public FactorCategory Category { get; set; }
    public string Code { get; set; } = string.Empty; // e.g., petrol, diesel, natural_gas, grid
    public string Unit { get; set; } = string.Empty; // e.g., kg_per_liter, kg_per_kwh
    public double Factor { get; set; }              // numeric factor in kg CO2e per unit
    public DateTime EffectiveFrom { get; set; } = DateTime.UtcNow.Date;
    public DateTime? EffectiveTo { get; set; }
}
