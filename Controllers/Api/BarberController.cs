using BarberPro.Models.DTOs.Requests;
using BarberPro.Models.DTOs.Responses;
using BarberPro.Models.Entities;
using BarberPro.Services.Interfaces;
using BarberPro.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BarberPro.Controllers.Api;

/// <summary>
/// Controlador de rutas del barbero
/// </summary>
[ApiController]
[Route("api/barber")]
[Authorize(Roles = "Barber")]
public class BarberController : ControllerBase
{
    private readonly IBarberService _barberService;
    private readonly IAppointmentService _appointmentService;
    private readonly IServiceService _serviceService;
    private readonly IFinanceService _financeService;
    private readonly IDashboardService _dashboardService;
    private readonly ILogger<BarberController> _logger;

    public BarberController(
        IBarberService barberService,
        IAppointmentService appointmentService,
        IServiceService serviceService,
        IFinanceService financeService,
        IDashboardService dashboardService,
        ILogger<BarberController> logger)
    {
        _barberService = barberService;
        _appointmentService = appointmentService;
        _serviceService = serviceService;
        _financeService = financeService;
        _dashboardService = dashboardService;
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
    /// Crear cita manual
    /// </summary>
    [HttpPost("appointments")]
    public async Task<ActionResult<AppointmentDto>> CreateAppointment([FromBody] CreateAppointmentRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var appointment = await _appointmentService.CreateAppointmentAsync(request);
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
    /// Actualizar cita
    /// </summary>
    [HttpPut("appointments/{id}")]
    public async Task<ActionResult<AppointmentDto>> UpdateAppointment(int id, [FromBody] UpdateAppointmentRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var appointment = await _appointmentService.UpdateAppointmentAsync(id, request);
            return Ok(appointment);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Cita no encontrada" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar cita {Id}", id);
            return StatusCode(500, new { message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Eliminar cita
    /// </summary>
    [HttpDelete("appointments/{id}")]
    public async Task<ActionResult> DeleteAppointment(int id)
    {
        try
        {
            var deleted = await _appointmentService.DeleteAppointmentAsync(id);
            if (!deleted)
                return NotFound(new { message = "Cita no encontrada" });

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
}

