using BarberPro.Data;
using BarberPro.Models.DTOs.Requests;
using BarberPro.Models.DTOs.Responses;
using BarberPro.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BarberPro.Services.Implementations;

/// <summary>
/// Servicio para gestión de servicios del barbero
/// </summary>
public class ServiceService : IServiceService
{
    private readonly ApplicationDbContext _context;

    public ServiceService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<ServiceDto>> GetBarberServicesAsync(int barberId)
    {
        return await _context.Services
            .Where(s => s.BarberId == barberId && s.IsActive)
            .Select(s => new ServiceDto
            {
                Id = s.Id,
                Name = s.Name,
                Price = s.Price,
                DurationMinutes = s.DurationMinutes,
                IsActive = s.IsActive
            })
            .ToListAsync();
    }

    public async Task<ServiceDto> CreateServiceAsync(int barberId, CreateServiceRequest request)
    {
        var service = new Models.Entities.Service
        {
            BarberId = barberId,
            Name = request.Name,
            Price = request.Price,
            DurationMinutes = request.DurationMinutes,
            IsActive = true
        };

        _context.Services.Add(service);
        await _context.SaveChangesAsync();

        return new ServiceDto
        {
            Id = service.Id,
            Name = service.Name,
            Price = service.Price,
            DurationMinutes = service.DurationMinutes,
            IsActive = service.IsActive
        };
    }

    public async Task<ServiceDto?> GetServiceByIdAsync(int id)
    {
        return await _context.Services
            .Where(s => s.Id == id)
            .Select(s => new ServiceDto
            {
                Id = s.Id,
                Name = s.Name,
                Price = s.Price,
                DurationMinutes = s.DurationMinutes,
                IsActive = s.IsActive
            })
            .FirstOrDefaultAsync();
    }

    public async Task<bool> UpdateServiceAsync(int id, CreateServiceRequest request)
    {
        var service = await _context.Services.FindAsync(id);
        if (service == null)
            return false;

        service.Name = request.Name;
        service.Price = request.Price;
        service.DurationMinutes = request.DurationMinutes;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteServiceAsync(int id)
    {
        var service = await _context.Services.FindAsync(id);
        if (service == null)
            return false;

        // Soft delete
        service.IsActive = false;
        await _context.SaveChangesAsync();

        return true;
    }
}

