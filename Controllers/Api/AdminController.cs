using BarberPro.Models.DTOs.Requests;
using BarberPro.Models.DTOs.Responses;
using BarberPro.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BarberPro.Controllers.Api;

/// <summary>
/// Controlador de rutas del administrador
/// </summary>
[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly IBarberService _barberService;
    private readonly IDashboardService _dashboardService;
    private readonly ILogger<AdminController> _logger;

    public AdminController(
        IBarberService barberService,
        IDashboardService dashboardService,
        ILogger<AdminController> logger)
    {
        _barberService = barberService;
        _dashboardService = dashboardService;
        _logger = logger;
    }

    /// <summary>
    /// Obtener dashboard del admin
    /// </summary>
    [HttpGet("dashboard")]
    public async Task<ActionResult<AdminDashboardDto>> GetDashboard()
    {
        try
        {
            var dashboard = await _dashboardService.GetAdminDashboardAsync();
            return Ok(dashboard);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener dashboard");
            return StatusCode(500, new { message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Obtener todos los barberos
    /// </summary>
    [HttpGet("barbers")]
    public async Task<ActionResult<List<BarberDto>>> GetBarbers([FromQuery] bool? isActive = null)
    {
        try
        {
            var barbers = await _barberService.GetAllBarbersAsync(isActive);
            return Ok(barbers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener barberos");
            return StatusCode(500, new { message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Actualizar estado del barbero (activar/desactivar)
    /// </summary>
    [HttpPut("barbers/{id}/status")]
    public async Task<ActionResult> UpdateBarberStatus(int id, [FromBody] UpdateBarberStatusRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var updated = await _barberService.UpdateBarberStatusAsync(id, request.IsActive);
            if (!updated)
                return NotFound(new { message = "Barbero no encontrado" });

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar estado del barbero {Id}", id);
            return StatusCode(500, new { message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Crear nuevo barbero
    /// </summary>
    [HttpPost("barbers")]
    public async Task<ActionResult<BarberDto>> CreateBarber([FromBody] CreateBarberRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var barber = await _barberService.CreateBarberAsync(request);
            return CreatedAtAction(nameof(GetBarbers), new { id = barber.Id }, barber);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear barbero");
            return StatusCode(500, new { message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Eliminar barbero
    /// </summary>
    [HttpDelete("barbers/{id}")]
    public async Task<ActionResult> DeleteBarber(int id)
    {
        try
        {
            var deleted = await _barberService.DeleteBarberAsync(id);
            if (!deleted)
                return NotFound(new { message = "Barbero no encontrado" });

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al eliminar barbero {Id}", id);
            return StatusCode(500, new { message = "Error interno del servidor" });
        }
    }
}

/// <summary>
/// DTO para actualizar estado del barbero
/// </summary>
public class UpdateBarberStatusRequest
{
    public bool IsActive { get; set; }
}

