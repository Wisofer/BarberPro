using BarberPro.Data;
using BarberPro.Models.DTOs.Requests;
using BarberPro.Models.DTOs.Responses;
using BarberPro.Models.Entities;
using BarberPro.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BarberPro.Services.Implementations;

/// <summary>
/// Servicio para gestión de citas
/// </summary>
public class AppointmentService : IAppointmentService
{
    private readonly ApplicationDbContext _context;
    private readonly IBarberService _barberService;
    private readonly IAvailabilityService _availabilityService;
    private readonly IFinanceService _financeService;

    public AppointmentService(
        ApplicationDbContext context,
        IBarberService barberService,
        IAvailabilityService availabilityService,
        IFinanceService financeService)
    {
        _context = context;
        _barberService = barberService;
        _availabilityService = availabilityService;
        _financeService = financeService;
    }

    public async Task<AppointmentDto> CreateAppointmentAsync(CreateAppointmentRequest request)
    {
        // Validar que barberSlug esté presente (para creación pública)
        if (string.IsNullOrEmpty(request.BarberSlug))
            throw new InvalidOperationException("El slug del barbero es requerido para creación pública");

        // Validar que el barbero existe
        var barber = await _barberService.GetBarberBySlugAsync(request.BarberSlug);
        if (barber == null)
            throw new KeyNotFoundException("Barbero no encontrado");

        // Validar servicio si se proporciona
        Service? service = null;
        int durationMinutes = 30; // Duración por defecto si no hay servicio (30 minutos)
        
        if (request.ServiceId.HasValue)
        {
            service = await _context.Services
                .FirstOrDefaultAsync(s => s.Id == request.ServiceId.Value && s.BarberId == barber.Id && s.IsActive);
            if (service == null)
                throw new KeyNotFoundException("Servicio no encontrado");
            durationMinutes = service.DurationMinutes;
        }

        // Validar disponibilidad (usar duración del servicio o 30 min por defecto)
        var isAvailable = await ValidateAppointmentAvailabilityAsync(
            barber.Id, request.Date, request.Time, durationMinutes);
        if (!isAvailable)
            throw new InvalidOperationException("El horario no está disponible");

        // Validar que no sea en el pasado
        var appointmentDateTime = request.Date.ToDateTime(request.Time);
        if (appointmentDateTime < DateTime.Now)
            throw new InvalidOperationException("No se pueden crear citas en el pasado");

        // Crear la cita
        var appointment = new Appointment
        {
            BarberId = barber.Id,
            ServiceId = request.ServiceId, // Puede ser null
            ClientName = request.ClientName,
            ClientPhone = request.ClientPhone,
            Date = request.Date,
            Time = request.Time,
            Status = AppointmentStatus.Pending
        };

        _context.Appointments.Add(appointment);
        await _context.SaveChangesAsync();

        return await GetAppointmentByIdAsync(appointment.Id) ?? throw new Exception("Error al crear la cita");
    }

    public async Task<List<AppointmentDto>> GetBarberAppointmentsAsync(int barberId, DateOnly? date = null, AppointmentStatus? status = null)
    {
        var query = _context.Appointments
            .Include(a => a.Service)
            .Include(a => a.Barber)
            .Where(a => a.BarberId == barberId);

        if (date.HasValue)
            query = query.Where(a => a.Date == date.Value);

        if (status.HasValue)
            query = query.Where(a => a.Status == status.Value);

        return await query
            .OrderBy(a => a.Date)
            .ThenBy(a => a.Time)
            .Select(a => new AppointmentDto
            {
                Id = a.Id,
                BarberId = a.BarberId,
                BarberName = a.Barber.Name,
                ServiceId = a.ServiceId,
                ServiceName = a.Service != null ? a.Service.Name : null,
                ServicePrice = a.Service != null ? a.Service.Price : null,
                ClientName = a.ClientName,
                ClientPhone = a.ClientPhone,
                Date = a.Date,
                Time = a.Time,
                Status = a.Status.ToString(),
                CreatedAt = a.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<AppointmentDto?> GetAppointmentByIdAsync(int id)
    {
        return await _context.Appointments
            .Include(a => a.Service)
            .Include(a => a.Barber)
            .Where(a => a.Id == id)
            .Select(a => new AppointmentDto
            {
                Id = a.Id,
                BarberId = a.BarberId,
                BarberName = a.Barber.Name,
                ServiceId = a.ServiceId,
                ServiceName = a.Service != null ? a.Service.Name : null,
                ServicePrice = a.Service != null ? a.Service.Price : null,
                ClientName = a.ClientName,
                ClientPhone = a.ClientPhone,
                Date = a.Date,
                Time = a.Time,
                Status = a.Status.ToString(),
                CreatedAt = a.CreatedAt
            })
            .FirstOrDefaultAsync();
    }

    public async Task<AppointmentDto> UpdateAppointmentAsync(int id, UpdateAppointmentRequest request)
    {
        var appointment = await _context.Appointments
            .Include(a => a.Service)
            .FirstOrDefaultAsync(a => a.Id == id);
        if (appointment == null)
            throw new KeyNotFoundException("Cita no encontrada");

        // Actualizar servicio si se proporciona
        Service? service = null;
        if (request.ServiceId.HasValue)
        {
            service = await _context.Services
                .FirstOrDefaultAsync(s => s.Id == request.ServiceId.Value && s.BarberId == appointment.BarberId && s.IsActive);
            if (service == null)
                throw new KeyNotFoundException("Servicio no encontrado");
            appointment.ServiceId = request.ServiceId.Value;
        }

        // Obtener el servicio actualizado (el que se asignó o el que ya tenía)
        service = service ?? appointment.Service;

        // Si cambia el estado a Confirmed o Completed, crear ingreso automáticamente (solo si hay servicio y no se ha creado ya)
        if (request.Status.HasValue && 
            (request.Status.Value == AppointmentStatus.Confirmed || request.Status.Value == AppointmentStatus.Completed) && 
            appointment.Status != AppointmentStatus.Confirmed && 
            appointment.Status != AppointmentStatus.Completed && 
            service != null)
        {
            // Crear ingreso automático solo si hay servicio con precio
            await _financeService.CreateIncomeFromAppointmentAsync(
                appointment.BarberId,
                appointment.Id,
                service.Price,
                $"Cita - {service.Name} - {appointment.ClientName}");
        }

        // Actualizar campos
        if (request.Status.HasValue)
            appointment.Status = (AppointmentStatus)request.Status.Value;

        if (request.Date.HasValue)
            appointment.Date = request.Date.Value;

        if (request.Time.HasValue)
            appointment.Time = request.Time.Value;

        appointment.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return await GetAppointmentByIdAsync(id) ?? throw new Exception("Error al actualizar la cita");
    }

    public async Task<AppointmentDto> UpdateAppointmentForBarberAsync(int barberId, int appointmentId, UpdateAppointmentRequest request)
    {
        var appointment = await _context.Appointments
            .Include(a => a.Service)
            .FirstOrDefaultAsync(a => a.Id == appointmentId && a.BarberId == barberId);
        
        if (appointment == null)
            throw new KeyNotFoundException("Cita no encontrada o no pertenece al barbero");

        // Actualizar servicio si se proporciona
        Service? service = null;
        if (request.ServiceId.HasValue)
        {
            service = await _context.Services
                .FirstOrDefaultAsync(s => s.Id == request.ServiceId.Value && s.BarberId == barberId && s.IsActive);
            if (service == null)
                throw new KeyNotFoundException("Servicio no encontrado");
            appointment.ServiceId = request.ServiceId.Value;
        }

        // Obtener el servicio actualizado (el que se asignó o el que ya tenía)
        service = service ?? appointment.Service;

        // Si cambia el estado a Confirmed o Completed, crear ingreso automáticamente (solo si hay servicio y no se ha creado ya)
        if (request.Status.HasValue && 
            (request.Status.Value == AppointmentStatus.Confirmed || request.Status.Value == AppointmentStatus.Completed) && 
            appointment.Status != AppointmentStatus.Confirmed && 
            appointment.Status != AppointmentStatus.Completed && 
            service != null)
        {
            // Crear ingreso automático solo si hay servicio con precio
            await _financeService.CreateIncomeFromAppointmentAsync(
                appointment.BarberId,
                appointment.Id,
                service.Price,
                $"Cita - {service.Name} - {appointment.ClientName}");
        }

        // Actualizar campos
        if (request.Status.HasValue)
            appointment.Status = (AppointmentStatus)request.Status.Value;

        if (request.Date.HasValue)
            appointment.Date = request.Date.Value;

        if (request.Time.HasValue)
            appointment.Time = request.Time.Value;

        appointment.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return await GetAppointmentByIdAsync(appointmentId) ?? throw new Exception("Error al actualizar la cita");
    }

    public async Task<bool> DeleteAppointmentAsync(int id)
    {
        var appointment = await _context.Appointments.FindAsync(id);
        if (appointment == null)
            return false;

        _context.Appointments.Remove(appointment);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> DeleteAppointmentForBarberAsync(int barberId, int appointmentId)
    {
        var appointment = await _context.Appointments
            .FirstOrDefaultAsync(a => a.Id == appointmentId && a.BarberId == barberId);
        
        if (appointment == null)
            return false;

        _context.Appointments.Remove(appointment);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<AppointmentDto> CreateAppointmentForBarberAsync(int barberId, CreateAppointmentRequest request)
    {
        // Obtener el barbero para validar que existe
        var barber = await _barberService.GetBarberByIdAsync(barberId);
        if (barber == null)
            throw new KeyNotFoundException("Barbero no encontrado");

        // Validar servicio si se proporciona
        Service? service = null;
        int durationMinutes = 30; // Duración por defecto si no hay servicio (30 minutos)
        
        if (request.ServiceId.HasValue)
        {
            service = await _context.Services
                .FirstOrDefaultAsync(s => s.Id == request.ServiceId.Value && s.BarberId == barberId && s.IsActive);
            if (service == null)
                throw new KeyNotFoundException("Servicio no encontrado");
            durationMinutes = service.DurationMinutes;
        }

        // Validar disponibilidad (usar duración del servicio o 30 min por defecto)
        var isAvailable = await ValidateAppointmentAvailabilityAsync(
            barberId, request.Date, request.Time, durationMinutes);
        if (!isAvailable)
            throw new InvalidOperationException("El horario no está disponible");

        // Validar que no sea en el pasado
        var appointmentDateTime = request.Date.ToDateTime(request.Time);
        if (appointmentDateTime < DateTime.Now)
            throw new InvalidOperationException("No se pueden crear citas en el pasado");

        // Crear la cita
        var appointment = new Appointment
        {
            BarberId = barberId,
            ServiceId = request.ServiceId,
            ClientName = request.ClientName,
            ClientPhone = request.ClientPhone,
            Date = request.Date,
            Time = request.Time,
            Status = AppointmentStatus.Pending
        };

        _context.Appointments.Add(appointment);
        await _context.SaveChangesAsync();

        return await GetAppointmentByIdAsync(appointment.Id) ?? throw new Exception("Error al crear la cita");
    }

    public async Task<bool> ValidateAppointmentAvailabilityAsync(int barberId, DateOnly date, TimeOnly time, int durationMinutes, int? excludeAppointmentId = null)
    {
        // Verificar horarios laborales
        var dayOfWeek = date.DayOfWeek;
        var workingHours = await _context.WorkingHours
            .FirstOrDefaultAsync(wh => wh.BarberId == barberId && wh.DayOfWeek == dayOfWeek && wh.IsActive);

        if (workingHours == null)
            return false;

        // Verificar que el horario esté dentro del rango laboral
        var endTime = time.AddMinutes(durationMinutes);
        if (time < workingHours.StartTime || endTime > workingHours.EndTime)
            return false;

        // Verificar bloqueos temporales
        var isBlocked = await _context.BlockedTimes
            .AnyAsync(bt => bt.BarberId == barberId &&
                           bt.Date == date &&
                           ((time >= bt.StartTime && time < bt.EndTime) ||
                            (endTime > bt.StartTime && endTime <= bt.EndTime) ||
                            (time <= bt.StartTime && endTime >= bt.EndTime)));

        if (isBlocked)
            return false;

        // Verificar que no haya otra cita en el mismo horario
        var existingAppointments = await _context.Appointments
            .Include(a => a.Service)
            .Where(a => a.BarberId == barberId &&
                       a.Date == date &&
                       a.Id != excludeAppointmentId &&
                       a.Status != AppointmentStatus.Cancelled)
            .ToListAsync();

        var hasConflict = existingAppointments.Any(a =>
        {
            // Si la cita no tiene servicio, usar duración por defecto de 30 minutos
            var appointmentDuration = a.Service?.DurationMinutes ?? 30;
            var appointmentEndTime = a.Time.AddMinutes(appointmentDuration);
            return (a.Time <= time && appointmentEndTime > time) ||
                   (time <= a.Time && endTime > a.Time);
        });

        return !hasConflict;
    }
}

