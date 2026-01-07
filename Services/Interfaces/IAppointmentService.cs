using BarberNic.Models.DTOs.Requests;
using BarberNic.Models.DTOs.Responses;
using BarberNic.Models.Entities;

namespace BarberNic.Services.Interfaces;

/// <summary>
/// Interfaz para el servicio de citas
/// </summary>
public interface IAppointmentService
{
    Task<AppointmentDto> CreateAppointmentAsync(CreateAppointmentRequest request);
    Task<AppointmentDto> CreateAppointmentForBarberAsync(int barberId, CreateAppointmentRequest request, int? employeeId = null); // Para barbero autenticado o trabajador
    Task<List<AppointmentDto>> GetBarberAppointmentsAsync(int barberId, DateOnly? date = null, AppointmentStatus? status = null);
    Task<AppointmentDto?> GetAppointmentByIdAsync(int id);
    Task<AppointmentDto> UpdateAppointmentAsync(int id, UpdateAppointmentRequest request);
    Task<AppointmentDto> UpdateAppointmentForBarberAsync(int barberId, int appointmentId, UpdateAppointmentRequest request, int? employeeId = null); // Con validación de seguridad, employeeId opcional para asignar trabajador
    Task<bool> DeleteAppointmentAsync(int id);
    Task<bool> DeleteAppointmentForBarberAsync(int barberId, int appointmentId); // Con validación de seguridad
    Task<bool> ValidateAppointmentAvailabilityAsync(int barberId, DateOnly date, TimeOnly time, int durationMinutes, int? excludeAppointmentId = null);
}

