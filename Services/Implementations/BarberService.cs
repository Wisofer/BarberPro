using BarberPro.Data;
using BarberPro.Models.DTOs.Requests;
using BarberPro.Models.DTOs.Responses;
using BarberPro.Models.Entities;
using BarberPro.Services.Interfaces;
using BarberPro.Utils;
using Microsoft.EntityFrameworkCore;

namespace BarberPro.Services.Implementations;

/// <summary>
/// Servicio para gestión de barberos
/// </summary>
public class BarberService : IBarberService
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;

    public BarberService(ApplicationDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    public async Task<Barber?> GetBarberBySlugAsync(string slug)
    {
        var barber = await _context.Barbers
            .Include(b => b.User)
            .FirstOrDefaultAsync(b => b.Slug == slug && b.IsActive);
        
        if (barber != null)
        {
            await _context.Entry(barber)
                .Collection(b => b.Services)
                .Query()
                .Where(s => s.IsActive)
                .LoadAsync();
            
            await _context.Entry(barber)
                .Collection(b => b.WorkingHours)
                .Query()
                .Where(wh => wh.IsActive)
                .LoadAsync();
        }
        
        return barber;
    }

    public async Task<Barber?> GetBarberByIdAsync(int id)
    {
        return await _context.Barbers
            .Include(b => b.User)
            .FirstOrDefaultAsync(b => b.Id == id);
    }

    public async Task<Barber?> GetBarberByUserIdAsync(int userId)
    {
        return await _context.Barbers
            .Include(b => b.User)
            .FirstOrDefaultAsync(b => b.UserId == userId);
    }

    public async Task<BarberPublicDto> GetPublicBarberInfoAsync(string slug)
    {
        var barber = await GetBarberBySlugAsync(slug);
        if (barber == null)
            throw new KeyNotFoundException("Barbero no encontrado");

        return new BarberPublicDto
        {
            Id = barber.Id,
            Name = barber.Name,
            BusinessName = barber.BusinessName,
            Phone = barber.Phone,
            Slug = barber.Slug,
            Services = barber.Services.Select(s => new ServiceDto
            {
                Id = s.Id,
                Name = s.Name,
                Price = s.Price,
                DurationMinutes = s.DurationMinutes,
                IsActive = s.IsActive
            }).ToList(),
            WorkingHours = barber.WorkingHours.Select(wh => new WorkingHoursDto
            {
                Id = wh.Id,
                DayOfWeek = wh.DayOfWeek,
                StartTime = wh.StartTime,
                EndTime = wh.EndTime,
                IsActive = wh.IsActive
            }).ToList()
        };
    }

    public async Task<BarberDto> GetBarberProfileAsync(int barberId)
    {
        var barber = await GetBarberByIdAsync(barberId);
        if (barber == null)
            throw new KeyNotFoundException("Barbero no encontrado");

        return new BarberDto
        {
            Id = barber.Id,
            Name = barber.Name,
            BusinessName = barber.BusinessName,
            Phone = barber.Phone,
            Slug = barber.Slug,
            IsActive = barber.IsActive,
            QrUrl = QrHelper.GenerateBarberUrl(barber.Slug),
            CreatedAt = barber.CreatedAt,
            Email = barber.User?.Email
        };
    }

    public async Task<BarberDto> UpdateBarberProfileAsync(int barberId, UpdateBarberProfileRequest request)
    {
        var barber = await GetBarberByIdAsync(barberId);
        if (barber == null)
            throw new KeyNotFoundException("Barbero no encontrado");

        barber.Name = request.Name;
        barber.BusinessName = request.BusinessName;
        barber.Phone = request.Phone;
        barber.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return await GetBarberProfileAsync(barberId);
    }

    public async Task<string> GetQrUrlAsync(int barberId)
    {
        var barber = await GetBarberByIdAsync(barberId);
        if (barber == null)
            throw new KeyNotFoundException("Barbero no encontrado");

        return QrHelper.GenerateBarberUrl(barber.Slug);
    }

    public async Task<List<BarberDto>> GetAllBarbersAsync(bool? isActive = null)
    {
        var query = _context.Barbers.AsQueryable();

        if (isActive.HasValue)
            query = query.Where(b => b.IsActive == isActive.Value);

        var barbers = await query
            .Select(b => new
            {
                b.Id,
                b.Name,
                b.BusinessName,
                b.Phone,
                b.Slug,
                b.IsActive,
                b.CreatedAt
            })
            .ToListAsync();

        return barbers.Select(b => new BarberDto
        {
            Id = b.Id,
            Name = b.Name,
            BusinessName = b.BusinessName,
            Phone = b.Phone,
            Slug = b.Slug,
            IsActive = b.IsActive,
            QrUrl = QrHelper.GenerateBarberUrl(b.Slug),
            CreatedAt = b.CreatedAt
        }).ToList();
    }

    public async Task<bool> UpdateBarberStatusAsync(int id, bool isActive)
    {
        var barber = await GetBarberByIdAsync(id);
        if (barber == null)
            return false;

        barber.IsActive = isActive;
        barber.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> DeleteBarberAsync(int id)
    {
        var barber = await GetBarberByIdAsync(id);
        if (barber == null)
            return false;

        _context.Barbers.Remove(barber);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<BarberDto> CreateBarberAsync(CreateBarberRequest request)
    {
        // Verificar si el email ya existe
        var existingUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Email.ToLower() == request.Email.ToLower());
        
        if (existingUser != null)
            throw new InvalidOperationException("El email ya está registrado");

        // Crear usuario
        var user = new User
        {
            Email = request.Email,
            PasswordHash = PasswordHelper.HashPassword(request.Password),
            Role = UserRole.Barber,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Generar slug único
        var baseSlug = SlugHelper.GenerateSlug(request.Name);
        var slug = baseSlug;
        int counter = 1;
        while (await _context.Barbers.AnyAsync(b => b.Slug == slug))
        {
            slug = $"{baseSlug}-{counter}";
            counter++;
        }

        // Crear perfil de barbero
        var barber = new Barber
        {
            UserId = user.Id,
            Name = request.Name,
            BusinessName = request.BusinessName,
            Phone = request.Phone,
            Slug = slug,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Barbers.Add(barber);
        await _context.SaveChangesAsync();

        // Crear horarios de trabajo por defecto (Lunes a Viernes, 9:00 - 17:00)
        for (int i = 1; i <= 5; i++) // Lunes a Viernes
        {
            _context.WorkingHours.Add(new WorkingHours
            {
                BarberId = barber.Id,
                DayOfWeek = (DayOfWeek)i,
                StartTime = new TimeOnly(9, 0),
                EndTime = new TimeOnly(17, 0),
                IsActive = true
            });
        }
        await _context.SaveChangesAsync();

        return await GetBarberProfileAsync(barber.Id);
    }
}

