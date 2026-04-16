using System.ComponentModel.DataAnnotations;

namespace SegurosApi.DTOs;

public class CreatePolicyRequest
{
    [Required]
    public required string Type { get; set; }           // Auto, Health, Home, Life

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }

    [Required, Range(0.01, double.MaxValue)]
    public decimal PremiumAmount { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    [Required]
    public int ClientId { get; set; }

    public int? AgentId { get; set; }
}

public class UpdatePolicyRequest
{
    public string? Type { get; set; }
    public string? Status { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public decimal? PremiumAmount { get; set; }
    public string? Description { get; set; }
    public int? AgentId { get; set; }
}

public class PolicyResponse
{
    public int PolicyId { get; set; }
    public required string PolicyNumber { get; set; }
    public required string Type { get; set; }
    public required string Status { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal PremiumAmount { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public int ClientId { get; set; }
    public required string ClientEmail { get; set; }
    public required string ClientFullName { get; set; }
    public int? AgentId { get; set; }
    public string? AgentEmail { get; set; }
    public string? AgentFullName { get; set; }
}
