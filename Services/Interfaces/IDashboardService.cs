using BarberPro.Models.DTOs.Responses;

namespace BarberPro.Services.Interfaces;

/// <summary>
/// Interfaz para el servicio de dashboards
/// </summary>
public interface IDashboardService
{
    Task<BarberDashboardDto> GetBarberDashboardAsync(int barberId);
    Task<AdminDashboardDto> GetAdminDashboardAsync();
}

