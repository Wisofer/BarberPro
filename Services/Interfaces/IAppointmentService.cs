using BarberPro.Models.DTOs.Requests;
using BarberPro.Models.DTOs.Responses;
using BarberPro.Models.Entities;

namespace BarberPro.Services.Interfaces;

/// <summary>
/// Interfaz para el servicio de citas
/// </summary>
public interface IAppointmentService
{
    Task<AppointmentDto> CreateAppointmentAsync(CreateAppointmentRequest request);
    Task<List<AppointmentDto>> GetBarberAppointmentsAsync(int barberId, DateOnly? date = null, AppointmentStatus? status = null);
    Task<AppointmentDto?> GetAppointmentByIdAsync(int id);
    Task<AppointmentDto> UpdateAppointmentAsync(int id, UpdateAppointmentRequest request);
    Task<bool> DeleteAppointmentAsync(int id);
    Task<bool> ValidateAppointmentAvailabilityAsync(int barberId, DateOnly date, TimeOnly time, int durationMinutes, int? excludeAppointmentId = null);
}

