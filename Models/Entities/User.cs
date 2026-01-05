namespace BarberPro.Models.Entities;

/// <summary>
/// Entidad de usuario para autenticación (Admin y Barbero)
/// </summary>
public class User
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Relación 1:1 con Barber
    public Barber? Barber { get; set; }
}

/// <summary>
/// Roles de usuario en el sistema
/// </summary>
public enum UserRole
{
    Admin = 1,
    Barber = 2
}

