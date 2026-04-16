using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SegurosApi.Data;
using SegurosApi.DTOs;
using SegurosApi.Entities;

namespace SegurosApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PoliciesController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public PoliciesController(ApplicationDbContext context)
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

        var query = _context.Policies
            .Include(p => p.Client)
            .Include(p => p.Agent)
            .AsQueryable();

        if (role == "Client")
            query = query.Where(p => p.ClientId == userId);
        else if (role == "Agent")
            query = query.Where(p => p.AgentId == userId);

        var policies = await query.OrderByDescending(p => p.CreatedAt).ToListAsync(ct);
        return Ok(policies.Select(ToResponse));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var userId = GetUserId();
        var role = GetUserRole();

        var policy = await _context.Policies
            .Include(p => p.Client)
            .Include(p => p.Agent)
            .FirstOrDefaultAsync(p => p.PolicyId == id, ct);

        if (policy is null) return NotFound();
        if (role == "Client" && policy.ClientId != userId) return Forbid();
        if (role == "Agent" && policy.AgentId != userId) return Forbid();

        return Ok(ToResponse(policy));
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Agent")]
    public async Task<IActionResult> Create([FromBody] CreatePolicyRequest request, CancellationToken ct)
    {
        if (!IsValidType(request.Type))
            return BadRequest(new { message = "Type must be: Auto, Health, Home, Life" });

        if (request.EndDate <= request.StartDate)
            return BadRequest(new { message = "EndDate must be after StartDate." });

        var client = await _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.UserId == request.ClientId, ct);

        if (client is null || client.Role.RoleName != "Client")
            return BadRequest(new { message = "ClientId must refer to a user with role Client." });

        var policy = new Policy
        {
            PolicyNumber = GeneratePolicyNumber(),
            Type = request.Type,
            Status = "Pending",
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            PremiumAmount = request.PremiumAmount,
            Description = request.Description,
            ClientId = request.ClientId,
            AgentId = request.AgentId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Policies.Add(policy);
        await _context.SaveChangesAsync(ct);

        await _context.Entry(policy).Reference(p => p.Client).LoadAsync(ct);
        if (policy.AgentId.HasValue)
            await _context.Entry(policy).Reference(p => p.Agent).LoadAsync(ct);

        return CreatedAtAction(nameof(GetById), new { id = policy.PolicyId }, ToResponse(policy));
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,Agent")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdatePolicyRequest request, CancellationToken ct)
    {
        var policy = await _context.Policies
            .Include(p => p.Client)
            .Include(p => p.Agent)
            .FirstOrDefaultAsync(p => p.PolicyId == id, ct);

        if (policy is null) return NotFound();

        if (request.Type is not null)
        {
            if (!IsValidType(request.Type))
                return BadRequest(new { message = "Type must be: Auto, Health, Home, Life" });
            policy.Type = request.Type;
        }

        if (request.Status is not null)
        {
            if (!IsValidStatus(request.Status))
                return BadRequest(new { message = "Status must be: Pending, Active, Expired, Cancelled" });
            policy.Status = request.Status;
        }

        if (request.StartDate.HasValue) policy.StartDate = request.StartDate.Value;
        if (request.EndDate.HasValue) policy.EndDate = request.EndDate.Value;
        if (request.PremiumAmount.HasValue) policy.PremiumAmount = request.PremiumAmount.Value;
        if (request.Description is not null) policy.Description = request.Description;
        if (request.AgentId is not null) policy.AgentId = request.AgentId;

        await _context.SaveChangesAsync(ct);
        return Ok(ToResponse(policy));
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var policy = await _context.Policies.FindAsync(new object[] { id }, ct);
        if (policy is null) return NotFound();

        _context.Policies.Remove(policy);
        await _context.SaveChangesAsync(ct);
        return NoContent();
    }

    private static PolicyResponse ToResponse(Policy p) => new()
    {
        PolicyId = p.PolicyId,
        PolicyNumber = p.PolicyNumber,
        Type = p.Type,
        Status = p.Status,
        StartDate = p.StartDate,
        EndDate = p.EndDate,
        PremiumAmount = p.PremiumAmount,
        Description = p.Description,
        CreatedAt = p.CreatedAt,
        ClientId = p.ClientId,
        ClientEmail = p.Client?.Email ?? "",
        ClientFullName = p.Client is not null ? $"{p.Client.FirstName} {p.Client.LastName}" : "",
        AgentId = p.AgentId,
        AgentEmail = p.Agent?.Email,
        AgentFullName = p.Agent is not null ? $"{p.Agent.FirstName} {p.Agent.LastName}" : null
    };

    private static string GeneratePolicyNumber() =>
        $"POL-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}";

    private static bool IsValidType(string t) => t is "Auto" or "Health" or "Home" or "Life";
    private static bool IsValidStatus(string s) => s is "Pending" or "Active" or "Expired" or "Cancelled";
}
