using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BarberPro.Utils;
using BarberPro.Services.Interfaces;
using BarberPro.Models.DTOs.Requests;

namespace BarberPro.Controllers.Web;

[Authorize]
public class AdminController : Controller
{
    private readonly IDashboardService _dashboardService;
    private readonly IBarberService _barberService;
    private readonly IFinanceService _financeService;
    private readonly IServiceService _serviceService;
    private readonly IAppointmentService _appointmentService;

    public AdminController(
        IDashboardService dashboardService, 
        IBarberService barberService,
        IFinanceService financeService,
        IServiceService serviceService,
        IAppointmentService appointmentService)
    {
        _dashboardService = dashboardService;
        _barberService = barberService;
        _financeService = financeService;
        _serviceService = serviceService;
        _appointmentService = appointmentService;
    }

    [HttpGet("admin/dashboard")]
    [HttpGet("admin")]
    public async Task<IActionResult> Dashboard()
    {
        if (!SecurityHelper.IsAdministrator(User))
        {
            return Redirect("/access-denied");
        }
        
        var dashboard = await _dashboardService.GetAdminDashboardAsync();
        ViewBag.Nombre = SecurityHelper.GetUserFullName(User);
        ViewBag.Dashboard = dashboard;
        
        return View();
    }

    [HttpPost("admin/createbarber")]
    public async Task<IActionResult> CreateBarber([FromBody] CreateBarberRequest? request)
    {
        if (!SecurityHelper.IsAdministrator(User))
        {
            return Json(new { success = false, message = "No autorizado" });
        }

        if (request == null)
        {
            return Json(new { success = false, message = "Datos no recibidos" });
        }

        // Validar campos requeridos
        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return Json(new { success = false, message = "El email es requerido" });
        }

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            return Json(new { success = false, message = "La contraseña es requerida" });
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return Json(new { success = false, message = "El nombre es requerido" });
        }

        if (string.IsNullOrWhiteSpace(request.Phone))
        {
            return Json(new { success = false, message = "El teléfono es requerido" });
        }

        try
        {
            var barber = await _barberService.CreateBarberAsync(request);
            return Json(new { 
                success = true, 
                message = "Barbero creado exitosamente", 
                barber = new { 
                    id = barber.Id, 
                    name = barber.Name,
                    businessName = barber.BusinessName,
                    phone = barber.Phone
                } 
            });
        }
        catch (InvalidOperationException ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = $"Error al crear el barbero: {ex.Message}" });
        }
    }

    [HttpPut("admin/barbers/{id}/status")]
    [HttpPost("admin/barbers/{id}/status")]
    public async Task<IActionResult> UpdateBarberStatus(int id, [FromBody] UpdateBarberStatusRequest request)
    {
        if (!SecurityHelper.IsAdministrator(User))
        {
            return Json(new { success = false, message = "No autorizado" });
        }

        try
        {
            var updated = await _barberService.UpdateBarberStatusAsync(id, request.IsActive);
            if (!updated)
            {
                return Json(new { success = false, message = "Barbero no encontrado" });
            }

            return Json(new { success = true, message = "Estado actualizado exitosamente" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = $"Error al actualizar estado: {ex.Message}" });
        }
    }

    [HttpPut("admin/barbers/{id}")]
    [HttpPost("admin/barbers/{id}/update")]
    public async Task<IActionResult> UpdateBarber(int id, [FromBody] UpdateBarberProfileRequest request)
    {
        if (!SecurityHelper.IsAdministrator(User))
        {
            return Json(new { success = false, message = "No autorizado" });
        }

        if (request == null)
        {
            return Json(new { success = false, message = "Datos no recibidos" });
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return Json(new { success = false, message = "El nombre es requerido" });
        }

        if (string.IsNullOrWhiteSpace(request.Phone))
        {
            return Json(new { success = false, message = "El teléfono es requerido" });
        }

        try
        {
            var barber = await _barberService.UpdateBarberProfileAsync(id, request);
            return Json(new { 
                success = true, 
                message = "Barbero actualizado exitosamente",
                barber = new {
                    id = barber.Id,
                    name = barber.Name,
                    businessName = barber.BusinessName,
                    phone = barber.Phone
                }
            });
        }
        catch (KeyNotFoundException)
        {
            return Json(new { success = false, message = "Barbero no encontrado" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = $"Error al actualizar barbero: {ex.Message}" });
        }
    }

    [HttpDelete("admin/barbers/{id}")]
    [HttpPost("admin/barbers/{id}/delete")]
    public async Task<IActionResult> DeleteBarber(int id)
    {
        if (!SecurityHelper.IsAdministrator(User))
        {
            return Json(new { success = false, message = "No autorizado" });
        }

        try
        {
            var deleted = await _barberService.DeleteBarberAsync(id);
            if (!deleted)
            {
                return Json(new { success = false, message = "Barbero no encontrado" });
            }

            return Json(new { success = true, message = "Barbero eliminado exitosamente" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = $"Error al eliminar barbero: {ex.Message}" });
        }
    }

    [HttpGet("admin/barbers/{id}/dashboard")]
    public async Task<IActionResult> GetBarberDashboard(int id)
    {
        if (!SecurityHelper.IsAdministrator(User))
        {
            return Json(new { success = false, message = "No autorizado" });
        }

        try
        {
            var dashboard = await _dashboardService.GetBarberDashboardAsync(id);
            return Json(dashboard);
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = $"Error al obtener dashboard: {ex.Message}" });
        }
    }

    [HttpGet("admin/barbers/{id}/finances/summary")]
    public async Task<IActionResult> GetBarberFinanceSummary(int id)
    {
        if (!SecurityHelper.IsAdministrator(User))
        {
            return Json(new { success = false, message = "No autorizado" });
        }

        try
        {
            var summary = await _financeService.GetFinanceSummaryAsync(id);
            return Json(summary);
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = $"Error al obtener finanzas: {ex.Message}" });
        }
    }

    [HttpGet("admin/barbers/{id}/services")]
    public async Task<IActionResult> GetBarberServices(int id)
    {
        if (!SecurityHelper.IsAdministrator(User))
        {
            return Json(new { success = false, message = "No autorizado" });
        }

        try
        {
            var services = await _serviceService.GetBarberServicesAsync(id);
            return Json(services);
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = $"Error al obtener servicios: {ex.Message}" });
        }
    }

    [HttpGet("admin/barbers/{id}/appointments")]
    public async Task<IActionResult> GetBarberAppointments(int id)
    {
        if (!SecurityHelper.IsAdministrator(User))
        {
            return Json(new { success = false, message = "No autorizado" });
        }

        try
        {
            var appointments = await _appointmentService.GetBarberAppointmentsAsync(id);
            return Json(appointments);
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = $"Error al obtener citas: {ex.Message}" });
        }
    }
}

public class UpdateBarberStatusRequest
{
    public bool IsActive { get; set; }
}

