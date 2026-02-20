using System.ComponentModel.DataAnnotations;

namespace SegurosApi.DTOs;

public class RegisterRequest
{
    [Required]
    [EmailAddress]
    [MaxLength(256)]
    public required string Email { get; set; }

    [Required]
    [MinLength(6, ErrorMessage = "Password must be at least 6 characters.")]
    public required string Password { get; set; }

    [Required]
    [MaxLength(100)]
    public required string FirstName { get; set; }

    [Required]
    [MaxLength(100)]
    public required string LastName { get; set; }

    /// <summary>
    /// Role name: "Admin", "Client", or "Agent"
    /// </summary>
    [Required]
    [MaxLength(50)]
    public required string RoleName { get; set; }
}
