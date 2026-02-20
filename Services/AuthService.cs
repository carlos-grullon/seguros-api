using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SegurosApi.Data;
using SegurosApi.DTOs;
using SegurosApi.Entities;

namespace SegurosApi.Services;

public class AuthService : IAuthService
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;

    public AuthService(ApplicationDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    public async Task<AuthResponse?> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var normalizedRole = request.RoleName.Trim();
        if (!IsValidRole(normalizedRole))
            return null;

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        if (await _context.Users.AnyAsync(u => u.Email.ToLower() == normalizedEmail, cancellationToken))
            return null;

        var role = await _context.Roles
            .FirstOrDefaultAsync(r => r.RoleName.ToLower() == normalizedRole.ToLower(), cancellationToken);
        if (role is null)
            return null;

        var user = new User
        {
            Email = normalizedEmail,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            RoleId = role.RoleId
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync(cancellationToken);

        return await BuildAuthResponseAsync(user, role.RoleName, cancellationToken);
    }

    public async Task<AuthResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var user = await _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail, cancellationToken);
        if (user is null)
            return null;

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return null;

        return await BuildAuthResponseAsync(user, user.Role.RoleName, cancellationToken);
    }

    private static bool IsValidRole(string roleName) =>
        roleName.Equals("Admin", StringComparison.OrdinalIgnoreCase) ||
        roleName.Equals("Client", StringComparison.OrdinalIgnoreCase) ||
        roleName.Equals("Agent", StringComparison.OrdinalIgnoreCase);

    private Task<AuthResponse> BuildAuthResponseAsync(User user, string roleName, CancellationToken cancellationToken)
    {
        var token = GenerateJwt(user.UserId, user.Email, roleName);
        var expiresAt = DateTime.UtcNow.AddHours(GetTokenExpirationHours());

        var response = new AuthResponse
        {
            Token = token,
            Email = user.Email,
            RoleName = roleName,
            FirstName = user.FirstName,
            LastName = user.LastName,
            ExpiresAt = expiresAt
        };

        return Task.FromResult(response);
    }

    private string GenerateJwt(int userId, string email, string roleName)
    {
        var key = _configuration["Jwt:Key"] ?? "YourSuperSecretKeyThatIsAtLeast32CharactersLong!";
        var issuer = _configuration["Jwt:Issuer"] ?? "SegurosApi";
        var audience = _configuration["Jwt:Audience"] ?? "SegurosApi";

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Email, email),
            new Claim(ClaimTypes.Role, roleName),
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer,
            audience,
            claims,
            expires: DateTime.UtcNow.AddHours(GetTokenExpirationHours()),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private int GetTokenExpirationHours()
    {
        if (int.TryParse(_configuration["Jwt:ExpirationHours"], out var hours))
            return hours;
        return 24;
    }
}
