namespace SegurosApi.DTOs;

public class AuthResponse
{
    public required string Token { get; set; }
    public required string Email { get; set; }
    public required string RoleName { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public DateTime ExpiresAt { get; set; }
}
