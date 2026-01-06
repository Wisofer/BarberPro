using BarberPro.Models.DTOs.Requests;
using BarberPro.Models.DTOs.Responses;
using BarberPro.Models.Entities;
using BarberPro.Services.Interfaces;
using BarberPro.Utils;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BarberPro.Controllers.Api;

/// <summary>
/// Controlador de rutas del barbero
/// </summary>
[ApiController]
[Route("api/barber")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Barber")]
public class BarberController : ControllerBase
{
    private readonly IBarberService _barberService;
    private readonly IAppointmentService _appointmentService;
    private readonly IServiceService _serviceService;
    private readonly IFinanceService _financeService;
    private readonly IDashboardService _dashboardService;
    private readonly IAuthService _authService;
    private readonly IWorkingHoursService _workingHoursService;
    private readonly IExportService _exportService;
    private readonly IHelpSupportService _helpSupportService;
    private readonly ILogger<BarberController> _logger;

    public BarberController(
        IBarberService barberService,
        IAppointmentService appointmentService,
        IServiceService serviceService,
        IFinanceService financeService,
        IDashboardService dashboardService,
        IAuthService authService,
        IWorkingHoursService workingHoursService,
        IExportService exportService,
        IHelpSupportService helpSupportService,
        ILogger<BarberController> logger)
    {
        _barberService = barberService;
        _appointmentService = appointmentService;
        _serviceService = serviceService;
        _financeService = financeService;
        _dashboardService = dashboardService;
        _authService = authService;
        _workingHoursService = workingHoursService;
        _exportService = exportService;
        _helpSupportService = helpSupportService;
        _logger = logger;
    }

    private int GetBarberId()
    {
        var barberId = JwtHelper.GetBarberId(User);
        if (!barberId.HasValue)
            throw new UnauthorizedAccessException("Barbero no identificado");
        return barberId.Value;
    }

    /// <summary>
    /// Obtener dashboard del barbero
    /// </summary>
    [HttpGet("dashboard")]
    public async Task<ActionResult<BarberDashboardDto>> GetDashboard()
    {
        try
        {
            var barberId = GetBarberId();
            var dashboard = await _dashboardService.GetBarberDashboardAsync(barberId);
            return Ok(dashboard);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener dashboard");
            return StatusCode(500, new { message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Obtener perfil del barbero
    /// </summary>
    [HttpGet("profile")]
    public async Task<ActionResult<BarberDto>> GetProfile()
    {
        try
        {
            var barberId = GetBarberId();
            var profile = await _barberService.GetBarberProfileAsync(barberId);
            return Ok(profile);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Barbero no encontrado" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener perfil");
            return StatusCode(500, new { message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Actualizar perfil del barbero
    /// </summary>
    [HttpPut("profile")]
    public async Task<ActionResult<BarberDto>> UpdateProfile([FromBody] UpdateBarberProfileRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var barberId = GetBarberId();
            var profile = await _barberService.UpdateBarberProfileAsync(barberId, request);
            return Ok(profile);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Barbero no encontrado" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar perfil");
            return StatusCode(500, new { message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Obtener URL del QR
    /// </summary>
    [HttpGet("qr-url")]
    public async Task<ActionResult<QrUrlResponse>> GetQrUrl()
    {
        try
        {
            var barberId = GetBarberId();
            var url = await _barberService.GetQrUrlAsync(barberId);
            var qrCode = QrHelper.GenerateQrCodeBase64(url);

            return Ok(new QrUrlResponse
            {
                Url = url,
                QrCode = qrCode
            });
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Barbero no encontrado" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener QR");
            return StatusCode(500, new { message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Obtener citas del barbero
    /// </summary>
    [HttpGet("appointments")]
    public async Task<ActionResult<List<AppointmentDto>>> GetAppointments([FromQuery] DateOnly? date = null, [FromQuery] AppointmentStatus? status = null)
    {
        try
        {
            var barberId = GetBarberId();
            var appointments = await _appointmentService.GetBarberAppointmentsAsync(barberId, date, status);
            return Ok(appointments);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener citas");
            return StatusCode(500, new { message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Crear cita manual (el barbero puede crear citas sin necesidad de barberSlug)
    /// </summary>
    [HttpPost("appointments")]
    public async Task<ActionResult<AppointmentDto>> CreateAppointment([FromBody] CreateAppointmentRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var barberId = GetBarberId();
            // El barbero no necesita pasar barberSlug, se usa su barberId del token
            // Ignorar barberSlug si viene en el request
            request.BarberSlug = null;
            var appointment = await _appointmentService.CreateAppointmentForBarberAsync(barberId, request);
            return CreatedAtAction(nameof(GetAppointments), null, appointment);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear cita");
            return StatusCode(500, new { message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Actualizar cita (solo del barbero autenticado)
    /// </summary>
    [HttpPut("appointments/{id}")]
    public async Task<ActionResult<AppointmentDto>> UpdateAppointment(int id, [FromBody] UpdateAppointmentRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var barberId = GetBarberId();
            var appointment = await _appointmentService.UpdateAppointmentForBarberAsync(barberId, id, request);
            return Ok(appointment);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar cita {Id}", id);
            return StatusCode(500, new { message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Eliminar cita (solo del barbero autenticado)
    /// </summary>
    [HttpDelete("appointments/{id}")]
    public async Task<ActionResult> DeleteAppointment(int id)
    {
        try
        {
            var barberId = GetBarberId();
            var deleted = await _appointmentService.DeleteAppointmentForBarberAsync(barberId, id);
            if (!deleted)
                return NotFound(new { message = "Cita no encontrada o no pertenece al barbero" });

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al eliminar cita {Id}", id);
            return StatusCode(500, new { message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Obtener servicios del barbero
    /// </summary>
    [HttpGet("services")]
    public async Task<ActionResult<List<ServiceDto>>> GetServices()
    {
        try
        {
            var barberId = GetBarberId();
            var services = await _serviceService.GetBarberServicesAsync(barberId);
            return Ok(services);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener servicios");
            return StatusCode(500, new { message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Crear servicio
    /// </summary>
    [HttpPost("services")]
    public async Task<ActionResult<ServiceDto>> CreateService([FromBody] CreateServiceRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var barberId = GetBarberId();
            var service = await _serviceService.CreateServiceAsync(barberId, request);
            return CreatedAtAction(nameof(GetServices), null, service);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear servicio");
            return StatusCode(500, new { message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Obtener resumen financiero
    /// </summary>
    [HttpGet("finances/summary")]
    public async Task<ActionResult<FinanceSummaryDto>> GetFinanceSummary([FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null)
    {
        try
        {
            var barberId = GetBarberId();
            var summary = await _financeService.GetFinanceSummaryAsync(barberId, startDate, endDate);
            return Ok(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener resumen financiero");
            return StatusCode(500, new { message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Obtener ingresos
    /// </summary>
    [HttpGet("finances/income")]
    public async Task<ActionResult<TransactionsResponse>> GetIncome([FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        try
        {
            var barberId = GetBarberId();
            var income = await _financeService.GetIncomeAsync(barberId, startDate, endDate, page, pageSize);
            return Ok(income);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener ingresos");
            return StatusCode(500, new { message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Obtener egresos
    /// </summary>
    [HttpGet("finances/expenses")]
    public async Task<ActionResult<TransactionsResponse>> GetExpenses([FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        try
        {
            var barberId = GetBarberId();
            var expenses = await _financeService.GetExpensesAsync(barberId, startDate, endDate, page, pageSize);
            return Ok(expenses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener egresos");
            return StatusCode(500, new { message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Crear egreso
    /// </summary>
    [HttpPost("finances/expenses")]
    public async Task<ActionResult<TransactionDto>> CreateExpense([FromBody] CreateExpenseRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var barberId = GetBarberId();
            var expense = await _financeService.CreateExpenseAsync(barberId, request);
            return CreatedAtAction(nameof(GetExpenses), null, expense);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear egreso");
            return StatusCode(500, new { message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Cambiar contraseña del barbero
    /// </summary>
    [HttpPost("change-password")]
    public async Task<ActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var userId = JwtHelper.GetUserId(User);
            if (!userId.HasValue)
                return Unauthorized(new { message = "Usuario no identificado" });

            var success = await _authService.ChangePasswordAsync(userId.Value, request.CurrentPassword, request.NewPassword);
            if (!success)
                return BadRequest(new { message = "La contraseña actual es incorrecta" });

            return Ok(new { message = "Contraseña actualizada exitosamente" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al cambiar contraseña");
            return StatusCode(500, new { message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Obtener todos los horarios de trabajo del barbero
    /// </summary>
    [HttpGet("working-hours")]
    public async Task<ActionResult<List<WorkingHoursDto>>> GetWorkingHours()
    {
        try
        {
            var barberId = GetBarberId();
            var workingHours = await _workingHoursService.GetWorkingHoursAsync(barberId);
            return Ok(workingHours);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener horarios de trabajo");
            return StatusCode(500, new { message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Actualizar o crear horarios de trabajo (upsert)
    /// Si el horario para ese día ya existe, lo actualiza; si no, lo crea
    /// </summary>
    [HttpPut("working-hours")]
    public async Task<ActionResult<List<WorkingHoursDto>>> UpdateWorkingHours([FromBody] UpdateWorkingHoursBatchRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        if (request.WorkingHours == null || !request.WorkingHours.Any())
            return BadRequest(new { message = "Debe incluir al menos un horario" });

        try
        {
            var barberId = GetBarberId();
            var workingHours = await _workingHoursService.UpdateWorkingHoursAsync(barberId, request.WorkingHours);
            return Ok(workingHours);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar horarios de trabajo");
            return StatusCode(500, new { message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Eliminar un horario de trabajo específico
    /// </summary>
    [HttpDelete("working-hours/{id}")]
    public async Task<ActionResult> DeleteWorkingHours(int id)
    {
        try
        {
            var barberId = GetBarberId();
            var deleted = await _workingHoursService.DeleteWorkingHoursAsync(barberId, id);
            
            if (!deleted)
                return NotFound(new { message = "Horario de trabajo no encontrado" });

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al eliminar horario de trabajo");
            return StatusCode(500, new { message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Exportar reporte de citas
    /// </summary>
    [HttpGet("export/appointments")]
    public async Task<ActionResult> ExportAppointments([FromQuery] string? startDate = null, [FromQuery] string? endDate = null, [FromQuery] string format = "csv")
    {
        try
        {
            var barberId = GetBarberId();
            DateOnly? start = null;
            DateOnly? end = null;

            if (!string.IsNullOrEmpty(startDate) && DateOnly.TryParse(startDate, out var parsedStart))
                start = parsedStart;
            if (!string.IsNullOrEmpty(endDate) && DateOnly.TryParse(endDate, out var parsedEnd))
                end = parsedEnd;

            var fileBytes = await _exportService.ExportAppointmentsAsync(barberId, start, end, format);
            var contentType = format.ToLower() switch
            {
                "csv" => "text/csv",
                "excel" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "pdf" => "application/pdf",
                _ => "application/octet-stream"
            };
            var fileName = $"citas_{DateTime.UtcNow:yyyyMMdd}.{format.ToLower()}";

            return File(fileBytes, contentType, fileName);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al exportar citas");
            return StatusCode(500, new { message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Exportar reporte financiero
    /// </summary>
    [HttpGet("export/finances")]
    public async Task<ActionResult> ExportFinances([FromQuery] string? startDate = null, [FromQuery] string? endDate = null, [FromQuery] string format = "csv")
    {
        try
        {
            var barberId = GetBarberId();
            DateOnly? start = null;
            DateOnly? end = null;

            if (!string.IsNullOrEmpty(startDate) && DateOnly.TryParse(startDate, out var parsedStart))
                start = parsedStart;
            if (!string.IsNullOrEmpty(endDate) && DateOnly.TryParse(endDate, out var parsedEnd))
                end = parsedEnd;

            var fileBytes = await _exportService.ExportFinancesAsync(barberId, start, end, format);
            var contentType = format.ToLower() switch
            {
                "csv" => "text/csv",
                "excel" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "pdf" => "application/pdf",
                _ => "application/octet-stream"
            };
            var fileName = $"finanzas_{DateTime.UtcNow:yyyyMMdd}.{format.ToLower()}";

            return File(fileBytes, contentType, fileName);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al exportar finanzas");
            return StatusCode(500, new { message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Exportar reporte de clientes
    /// </summary>
    [HttpGet("export/clients")]
    public async Task<ActionResult> ExportClients([FromQuery] string format = "csv")
    {
        try
        {
            var barberId = GetBarberId();
            var fileBytes = await _exportService.ExportClientsAsync(barberId, format);
            var contentType = format.ToLower() switch
            {
                "csv" => "text/csv",
                "excel" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "pdf" => "application/pdf",
                _ => "application/octet-stream"
            };
            var fileName = $"clientes_{DateTime.UtcNow:yyyyMMdd}.{format.ToLower()}";

            return File(fileBytes, contentType, fileName);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al exportar clientes");
            return StatusCode(500, new { message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Crear backup completo de datos
    /// </summary>
    [HttpGet("export/backup")]
    public async Task<ActionResult> ExportBackup()
    {
        try
        {
            var barberId = GetBarberId();
            var fileBytes = await _exportService.ExportBackupAsync(barberId);
            var fileName = $"backup_{DateTime.UtcNow:yyyyMMdd_HHmmss}.json";

            return File(fileBytes, "application/json", fileName);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear backup");
            return StatusCode(500, new { message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Obtener información de ayuda y soporte
    /// </summary>
    [HttpGet("help-support")]
    public async Task<ActionResult<HelpSupportDto>> GetHelpSupport()
    {
        try
        {
            var helpSupport = await _helpSupportService.GetHelpSupportAsync();
            return Ok(helpSupport);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener ayuda y soporte");
            return StatusCode(500, new { message = "Error interno del servidor" });
        }
    }
}

