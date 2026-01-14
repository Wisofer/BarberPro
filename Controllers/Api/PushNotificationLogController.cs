using BarberNic.Data;
using BarberNic.Models.DTOs.Responses;
using BarberNic.Utils;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BarberNic.Controllers.Api;

/// <summary>
/// Controlador para gestión de logs de notificaciones (compatibilidad con frontend)
/// </summary>
[ApiController]
[Route("v1/push/notificationlog")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class PushNotificationLogController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<PushNotificationLogController> _logger;

    public PushNotificationLogController(
        ApplicationDbContext context,
        ILogger<PushNotificationLogController> logger)
    {
        _context = context;
        _logger = logger;
    }

    private Task<int> GetUserIdAsync()
    {
        var userId = JwtHelper.GetUserId(User);
        if (!userId.HasValue)
            throw new UnauthorizedAccessException("Usuario no identificado");
        return Task.FromResult(userId.Value);
    }

    /// <summary>
    /// Marcar notificación como leída
    /// </summary>
    [HttpPost("{id}/opened")]
    public async Task<ActionResult> MarkNotificationAsOpened(int id)
    {
        try
        {
            var userId = await GetUserIdAsync();
            var log = await _context.NotificationLogs
                .FirstOrDefaultAsync(nl => nl.Id == id && nl.UserId == userId);

            if (log == null)
                return NotFound(new { message = "Notificación no encontrada" });

            log.Status = "opened";
            log.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Notificación marcada como leída", id = log.Id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al marcar notificación como leída {Id}", id);
            return StatusCode(500, new { message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Eliminar notificación
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteNotificationLog(int id)
    {
        try
        {
            var userId = await GetUserIdAsync();
            var log = await _context.NotificationLogs
                .FirstOrDefaultAsync(nl => nl.Id == id && nl.UserId == userId);

            if (log == null)
                return NotFound(new { message = "Notificación no encontrada" });

            _context.NotificationLogs.Remove(log);
            await _context.SaveChangesAsync();

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al eliminar notificación {Id}", id);
            return StatusCode(500, new { message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Marcar todas las notificaciones como leídas
    /// </summary>
    [HttpPost("opened-all")]
    public async Task<ActionResult> MarkAllNotificationsAsOpened()
    {
        try
        {
            var userId = await GetUserIdAsync();
            var logs = await _context.NotificationLogs
                .Where(nl => nl.UserId == userId && nl.Status != "opened")
                .ToListAsync();

            foreach (var log in logs)
            {
                log.Status = "opened";
                log.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            return Ok(new { message = $"{logs.Count} notificaciones marcadas como leídas", count = logs.Count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al marcar todas las notificaciones como leídas");
            return StatusCode(500, new { message = "Error interno del servidor" });
        }
    }
}
