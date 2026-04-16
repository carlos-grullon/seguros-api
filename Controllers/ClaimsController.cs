using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SegurosApi.Data;
using SegurosApi.DTOs;
using SegurosApi.Entities;
using InsuranceClaim = SegurosApi.Entities.Claim;

namespace SegurosApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ClaimsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public ClaimsController(ApplicationDbContext context)
    {
        _context = context;
    }

    private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private string GetUserRole() => User.FindFirstValue(ClaimTypes.Role)!;

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var userId = GetUserId();
        var role = GetUserRole();

        var query = _context.Claims
            .Include(c => c.Policy)
                .ThenInclude(p => p.Client)
            .AsQueryable();

        if (role == "Client")
            query = query.Where(c => c.Policy.ClientId == userId);
        else if (role == "Agent")
            query = query.Where(c => c.Policy.AgentId == userId);

        var claims = await query.OrderByDescending(c => c.CreatedAt).ToListAsync(ct);
        return Ok(claims.Select(ToResponse));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var userId = GetUserId();
        var role = GetUserRole();

        var claim = await _context.Claims
            .Include(c => c.Policy)
                .ThenInclude(p => p.Client)
            .FirstOrDefaultAsync(c => c.ClaimId == id, ct);

        if (claim is null) return NotFound();
        if (role == "Client" && claim.Policy.ClientId != userId) return Forbid();
        if (role == "Agent" && claim.Policy.AgentId != userId) return Forbid();

        return Ok(ToResponse(claim));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateClaimRequest request, CancellationToken ct)
    {
        var userId = GetUserId();
        var role = GetUserRole();

        var policy = await _context.Policies
            .FirstOrDefaultAsync(p => p.PolicyId == request.PolicyId, ct);

        if (policy is null) return NotFound(new { message = "Policy not found." });
        if (policy.Status != "Active")
            return BadRequest(new { message = "Claims can only be filed on Active policies." });

        if (role == "Client" && policy.ClientId != userId)
            return Forbid();
        if (role == "Agent" && policy.AgentId != userId)
            return Forbid();

        var claim = new InsuranceClaim
        {
            ClaimNumber = GenerateClaimNumber(),
            Description = request.Description,
            Status = "Pending",
            IncidentDate = request.IncidentDate,
            PolicyId = request.PolicyId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Claims.Add(claim);
        await _context.SaveChangesAsync(ct);

        await _context.Entry(claim).Reference(c => c.Policy).LoadAsync(ct);
        await _context.Entry(claim.Policy).Reference(p => p.Client).LoadAsync(ct);

        return CreatedAtAction(nameof(GetById), new { id = claim.ClaimId }, ToResponse(claim));
    }

    [HttpPut("{id}/status")]
    [Authorize(Roles = "Admin,Agent")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateClaimStatusRequest request, CancellationToken ct)
    {
        if (!IsValidStatus(request.Status))
            return BadRequest(new { message = "Status must be: Pending, InReview, Approved, Rejected" });

        var claim = await _context.Claims
            .Include(c => c.Policy)
                .ThenInclude(p => p.Client)
            .FirstOrDefaultAsync(c => c.ClaimId == id, ct);

        if (claim is null) return NotFound();

        claim.Status = request.Status;
        await _context.SaveChangesAsync(ct);

        return Ok(ToResponse(claim));
    }

    private static ClaimResponse ToResponse(InsuranceClaim c) => new()
    {
        ClaimId = c.ClaimId,
        ClaimNumber = c.ClaimNumber,
        Description = c.Description,
        Status = c.Status,
        IncidentDate = c.IncidentDate,
        CreatedAt = c.CreatedAt,
        PolicyId = c.PolicyId,
        PolicyNumber = c.Policy?.PolicyNumber ?? "",
        PolicyType = c.Policy?.Type ?? "",
        PolicyStatus = c.Policy?.Status ?? "",
        ClientFullName = c.Policy?.Client is not null
            ? $"{c.Policy.Client.FirstName} {c.Policy.Client.LastName}"
            : ""
    };

    private static string GenerateClaimNumber() =>
        $"CLM-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}";

    private static bool IsValidStatus(string s) =>
        s is "Pending" or "InReview" or "Approved" or "Rejected";
}
