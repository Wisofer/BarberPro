using BarberNic.Models.DTOs.Requests;
using BarberNic.Models.DTOs.Responses;

namespace BarberNic.Services.Interfaces;

/// <summary>
/// Interfaz para el servicio de servicios del barbero
/// </summary>
public interface IServiceService
{
    Task<List<ServiceDto>> GetBarberServicesAsync(int barberId);
    Task<ServiceDto> CreateServiceAsync(int barberId, CreateServiceRequest request);
    Task<ServiceDto?> GetServiceByIdAsync(int id);
    Task<bool> UpdateServiceAsync(int barberId, int id, CreateServiceRequest request);
    Task<bool> DeleteServiceAsync(int barberId, int id);
}

