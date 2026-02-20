using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SegurosApi.Data;
using SegurosApi.DTOs;
using SegurosApi.Services;

namespace SegurosApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ApplicationDbContext _context;

    public AuthController(IAuthService authService, ApplicationDbContext context)
    {
        _authService = authService;
        _context = context;
    }

    /// <summary>
    /// Register a new user. RoleName must be one of: Admin, Client, Agent.
    /// </summary>
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RoleName) ||
            !request.RoleName.Trim().Equals("Admin", StringComparison.OrdinalIgnoreCase) &&
            !request.RoleName.Trim().Equals("Client", StringComparison.OrdinalIgnoreCase) &&
            !request.RoleName.Trim().Equals("Agent", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { message = "RoleName must be one of: Admin, Client, Agent." });
        }

        var result = await _authService.RegisterAsync(request, cancellationToken);
        if (result is null)
            return BadRequest(new { message = "Registration failed. Email may already be in use or role is invalid." });

        return Ok(result);
    }

    /// <summary>
    /// Authenticate and return a JWT token.
    /// </summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await _authService.LoginAsync(request, cancellationToken);
        if (result is null)
            return Unauthorized(new { message = "Invalid email or password." });

        return Ok(result);
    }

    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType(typeof(MeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Me(CancellationToken cancellationToken)
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdValue, out var userId))
            return Unauthorized();

        var user = await _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.UserId == userId, cancellationToken);

        if (user is null)
            return Unauthorized();

        return Ok(new MeResponse
        {
            UserId = user.UserId,
            Email = user.Email,
            RoleName = user.Role.RoleName,
            FirstName = user.FirstName,
            LastName = user.LastName
        });
    }
}
