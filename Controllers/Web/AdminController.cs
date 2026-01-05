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

    public AdminController(IDashboardService dashboardService, IBarberService barberService)
    {
        _dashboardService = dashboardService;
        _barberService = barberService;
    }

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

    [HttpPost]
    public async Task<IActionResult> CreateBarber(CreateBarberRequest request)
    {
        if (!SecurityHelper.IsAdministrator(User))
        {
            return Json(new { success = false, message = "No autorizado" });
        }

        if (!ModelState.IsValid)
        {
            return Json(new { success = false, message = "Datos inválidos" });
        }

        try
        {
            var barber = await _barberService.CreateBarberAsync(request);
            return Json(new { success = true, message = "Barbero creado exitosamente" });
        }
        catch (InvalidOperationException ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "Error al crear el barbero" });
        }
    }
}

