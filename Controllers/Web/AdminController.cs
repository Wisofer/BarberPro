using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BarberNic.Utils;
using BarberNic.Services.Interfaces;
using BarberNic.Models.DTOs.Requests;
using BarberNic.Models.Entities;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace BarberNic.Controllers.Web;

[Authorize]
public class AdminController : Controller
{
    private readonly IDashboardService _dashboardService;
    private readonly IBarberService _barberService;
    private readonly IFinanceService _financeService;
    private readonly IServiceService _serviceService;
    private readonly IAppointmentService _appointmentService;
    private readonly IEmployeeService _employeeService;
    private readonly IReportService _reportService;
    private readonly IPushNotificationService _pushNotificationService;
    private readonly BarberNic.Data.ApplicationDbContext _context;

    public AdminController(
        IDashboardService dashboardService, 
        IBarberService barberService,
        IFinanceService financeService,
        IServiceService serviceService,
        IAppointmentService appointmentService,
        IEmployeeService employeeService,
        IReportService reportService,
        IPushNotificationService pushNotificationService,
        BarberNic.Data.ApplicationDbContext context)
    {
        _dashboardService = dashboardService;
        _barberService = barberService;
        _financeService = financeService;
        _serviceService = serviceService;
        _appointmentService = appointmentService;
        _employeeService = employeeService;
        _reportService = reportService;
        _pushNotificationService = pushNotificationService;
        _context = context;
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

    [HttpGet("admin/barbers")]
    public async Task<IActionResult> Barbers()
    {
        if (!SecurityHelper.IsAdministrator(User))
        {
            return Redirect("/access-denied");
        }

        try
        {
            var dashboard = await _dashboardService.GetAdminDashboardAsync();
            ViewBag.Barbers = dashboard.RecentBarbers;
            ViewBag.Nombre = SecurityHelper.GetUserFullName(User);
            return View();
        }
        catch (Exception)
        {
            ViewBag.Barbers = new List<BarberNic.Models.DTOs.Responses.BarberSummaryDto>();
            ViewBag.Nombre = SecurityHelper.GetUserFullName(User);
            return View();
        }
    }

    [HttpGet("admin/employees")]
    public async Task<IActionResult> Employees()
    {
        if (!SecurityHelper.IsAdministrator(User))
        {
            return Redirect("/access-denied");
        }

        try
        {
            // Obtener todos los barberos para el filtro
            var barbers = await _barberService.GetAllBarbersAsync();
            
            // Obtener todos los empleados de todos los barberos
            var allEmployees = new List<BarberNic.Models.DTOs.Responses.EmployeeDto>();
            foreach (var barber in barbers)
            {
                try
                {
                    var employees = await _employeeService.GetEmployeesAsync(barber.Id);
                    allEmployees.AddRange(employees);
                }
                catch
                {
                    // Continuar si hay error con un barbero específico
                }
            }

            ViewBag.Employees = allEmployees;
            ViewBag.Barbers = barbers;
            ViewBag.Nombre = SecurityHelper.GetUserFullName(User);
            return View();
        }
        catch (Exception)
        {
            ViewBag.Employees = new List<BarberNic.Models.DTOs.Responses.EmployeeDto>();
            ViewBag.Barbers = new List<BarberNic.Models.DTOs.Responses.BarberDto>();
            ViewBag.Nombre = SecurityHelper.GetUserFullName(User);
            return View();
        }
    }

    [HttpGet("admin/appointments")]
    public async Task<IActionResult> Appointments()
    {
        if (!SecurityHelper.IsAdministrator(User))
        {
            return Redirect("/access-denied");
        }

        try
        {
            // Obtener todos los barberos para el filtro
            var barbers = await _barberService.GetAllBarbersAsync();
            
            // Obtener todas las citas de todos los barberos
            var allAppointments = new List<BarberNic.Models.DTOs.Responses.AppointmentDto>();
            foreach (var barber in barbers)
            {
                try
                {
                    var appointments = await _appointmentService.GetBarberAppointmentsAsync(barber.Id);
                    allAppointments.AddRange(appointments);
                }
                catch
                {
                    // Continuar si hay error con un barbero específico
                }
            }

            ViewBag.Appointments = allAppointments.OrderByDescending(a => a.Date).ThenByDescending(a => a.Time).ToList();
            ViewBag.Barbers = barbers;
            ViewBag.Nombre = SecurityHelper.GetUserFullName(User);
            return View();
        }
        catch (Exception)
        {
            ViewBag.Appointments = new List<BarberNic.Models.DTOs.Responses.AppointmentDto>();
            ViewBag.Barbers = new List<BarberNic.Models.DTOs.Responses.BarberDto>();
            ViewBag.Nombre = SecurityHelper.GetUserFullName(User);
            return View();
        }
    }

    [HttpGet("admin/reports")]
    public async Task<IActionResult> Reports()
    {
        if (!SecurityHelper.IsAdministrator(User))
        {
            return Redirect("/access-denied");
        }

        try
        {
            var barbers = await _barberService.GetAllBarbersAsync();
            ViewBag.Barbers = barbers;
            ViewBag.Nombre = SecurityHelper.GetUserFullName(User);
            return View();
        }
        catch (Exception)
        {
            ViewBag.Barbers = new List<BarberNic.Models.DTOs.Responses.BarberDto>();
            ViewBag.Nombre = SecurityHelper.GetUserFullName(User);
            return View();
        }
    }

    [HttpGet("admin/settings")]
    public IActionResult Settings()
    {
        if (!SecurityHelper.IsAdministrator(User))
        {
            return Redirect("/access-denied");
        }

        ViewBag.Nombre = SecurityHelper.GetUserFullName(User);
        return View();
    }

    [HttpGet("admin/notifications")]
    public async Task<IActionResult> Notifications()
    {
        if (!SecurityHelper.IsAdministrator(User))
        {
            return Redirect("/access-denied");
        }

        try
        {
            var templates = await _context.Templates
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
            
            // Obtener solo barberos que tienen dispositivos con token registrado
            var barbersWithToken = await _context.Barbers
                .Where(b => _context.Devices.Any(d => d.UserId == b.UserId && !string.IsNullOrWhiteSpace(d.FcmToken)))
                .Select(b => new { 
                    b.Id, 
                    b.UserId, 
                    b.Name, 
                    b.BusinessName,
                    DeviceCount = _context.Devices.Count(d => d.UserId == b.UserId && !string.IsNullOrWhiteSpace(d.FcmToken))
                })
                .ToListAsync();
            
            ViewBag.Templates = templates;
            ViewBag.BarbersWithUserId = barbersWithToken;
            ViewBag.Nombre = SecurityHelper.GetUserFullName(User);
            return View();
        }
        catch (Exception)
        {
            ViewBag.Templates = new List<BarberNic.Models.Entities.Template>();
            ViewBag.BarbersWithUserId = new List<dynamic>();
            ViewBag.Nombre = SecurityHelper.GetUserFullName(User);
            return View();
        }
    }

    [HttpPost("admin/notifications/create-template")]
    public async Task<IActionResult> CreateTemplate([FromBody] CreateTemplateRequest? request)
    {
        if (!SecurityHelper.IsAdministrator(User))
        {
            return Json(new { success = false, message = "No autorizado" });
        }

        if (request == null)
        {
            return Json(new { success = false, message = "Datos no recibidos" });
        }

        if (string.IsNullOrWhiteSpace(request.Title))
        {
            return Json(new { success = false, message = "El título es requerido" });
        }

        if (string.IsNullOrWhiteSpace(request.Body))
        {
            return Json(new { success = false, message = "El cuerpo del mensaje es requerido" });
        }

        try
        {
            var template = new BarberNic.Models.Entities.Template
            {
                Title = request.Title,
                Body = request.Body,
                ImageUrl = request.ImageUrl,
                Name = request.Name
            };

            _context.Templates.Add(template);
            await _context.SaveChangesAsync();

            return Json(new { 
                success = true, 
                message = "Plantilla creada exitosamente",
                template = new {
                    id = template.Id,
                    title = template.Title,
                    body = template.Body,
                    imageUrl = template.ImageUrl,
                    name = template.Name
                }
            });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = $"Error al crear plantilla: {ex.Message}" });
        }
    }

    [HttpPut("admin/notifications/update-template/{id}")]
    [HttpPost("admin/notifications/update-template/{id}")]
    public async Task<IActionResult> UpdateTemplate(int id, [FromBody] CreateTemplateRequest? request)
    {
        if (!SecurityHelper.IsAdministrator(User))
        {
            return Json(new { success = false, message = "No autorizado" });
        }

        if (request == null)
        {
            return Json(new { success = false, message = "Datos no recibidos" });
        }

        if (string.IsNullOrWhiteSpace(request.Title))
        {
            return Json(new { success = false, message = "El título es requerido" });
        }

        if (string.IsNullOrWhiteSpace(request.Body))
        {
            return Json(new { success = false, message = "El cuerpo del mensaje es requerido" });
        }

        try
        {
            var template = await _context.Templates.FindAsync(id);
            if (template == null)
            {
                return Json(new { success = false, message = "Plantilla no encontrada" });
            }

            template.Title = request.Title;
            template.Body = request.Body;
            template.ImageUrl = request.ImageUrl;
            template.Name = request.Name;
            template.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Json(new { 
                success = true, 
                message = "Plantilla actualizada exitosamente",
                template = new {
                    id = template.Id,
                    title = template.Title,
                    body = template.Body,
                    imageUrl = template.ImageUrl,
                    name = template.Name
                }
            });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = $"Error al actualizar plantilla: {ex.Message}" });
        }
    }

    [HttpDelete("admin/notifications/delete-template/{id}")]
    [HttpPost("admin/notifications/delete-template/{id}")]
    public async Task<IActionResult> DeleteTemplate(int id)
    {
        if (!SecurityHelper.IsAdministrator(User))
        {
            return Json(new { success = false, message = "No autorizado" });
        }

        try
        {
            var template = await _context.Templates.FindAsync(id);
            if (template == null)
            {
                return Json(new { success = false, message = "Plantilla no encontrada" });
            }

            _context.Templates.Remove(template);
            await _context.SaveChangesAsync();

            return Json(new { 
                success = true, 
                message = "Plantilla eliminada exitosamente"
            });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = $"Error al eliminar plantilla: {ex.Message}" });
        }
    }

    [HttpPost("admin/notifications/send")]
    public async Task<IActionResult> SendNotification([FromBody] SendNotificationRequest? request)
    {
        if (!SecurityHelper.IsAdministrator(User))
        {
            return Json(new { success = false, message = "No autorizado" });
        }

        if (request == null)
        {
            return Json(new { success = false, message = "Datos no recibidos" });
        }

        try
        {
            var template = await _context.Templates.FindAsync(request.TemplateId);
            if (template == null)
            {
                return Json(new { success = false, message = "Plantilla no encontrada" });
            }

            // Determinar a qué usuarios enviar
            List<BarberNic.Models.Entities.Device> targetDevices;
            
            if (request.UserIds != null && request.UserIds.Any())
            {
                // Enviar solo a usuarios seleccionados
                targetDevices = await _context.Devices
                    .Where(d => request.UserIds.Contains(d.UserId) && !string.IsNullOrWhiteSpace(d.FcmToken))
                    .Include(d => d.User)
                    .ToListAsync();
                
                if (!targetDevices.Any())
                {
                    return Json(new { success = false, message = "No hay dispositivos registrados para los usuarios seleccionados" });
                }
            }
            else
            {
                // Enviar a todos los usuarios
                targetDevices = await _context.Devices
                    .Where(d => !string.IsNullOrWhiteSpace(d.FcmToken))
                    .Include(d => d.User)
                    .ToListAsync();
                
                if (!targetDevices.Any())
                {
                    return Json(new { success = false, message = "No hay dispositivos registrados" });
                }
            }

            await _pushNotificationService.SendPushNotificationAsync(
                template,
                targetDevices,
                request.ExtraData,
                request.DataOnly);

            var uniqueUsers = targetDevices.Select(d => d.UserId).Distinct().Count();
            
            return Json(new { 
                success = true, 
                message = $"Notificación enviada a {uniqueUsers} usuario(s) en {targetDevices.Count} dispositivo(s)",
                sentCount = targetDevices.Count,
                userCount = uniqueUsers
            });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = $"Error al enviar notificación: {ex.Message}" });
        }
    }

    [HttpPost("admin/settings/theme")]
    public IActionResult SaveTheme([FromBody] SaveThemeRequest request)
    {
        if (!SecurityHelper.IsAdministrator(User))
        {
            return Json(new { success = false, message = "No autorizado" });
        }

        if (request == null || string.IsNullOrWhiteSpace(request.Theme))
        {
            return Json(new { success = false, message = "Tema no válido" });
        }

        // Validar que el tema sea uno de los permitidos
        var temasPermitidos = new[] { "business", "corporate", "night", "luxury" };
        if (!temasPermitidos.Contains(request.Theme.ToLower()))
        {
            return Json(new { success = false, message = "Tema no válido" });
        }

        try
        {
            // Guardar en sesión (opcional, ya que se guarda en localStorage)
            HttpContext.Session.SetString("Tema", request.Theme);
            
            return Json(new { success = true, message = "Tema guardado exitosamente" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = $"Error al guardar tema: {ex.Message}" });
        }
    }

    #region API para Gráficos

    [HttpGet("admin/api/charts/income")]
    public async Task<IActionResult> GetIncomeChartData()
    {
        if (!SecurityHelper.IsAdministrator(User))
        {
            return Json(new { labels = new string[0], values = new decimal[0] });
        }

        try
        {
            var labels = new List<string>();
            var values = new List<decimal>();

            for (int i = 6; i >= 0; i--)
            {
                var date = DateTime.UtcNow.AddDays(-i);
                var startDate = date.Date;
                var endDate = startDate.AddDays(1).AddTicks(-1);

                var income = await _financeService.GetIncomeAsync(0, startDate, endDate, 1, 1000);
                var total = income.Items.Sum(t => t.Amount);

                labels.Add(date.ToString("dd/MM"));
                values.Add(total);
            }

            return Json(new { labels, values });
        }
        catch
        {
            return Json(new { labels = new string[0], values = new decimal[0] });
        }
    }

    [HttpGet("admin/api/charts/appointments-status")]
    public async Task<IActionResult> GetAppointmentsStatusChartData()
    {
        if (!SecurityHelper.IsAdministrator(User))
        {
            return Json(new { labels = new string[0], values = new int[0] });
        }

        try
        {
            var dashboard = await _dashboardService.GetAdminDashboardAsync();
            return Json(new
            {
                labels = new[] { "Pendientes", "Confirmadas", "Completadas", "Canceladas" },
                values = new[]
                {
                    dashboard.PendingAppointments,
                    dashboard.ConfirmedAppointments,
                    dashboard.TotalAppointments - dashboard.PendingAppointments - dashboard.ConfirmedAppointments - dashboard.CancelledAppointments,
                    dashboard.CancelledAppointments
                }
            });
        }
        catch
        {
            return Json(new { labels = new string[0], values = new int[0] });
        }
    }

    [HttpGet("admin/api/charts/top-barbers")]
    public async Task<IActionResult> GetTopBarbersChartData()
    {
        if (!SecurityHelper.IsAdministrator(User))
        {
            return Json(new { labels = new string[0], values = new decimal[0] });
        }

        try
        {
            var dashboard = await _dashboardService.GetAdminDashboardAsync();
            var topBarbers = dashboard.RecentBarbers
                .OrderByDescending(b => b.TotalRevenue)
                .Take(5)
                .ToList();

            return Json(new
            {
                labels = topBarbers.Select(b => b.Name).ToArray(),
                values = topBarbers.Select(b => b.TotalRevenue).ToArray()
            });
        }
        catch
        {
            return Json(new { labels = new string[0], values = new decimal[0] });
        }
    }

    [HttpGet("admin/api/charts/appointments-by-day")]
    public async Task<IActionResult> GetAppointmentsByDayChartData()
    {
        if (!SecurityHelper.IsAdministrator(User))
        {
            return Json(new { labels = new string[0], values = new int[0] });
        }

        try
        {
            var labels = new List<string>();
            var values = new List<int>();

            // Obtener todos los barberos
            var barbers = await _barberService.GetAllBarbersAsync();

            for (int i = 6; i >= 0; i--)
            {
                var date = DateTime.UtcNow.AddDays(-i);
                var dateOnly = DateOnly.FromDateTime(date);

                int totalCount = 0;
                foreach (var barber in barbers)
                {
                    var appointments = await _appointmentService.GetBarberAppointmentsAsync(barber.Id, dateOnly, null);
                    totalCount += appointments.Count;
                }

                labels.Add(date.ToString("dd/MM"));
                values.Add(totalCount);
            }

            return Json(new { labels, values });
        }
        catch
        {
            return Json(new { labels = new string[0], values = new int[0] });
        }
    }

    #endregion
}

public class UpdateBarberStatusRequest
{
    public bool IsActive { get; set; }
}

public class SaveThemeRequest
{
    public string Theme { get; set; } = string.Empty;
}
