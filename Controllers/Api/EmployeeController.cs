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
/// Controlador de rutas para trabajadores/empleados
/// </summary>
[ApiController]
[Route("api/employee")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Employee")]
public class EmployeeController : ControllerBase
{
    private readonly IAppointmentService _appointmentService;
    private readonly IFinanceService _financeService;
    private readonly ILogger<EmployeeController> _logger;

    public EmployeeController(
        IAppointmentService appointmentService,
        IFinanceService financeService,
        ILogger<EmployeeController> logger)
    {
        _appointmentService = appointmentService;
        _financeService = financeService;
        _logger = logger;
    }

    private int GetEmployeeId()
    {
        var userId = JwtHelper.GetUserId(User);
        if (!userId.HasValue)
            throw new UnauthorizedAccessException("Trabajador no identificado");

        // Obtener EmployeeId desde el token (se agregará en el login)
        var employeeIdClaim = User.FindFirst("EmployeeId")?.Value;
        if (string.IsNullOrEmpty(employeeIdClaim) || !int.TryParse(employeeIdClaim, out var employeeId))
            throw new UnauthorizedAccessException("ID de trabajador no encontrado en el token");

        return employeeId;
    }

    private int GetOwnerBarberId()
    {
        var barberIdClaim = User.FindFirst("OwnerBarberId")?.Value;
        if (string.IsNullOrEmpty(barberIdClaim) || !int.TryParse(barberIdClaim, out var barberId))
            throw new UnauthorizedAccessException("ID de barbero dueño no encontrado en el token");

        return barberId;
    }

    /// <summary>
    /// Obtener todas las citas del barbero dueño (el trabajador ve todas para poder aceptarlas)
    /// </summary>
    [HttpGet("appointments")]
    public async Task<ActionResult<List<AppointmentDto>>> GetAppointments([FromQuery] string? date = null)
    {
        try
        {
            var ownerBarberId = GetOwnerBarberId();
            
            DateOnly? dateFilter = null;
            if (!string.IsNullOrEmpty(date) && DateOnly.TryParse(date, out var parsedDate))
                dateFilter = parsedDate;

            // Obtener TODAS las citas del barbero dueño (no filtrar por EmployeeId)
            // El trabajador necesita ver todas para poder aceptar las pendientes
            var appointments = await _appointmentService.GetBarberAppointmentsAsync(ownerBarberId, dateFilter, null);
            
            return Ok(appointments);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener citas");
            return StatusCode(500, new { message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Crear cita manual (trabajador)
    /// </summary>
    [HttpPost("appointments")]
    public async Task<ActionResult<AppointmentDto>> CreateAppointment([FromBody] CreateAppointmentRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var employeeId = GetEmployeeId();
            var ownerBarberId = GetOwnerBarberId();

            // Asegurar que la cita se asocie al barbero dueño y al trabajador
            request.BarberSlug = null; // No usar slug, usar ID directamente
            ModelState.Remove(nameof(request.BarberSlug));

            // Crear cita usando el método del barbero con EmployeeId
            var appointment = await _appointmentService.CreateAppointmentForBarberAsync(ownerBarberId, request, employeeId);
            
            return CreatedAtAction(nameof(GetAppointments), null, appointment);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear cita");
            return StatusCode(500, new { message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Obtener una cita por ID
    /// </summary>
    [HttpGet("appointments/{id}")]
    public async Task<ActionResult<AppointmentDto>> GetAppointment(int id)
    {
        try
        {
            var ownerBarberId = GetOwnerBarberId();
            var appointment = await _appointmentService.GetAppointmentByIdAsync(id);
            
            if (appointment == null || appointment.BarberId != ownerBarberId)
                return NotFound(new { message = "Cita no encontrada o no pertenece al barbero" });
            
            return Ok(appointment);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener cita {Id}", id);
            return StatusCode(500, new { message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Actualizar cita (aceptar, completar, asignar servicios, etc.)
    /// Si el trabajador acepta una cita pendiente (sin EmployeeId), se asigna automáticamente a él
    /// </summary>
    [HttpPut("appointments/{id}")]
    public async Task<ActionResult<AppointmentDto>> UpdateAppointment(int id, [FromBody] UpdateAppointmentRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var employeeId = GetEmployeeId();
            var ownerBarberId = GetOwnerBarberId();

            // Verificar que la cita pertenece al barbero dueño
            var appointment = await _appointmentService.GetAppointmentByIdAsync(id);
            if (appointment == null || appointment.BarberId != ownerBarberId)
                return NotFound(new { message = "Cita no encontrada o no pertenece al barbero" });

            // Actualizar la cita usando el método del barbero
            // Si la cita no tiene EmployeeId y el trabajador la acepta, se asignará automáticamente
            var updatedAppointment = await _appointmentService.UpdateAppointmentForBarberAsync(ownerBarberId, id, request, employeeId);
            
            return Ok(updatedAppointment);
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
            _logger.LogError(ex, "Error al actualizar cita {Id}", id);
            return StatusCode(500, new { message = "Error interno del servidor" });
        }
    }


    /// <summary>
    /// Obtener ingresos del trabajador
    /// </summary>
    [HttpGet("finances/income")]
    public async Task<ActionResult<TransactionsResponse>> GetIncome([FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null)
    {
        try
        {
            var employeeId = GetEmployeeId();
            var ownerBarberId = GetOwnerBarberId();

            // Obtener ingresos del barbero y filtrar por EmployeeId
            var income = await _financeService.GetIncomeAsync(ownerBarberId, startDate, endDate, 1, 1000);
            
            // Filtrar solo ingresos del trabajador
            var employeeIncome = new TransactionsResponse
            {
                Total = income.Items.Where(t => t.EmployeeId == employeeId).Sum(t => t.Amount),
                Items = income.Items.Where(t => t.EmployeeId == employeeId).ToList()
            };

            return Ok(employeeIncome);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener ingresos del trabajador");
            return StatusCode(500, new { message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Crear ingreso manual (trabajador)
    /// </summary>
    [HttpPost("finances/income")]
    public async Task<ActionResult<TransactionDto>> CreateIncome([FromBody] CreateIncomeRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var employeeId = GetEmployeeId();
            var ownerBarberId = GetOwnerBarberId();

            // Crear ingreso asociado al barbero dueño y al trabajador
            var income = await _financeService.CreateIncomeAsync(ownerBarberId, request, employeeId);
            
            return CreatedAtAction(nameof(GetIncome), null, income);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear ingreso");
            return StatusCode(500, new { message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Obtener egresos del trabajador
    /// </summary>
    [HttpGet("finances/expenses")]
    public async Task<ActionResult<TransactionsResponse>> GetExpenses([FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null)
    {
        try
        {
            var employeeId = GetEmployeeId();
            var ownerBarberId = GetOwnerBarberId();

            // Obtener egresos del barbero y filtrar por EmployeeId
            var expenses = await _financeService.GetExpensesAsync(ownerBarberId, startDate, endDate, 1, 1000);
            
            // Filtrar solo egresos del trabajador
            var employeeExpenses = new TransactionsResponse
            {
                Total = expenses.Items.Where(t => t.EmployeeId == employeeId).Sum(t => t.Amount),
                Items = expenses.Items.Where(t => t.EmployeeId == employeeId).ToList()
            };

            return Ok(employeeExpenses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener egresos del trabajador");
            return StatusCode(500, new { message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Crear egreso (trabajador)
    /// </summary>
    [HttpPost("finances/expenses")]
    public async Task<ActionResult<TransactionDto>> CreateExpense([FromBody] CreateExpenseRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var employeeId = GetEmployeeId();
            var ownerBarberId = GetOwnerBarberId();

            // Crear egreso asociado al barbero dueño y al trabajador
            var expense = await _financeService.CreateExpenseAsync(ownerBarberId, request, employeeId);
            
            return CreatedAtAction(nameof(GetExpenses), null, expense);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear egreso");
            return StatusCode(500, new { message = "Error interno del servidor" });
        }
    }
}

