namespace Auth.Models;

public class User
{
    public int Id { get; set; }  // Auto-increment
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}
