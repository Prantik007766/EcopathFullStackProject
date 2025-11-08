namespace EcoPath.Domain.Entities;

public class PathwaysEntry
{
    public int Id { get; set; }
    public int ReportId { get; set; }
    public Report? Report { get; set; }

    public double EvCount { get; set; }
    public double ReMW { get; set; }
    public double RePct { get; set; }
    public double McCH4 { get; set; }
    public double VamCH4 { get; set; }
}
