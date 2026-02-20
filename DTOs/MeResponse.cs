namespace SegurosApi.DTOs;

public class MeResponse
{
    public int UserId { get; set; }
    public required string Email { get; set; }
    public required string RoleName { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
}
