namespace BarberNic.Models.DTOs.Responses;

/// <summary>
/// DTO de respuesta para plantilla de notificación
/// </summary>
public class TemplateDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public string? Name { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
