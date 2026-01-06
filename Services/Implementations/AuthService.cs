using BarberPro.Data;
using BarberPro.Models.DTOs.Requests;
using BarberPro.Models.DTOs.Responses;
using BarberPro.Models.Entities;
using BarberPro.Services.Interfaces;
using BarberPro.Utils;
using Microsoft.EntityFrameworkCore;

namespace BarberPro.Services.Implementations;

/// <summary>
/// Servicio de autenticación con JWT
/// </summary>
public class AuthService : IAuthService
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;

    public AuthService(ApplicationDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    public async Task<LoginResponse?> LoginAsync(LoginRequest request)
    {
        var user = await GetUserByEmailAsync(request.Email);
        if (user == null || !user.IsActive)
            return null;

        if (!PasswordHelper.VerifyPassword(request.Password, user.PasswordHash))
            return null;

        // Cargar barbero si existe
        if (user.Role == UserRole.Barber)
        {
            await _context.Entry(user)
                .Reference(u => u.Barber)
                .LoadAsync();
        }

        // Cargar trabajador si existe
        if (user.Role == UserRole.Employee)
        {
            await _context.Entry(user)
                .Reference(u => u.Employee)
                .LoadAsync();
            
            // Cargar también el barbero dueño
            if (user.Employee != null)
            {
                await _context.Entry(user.Employee)
                    .Reference(e => e.OwnerBarber)
                    .LoadAsync();
            }
        }

        // Generar token JWT
        var secretKey = _configuration["JwtSettings:SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey no configurado");
        var issuer = _configuration["JwtSettings:Issuer"] ?? "BarberPro";
        var audience = _configuration["JwtSettings:Audience"] ?? "BarberProUsers";
        var expirationMinutes = int.Parse(_configuration["JwtSettings:ExpirationInMinutes"] ?? "60");

        var token = JwtHelper.GenerateToken(user, secretKey, issuer, audience, expirationMinutes);

        return new LoginResponse
        {
            Token = token,
            User = new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                Role = user.Role.ToString(),
                Barber = user.Barber != null ? new BarberDto
                {
                    Id = user.Barber.Id,
                    Name = user.Barber.Name,
                    BusinessName = user.Barber.BusinessName,
                    Phone = user.Barber.Phone,
                    Slug = user.Barber.Slug,
                    IsActive = user.Barber.IsActive,
                    QrUrl = QrHelper.GenerateBarberUrl(user.Barber.Slug, _configuration),
                    CreatedAt = user.Barber.CreatedAt
                } : null
            },
            Role = user.Role.ToString()
        };
    }

    public async Task<User?> GetUserByIdAsync(int userId)
    {
        return await _context.Users
            .Include(u => u.Barber)
            .FirstOrDefaultAsync(u => u.Id == userId);
    }

    public async Task<User?> GetUserByEmailAsync(string email)
    {
        return await _context.Users
            .Include(u => u.Barber)
            .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());
    }

    public async Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword)
    {
        var user = await GetUserByIdAsync(userId);
        if (user == null)
            return false;

        // Verificar contraseña actual
        if (!PasswordHelper.VerifyPassword(currentPassword, user.PasswordHash))
            return false;

        // Actualizar contraseña
        user.PasswordHash = PasswordHelper.HashPassword(newPassword);
        user.UpdatedAt = DateTime.UtcNow;
        
        await _context.SaveChangesAsync();
        return true;
    }
}

