namespace BarberPro.Models.DTOs.Responses;

/// <summary>
/// DTO de cita
/// </summary>
public class AppointmentDto
{
    public int Id { get; set; }
    public int BarberId { get; set; }
    public string BarberName { get; set; } = string.Empty;
    public int ServiceId { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public decimal ServicePrice { get; set; }
    public string ClientName { get; set; } = string.Empty;
    public string ClientPhone { get; set; } = string.Empty;
    public DateOnly Date { get; set; }
    public TimeOnly Time { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

