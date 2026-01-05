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
        // Validar que el barbero existe
        var barber = await _barberService.GetBarberBySlugAsync(request.BarberSlug);
        if (barber == null)
            throw new KeyNotFoundException("Barbero no encontrado");

        // Validar que el servicio existe y pertenece al barbero
        var service = await _context.Services
            .FirstOrDefaultAsync(s => s.Id == request.ServiceId && s.BarberId == barber.Id && s.IsActive);
        if (service == null)
            throw new KeyNotFoundException("Servicio no encontrado");

        // Validar disponibilidad
        var isAvailable = await ValidateAppointmentAvailabilityAsync(
            barber.Id, request.Date, request.Time, service.DurationMinutes);
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
                ServiceName = a.Service.Name,
                ServicePrice = a.Service.Price,
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
                ServiceName = a.Service.Name,
                ServicePrice = a.Service.Price,
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

        // Si cambia el estado a Confirmed, crear ingreso automáticamente
        if (request.Status.HasValue && request.Status.Value == AppointmentStatus.Confirmed && 
            appointment.Status != AppointmentStatus.Confirmed)
        {
            // Crear ingreso automático
            await _financeService.CreateIncomeFromAppointmentAsync(
                appointment.BarberId,
                appointment.Id,
                appointment.Service.Price,
                $"Cita - {appointment.Service.Name} - {appointment.ClientName}");
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

    public async Task<bool> DeleteAppointmentAsync(int id)
    {
        var appointment = await _context.Appointments.FindAsync(id);
        if (appointment == null)
            return false;

        _context.Appointments.Remove(appointment);
        await _context.SaveChangesAsync();

        return true;
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
        var hasConflict = await _context.Appointments
            .AnyAsync(a => a.BarberId == barberId &&
                          a.Date == date &&
                          a.Id != excludeAppointmentId &&
                          a.Status != AppointmentStatus.Cancelled &&
                          ((a.Time <= time && a.Time.AddMinutes(
                              _context.Services.First(s => s.Id == a.ServiceId).DurationMinutes) > time) ||
                           (time <= a.Time && endTime > a.Time)));

        return !hasConflict;
    }
}

