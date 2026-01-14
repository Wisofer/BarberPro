using System.ComponentModel.DataAnnotations;

namespace BarberNic.Models.DTOs.Requests;

/// <summary>
/// DTO para actualizar el token FCM de un dispositivo
/// </summary>
public class UpdateDeviceTokenRequest
{
    [Required(ErrorMessage = "El token FCM actual es requerido")]
    public string CurrentFcmToken { get; set; } = string.Empty;

    [Required(ErrorMessage = "El nuevo token FCM es requerido")]
    [MaxLength(500, ErrorMessage = "El token FCM no puede exceder 500 caracteres")]
    public string NewFcmToken { get; set; } = string.Empty;

    [Required(ErrorMessage = "La plataforma es requerida")]
    [MaxLength(50, ErrorMessage = "La plataforma no puede exceder 50 caracteres")]
    public string Platform { get; set; } = string.Empty;
}
