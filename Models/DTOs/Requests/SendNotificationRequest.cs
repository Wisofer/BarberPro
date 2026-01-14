using System.ComponentModel.DataAnnotations;

namespace BarberNic.Models.DTOs.Requests;

/// <summary>
/// DTO para enviar una notificación push
/// </summary>
public class SendNotificationRequest
{
    [Required(ErrorMessage = "El ID de la plantilla es requerido")]
    public int TemplateId { get; set; }

    /// <summary>
    /// Datos adicionales a enviar (opcional)
    /// </summary>
    public Dictionary<string, string>? ExtraData { get; set; }

    /// <summary>
    /// Si es true, solo envía datos sin notificación del sistema
    /// </summary>
    public bool DataOnly { get; set; } = false;
}
