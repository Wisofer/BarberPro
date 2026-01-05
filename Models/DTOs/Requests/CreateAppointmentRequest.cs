using System.ComponentModel.DataAnnotations;

namespace BarberPro.Models.DTOs.Requests;

/// <summary>
/// DTO para crear una cita (público o barbero)
/// </summary>
public class CreateAppointmentRequest
{
    [Required(ErrorMessage = "El slug del barbero es requerido")]
    public string BarberSlug { get; set; } = string.Empty;

    [Required(ErrorMessage = "El servicio es requerido")]
    public int ServiceId { get; set; }

    [Required(ErrorMessage = "El nombre del cliente es requerido")]
    [MaxLength(200, ErrorMessage = "El nombre no puede exceder 200 caracteres")]
    public string ClientName { get; set; } = string.Empty;

    [Required(ErrorMessage = "El teléfono del cliente es requerido")]
    [MaxLength(20, ErrorMessage = "El teléfono no puede exceder 20 caracteres")]
    public string ClientPhone { get; set; } = string.Empty;

    [Required(ErrorMessage = "La fecha es requerida")]
    public DateOnly Date { get; set; }

    [Required(ErrorMessage = "La hora es requerida")]
    public TimeOnly Time { get; set; }
}

