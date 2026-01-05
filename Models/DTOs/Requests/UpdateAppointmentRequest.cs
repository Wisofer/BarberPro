using BarberPro.Models.Entities;

namespace BarberPro.Models.DTOs.Requests;

/// <summary>
/// DTO para actualizar una cita
/// </summary>
public class UpdateAppointmentRequest
{
    public AppointmentStatus? Status { get; set; }
    public DateOnly? Date { get; set; }
    public TimeOnly? Time { get; set; }
}

