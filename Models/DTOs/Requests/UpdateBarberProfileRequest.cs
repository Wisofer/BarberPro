using System.ComponentModel.DataAnnotations;

namespace BarberPro.Models.DTOs.Requests;

/// <summary>
/// DTO para actualizar el perfil del barbero
/// </summary>
public class UpdateBarberProfileRequest
{
    [Required(ErrorMessage = "El nombre es requerido")]
    [MaxLength(200, ErrorMessage = "El nombre no puede exceder 200 caracteres")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(200, ErrorMessage = "El nombre del negocio no puede exceder 200 caracteres")]
    public string? BusinessName { get; set; }

    [Required(ErrorMessage = "El teléfono es requerido")]
    [MaxLength(20, ErrorMessage = "El teléfono no puede exceder 20 caracteres")]
    public string Phone { get; set; } = string.Empty;
}

