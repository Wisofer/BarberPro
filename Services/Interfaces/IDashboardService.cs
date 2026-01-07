using BarberNic.Models.DTOs.Responses;

namespace BarberNic.Services.Interfaces;

/// <summary>
/// Interfaz para el servicio de dashboards
/// </summary>
public interface IDashboardService
{
    Task<BarberDashboardDto> GetBarberDashboardAsync(int barberId);
    Task<AdminDashboardDto> GetAdminDashboardAsync();
}

