namespace SegurosApi.Entities;

public class User
{
    public int UserId { get; set; }
    public required string Email { get; set; }
    public required string PasswordHash { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public int RoleId { get; set; }
    public Role Role { get; set; } = null!;
}
