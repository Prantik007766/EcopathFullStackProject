namespace EcoPath.Domain.Entities;

public class CalcEntry
{
    public int Id { get; set; }
    public int ReportId { get; set; }
    public Report? Report { get; set; }

    public string Activity { get; set; } = string.Empty;
    public string? FuelType { get; set; }
    public double Quantity { get; set; }
    public string Unit { get; set; } = string.Empty;
}
