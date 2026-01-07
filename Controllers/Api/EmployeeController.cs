using BarberNic.Models.DTOs.Requests;
using BarberNic.Models.DTOs.Responses;
using BarberNic.Services.Interfaces;
using BarberNic.Utils;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BarberNic.Controllers.Api;

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
    private readonly IServiceService _serviceService;
    private readonly IAuthService _authService;
    private readonly IBarberService _barberService;
    private readonly IEmployeeService _employeeService;
    private readonly ILogger<EmployeeController> _logger;

    public EmployeeController(
        IAppointmentService appointmentService,
        IFinanceService financeService,
        IServiceService serviceService,
        IAuthService authService,
        IBarberService barberService,
        IEmployeeService employeeService,
        ILogger<EmployeeController> logger)
    {
        _appointmentService = appointmentService;
        _financeService = financeService;
        _serviceService = serviceService;
        _authService = authService;
        _barberService = barberService;
        _employeeService = employeeService;
        _logger = logger;
    }

    private async Task<int> GetEmployeeIdAsync()
    {
        var userId = JwtHelper.GetUserId(User);
        if (!userId.HasValue)
            throw new UnauthorizedAccessException("Trabajador no identificado");

        // Obtener EmployeeId desde el token (se agregará en el login)
        var employeeIdClaim = User.FindFirst("EmployeeId")?.Value;
        if (string.IsNullOrEmpty(employeeIdClaim) || !int.TryParse(employeeIdClaim, out var employeeId))
            throw new UnauthorizedAccessException("ID de trabajador no encontrado en el token");

        // Verificar que el empleado existe y está activo
        var ownerBarberId = await GetOwnerBarberIdAsync();
        var employee = await _employeeService.GetEmployeeByIdAsync(employeeId, ownerBarberId);
        if (employee == null)
            throw new UnauthorizedAccessException("Empleado no encontrado o fue eliminado");
        
        if (!employee.IsActive)
            throw new UnauthorizedAccessException("Empleado desactivado");

        return employeeId;
    }

    private async Task<int> GetOwnerBarberIdAsync()
    {
        var barberIdClaim = User.FindFirst("OwnerBarberId")?.Value;
        if (string.IsNullOrEmpty(barberIdClaim) || !int.TryParse(barberIdClaim, out var barberId))
            throw new UnauthorizedAccessException("ID de barbero dueño no encontrado en el token");

        // Verificar que el barbero dueño existe y está activo
        var barber = await _barberService.GetBarberByIdAsync(barberId);
        if (barber == null)
            throw new UnauthorizedAccessException("Barbero dueño no encontrado o fue eliminado");
        
        if (!barber.IsActive)
            throw new UnauthorizedAccessException("Barbero dueño desactivado");

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
            var ownerBarberId = await GetOwnerBarberIdAsync();
            
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
            var employeeId = await GetEmployeeIdAsync();
            var ownerBarberId = await GetOwnerBarberIdAsync();

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
            var ownerBarberId = await GetOwnerBarberIdAsync();
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
            var employeeId = await GetEmployeeIdAsync();
            var ownerBarberId = await GetOwnerBarberIdAsync();

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
    public async Task<ActionResult<TransactionsResponse>> GetIncome([FromQuery] string? startDate = null, [FromQuery] string? endDate = null)
    {
        try
        {
            var employeeId = await GetEmployeeIdAsync();
            var ownerBarberId = await GetOwnerBarberIdAsync();

            // Parsear y normalizar fechas desde string
            DateTime? parsedStartDate = null;
            DateTime? parsedEndDate = null;
            
            if (!string.IsNullOrEmpty(startDate) && DateTime.TryParse(startDate, out var start))
            {
                parsedStartDate = NormalizeDateForFilter(start, isEndDate: false);
            }
            
            if (!string.IsNullOrEmpty(endDate) && DateTime.TryParse(endDate, out var end))
            {
                parsedEndDate = NormalizeDateForFilter(end, isEndDate: true);
            }

            // Obtener ingresos del barbero y filtrar por EmployeeId
            var income = await _financeService.GetIncomeAsync(ownerBarberId, parsedStartDate, parsedEndDate, 1, 1000);
            
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
            var employeeId = await GetEmployeeIdAsync();
            var ownerBarberId = await GetOwnerBarberIdAsync();

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
    public async Task<ActionResult<TransactionsResponse>> GetExpenses([FromQuery] string? startDate = null, [FromQuery] string? endDate = null)
    {
        try
        {
            var employeeId = await GetEmployeeIdAsync();
            var ownerBarberId = await GetOwnerBarberIdAsync();

            // Parsear y normalizar fechas desde string
            DateTime? parsedStartDate = null;
            DateTime? parsedEndDate = null;
            
            if (!string.IsNullOrEmpty(startDate) && DateTime.TryParse(startDate, out var start))
            {
                parsedStartDate = NormalizeDateForFilter(start, isEndDate: false);
            }
            
            if (!string.IsNullOrEmpty(endDate) && DateTime.TryParse(endDate, out var end))
            {
                parsedEndDate = NormalizeDateForFilter(end, isEndDate: true);
            }

            // Obtener egresos del barbero y filtrar por EmployeeId
            var expenses = await _financeService.GetExpensesAsync(ownerBarberId, parsedStartDate, parsedEndDate, 1, 1000);
            
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
            var employeeId = await GetEmployeeIdAsync();
            var ownerBarberId = await GetOwnerBarberIdAsync();

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

    #region Servicios (Solo Lectura)

    /// <summary>
    /// Obtener todos los servicios del barbero dueño (solo lectura)
    /// El trabajador puede ver los servicios para crear citas, pero no puede crear, editar ni borrar
    /// </summary>
    [HttpGet("services")]
    public async Task<ActionResult<List<ServiceDto>>> GetServices()
    {
        try
        {
            var ownerBarberId = await GetOwnerBarberIdAsync();
            
            // Obtener servicios del barbero dueño (solo lectura)
            var services = await _serviceService.GetBarberServicesAsync(ownerBarberId);
            
            return Ok(services);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener servicios");
            return StatusCode(500, new { message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Obtener un servicio por ID (solo lectura)
    /// </summary>
    [HttpGet("services/{id}")]
    public async Task<ActionResult<ServiceDto>> GetService(int id)
    {
        try
        {
            var ownerBarberId = await GetOwnerBarberIdAsync();
            
            // Obtener todos los servicios del barbero y buscar el específico
            // Esto asegura que solo se pueda acceder a servicios del barbero dueño
            var services = await _serviceService.GetBarberServicesAsync(ownerBarberId);
            var service = services.FirstOrDefault(s => s.Id == id);
            
            if (service == null)
                return NotFound(new { message = "Servicio no encontrado o no pertenece al barbero" });
            
            return Ok(service);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener servicio {Id}", id);
            return StatusCode(500, new { message = "Error interno del servidor" });
        }
    }

    #endregion

    /// <summary>
    /// Cambiar contraseña del trabajador/empleado
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
            _logger.LogError(ex, "Error al cambiar contraseña del empleado");
            return StatusCode(500, new { message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Normaliza una fecha para el filtro, asegurando que esté en UTC y maneje correctamente los formatos
    /// </summary>
    private DateTime NormalizeDateForFilter(DateTime date, bool isEndDate)
    {
        // Si la fecha viene sin hora (00:00:00), normalizar según si es inicio o fin
        if (date.TimeOfDay.TotalSeconds < 1)
        {
            if (isEndDate)
            {
                // Fin del día: 23:59:59.999
                return new DateTime(date.Year, date.Month, date.Day, 23, 59, 59, 999, DateTimeKind.Utc);
            }
            else
            {
                // Inicio del día: 00:00:00
                return new DateTime(date.Year, date.Month, date.Day, 0, 0, 0, DateTimeKind.Utc);
            }
        }
        
        // Si tiene hora específica, crear directamente en UTC (asumir que viene en UTC)
        // Extraer componentes para evitar problemas de zona horaria
        return new DateTime(date.Year, date.Month, date.Day, date.Hour, date.Minute, date.Second, date.Millisecond, DateTimeKind.Utc);
    }
}

