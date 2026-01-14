namespace BarberNic.Models.DTOs.Responses;

/// <summary>
/// DTO de respuesta para dispositivo
/// </summary>
public class DeviceDto
{
    public int Id { get; set; }
    public string FcmToken { get; set; } = string.Empty;
    public string Platform { get; set; } = string.Empty;
    public DateTime? LastActiveAt { get; set; }
    public int UserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
