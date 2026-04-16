namespace SegurosApi.Entities;

public class Claim
{
    public int ClaimId { get; set; }
    public required string ClaimNumber { get; set; }
    public required string Description { get; set; }
    public required string Status { get; set; }     // Pending, InReview, Approved, Rejected
    public DateTime IncidentDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public int PolicyId { get; set; }
    public Policy Policy { get; set; } = null!;
}
