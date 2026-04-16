using System.ComponentModel.DataAnnotations;

namespace SegurosApi.DTOs;

public class CreateClaimRequest
{
    [Required]
    public int PolicyId { get; set; }

    [Required, MaxLength(1000)]
    public required string Description { get; set; }

    [Required]
    public DateTime IncidentDate { get; set; }
}

public class UpdateClaimStatusRequest
{
    [Required]
    public required string Status { get; set; }     // Pending, InReview, Approved, Rejected
}

public class ClaimResponse
{
    public int ClaimId { get; set; }
    public required string ClaimNumber { get; set; }
    public required string Description { get; set; }
    public required string Status { get; set; }
    public DateTime IncidentDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public int PolicyId { get; set; }
    public required string PolicyNumber { get; set; }
    public required string PolicyType { get; set; }
    public required string PolicyStatus { get; set; }
    public required string ClientFullName { get; set; }
}
