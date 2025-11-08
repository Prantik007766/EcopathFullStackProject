namespace EcoPath.Domain.Entities;

public class OffsetEntry
{
    public int Id { get; set; }
    public int ReportId { get; set; }
    public Report? Report { get; set; }

    public double AreaHectares { get; set; }
    public double TreesPerHectare { get; set; }
    public double Years { get; set; }
    public double MarketRate { get; set; }
}
