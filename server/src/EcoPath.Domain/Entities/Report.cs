namespace EcoPath.Domain.Entities;

public class Report
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime? PeriodStart { get; set; }
    public DateTime? PeriodEnd { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<CalcEntry> CalcEntries { get; set; } = new();
    public List<PathwaysEntry> PathwaysEntries { get; set; } = new();
    public List<OffsetEntry> OffsetEntries { get; set; } = new();
}
