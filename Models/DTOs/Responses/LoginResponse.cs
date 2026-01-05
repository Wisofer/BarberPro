namespace BarberPro.Models.DTOs.Responses;

/// <summary>
/// DTO de respuesta para login
/// </summary>
public class LoginResponse
{
    public string Token { get; set; } = string.Empty;
    public UserDto User { get; set; } = null!;
    public string Role { get; set; } = string.Empty;
}

/// <summary>
/// DTO de usuario para respuestas
/// </summary>
public class UserDto
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public BarberDto? Barber { get; set; }
}

