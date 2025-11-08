namespace EcoPath.Domain.Entities;

public class Profile
{
    public int Id { get; set; }
    public string MineName { get; set; } = string.Empty;
    public string MineId { get; set; } = string.Empty;
    public string? Location { get; set; }
    public string? Area { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
}
