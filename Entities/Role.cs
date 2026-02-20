namespace SegurosApi.Entities;

public class Role
{
    public int RoleId { get; set; }
    public required string RoleName { get; set; }

    public ICollection<User> Users { get; set; } = [];
}
