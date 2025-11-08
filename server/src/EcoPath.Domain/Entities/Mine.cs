namespace EcoPath.Domain.Entities;

public enum MineType { Underground = 1, Surface = 2 }
public enum MineDegree { Degree1 = 1, Degree2 = 2, Degree3 = 3 }

public class Mine
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public MineType Type { get; set; }
    public MineDegree Degree { get; set; }
    public string? Location { get; set; }
}
