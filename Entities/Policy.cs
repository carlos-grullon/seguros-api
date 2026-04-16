namespace SegurosApi.Entities;

public class Policy
{
    public int PolicyId { get; set; }
    public required string PolicyNumber { get; set; }
    public required string Type { get; set; }       // Auto, Health, Home, Life
    public required string Status { get; set; }     // Pending, Active, Expired, Cancelled
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal PremiumAmount { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public int ClientId { get; set; }
    public User Client { get; set; } = null!;

    public int? AgentId { get; set; }
    public User? Agent { get; set; }

    public ICollection<Claim> Claims { get; set; } = [];
}
