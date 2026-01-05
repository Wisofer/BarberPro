using BarberPro.Data;
using BarberPro.Models.DTOs.Responses;
using BarberPro.Models.Entities;
using BarberPro.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BarberPro.Services.Implementations;

/// <summary>
/// Servicio para dashboards
/// </summary>
public class DashboardService : IDashboardService
{
    private readonly ApplicationDbContext _context;
    private readonly IBarberService _barberService;
    private readonly IFinanceService _financeService;

    public DashboardService(ApplicationDbContext context, IBarberService barberService, IFinanceService financeService)
    {
        _context = context;
        _barberService = barberService;
        _financeService = financeService;
    }

    public async Task<BarberDashboardDto> GetBarberDashboardAsync(int barberId)
    {
        var barber = await _barberService.GetBarberProfileAsync(barberId);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var startOfWeek = today.AddDays(-(int)today.DayOfWeek);
        var endOfWeek = startOfWeek.AddDays(6);
        var startOfMonth = new DateOnly(today.Year, today.Month, 1);
        var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

        // Estadísticas del día
        var todayAppointments = await _context.Appointments
            .Where(a => a.BarberId == barberId && a.Date == today)
            .ToListAsync();

        var todayStats = new TodayStatsDto
        {
            Appointments = todayAppointments.Count,
            Completed = todayAppointments.Count(a => a.Status == AppointmentStatus.Confirmed),
            Pending = todayAppointments.Count(a => a.Status == AppointmentStatus.Pending),
            Income = todayAppointments
                .Where(a => a.Status == AppointmentStatus.Confirmed)
                .Sum(a => a.Service.Price)
        };

        // Estadísticas de la semana
        var weekAppointments = await _context.Appointments
            .Include(a => a.Service)
            .Where(a => a.BarberId == barberId && 
                       a.Date >= startOfWeek && 
                       a.Date <= endOfWeek &&
                       a.Status == AppointmentStatus.Confirmed)
            .ToListAsync();

        var weekFinance = await _financeService.GetFinanceSummaryAsync(barberId, 
            startOfWeek.ToDateTime(TimeOnly.MinValue), 
            endOfWeek.ToDateTime(TimeOnly.MaxValue));

        var weekStats = new PeriodStatsDto
        {
            Appointments = weekAppointments.Count,
            Income = weekAppointments.Sum(a => a.Service.Price),
            Expenses = weekFinance.ExpensesThisMonth,
            Profit = weekAppointments.Sum(a => a.Service.Price) - weekFinance.ExpensesThisMonth
        };

        // Estadísticas del mes
        var monthAppointments = await _context.Appointments
            .Include(a => a.Service)
            .Where(a => a.BarberId == barberId && 
                       a.Date >= startOfMonth && 
                       a.Date <= endOfMonth &&
                       a.Status == AppointmentStatus.Confirmed)
            .ToListAsync();

        var monthFinance = await _financeService.GetFinanceSummaryAsync(barberId);

        var monthStats = new PeriodStatsDto
        {
            Appointments = monthAppointments.Count,
            Income = monthFinance.IncomeThisMonth,
            Expenses = monthFinance.ExpensesThisMonth,
            Profit = monthFinance.ProfitThisMonth
        };

        // Citas recientes y próximas
        var recentAppointments = await _context.Appointments
            .Include(a => a.Service)
            .Include(a => a.Barber)
            .Where(a => a.BarberId == barberId && a.Date < today)
            .OrderByDescending(a => a.Date)
            .ThenByDescending(a => a.Time)
            .Take(5)
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

        var upcomingAppointments = await _context.Appointments
            .Include(a => a.Service)
            .Include(a => a.Barber)
            .Where(a => a.BarberId == barberId && a.Date >= today)
            .OrderBy(a => a.Date)
            .ThenBy(a => a.Time)
            .Take(5)
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

        return new BarberDashboardDto
        {
            Barber = barber,
            Today = todayStats,
            ThisWeek = weekStats,
            ThisMonth = monthStats,
            RecentAppointments = recentAppointments,
            UpcomingAppointments = upcomingAppointments
        };
    }

    public async Task<AdminDashboardDto> GetAdminDashboardAsync()
    {
        var totalBarbers = await _context.Barbers.CountAsync();
        var activeBarbers = await _context.Barbers.CountAsync(b => b.IsActive);
        var inactiveBarbers = totalBarbers - activeBarbers;

        var totalAppointments = await _context.Appointments.CountAsync();
        var pendingAppointments = await _context.Appointments.CountAsync(a => a.Status == AppointmentStatus.Pending);
        var confirmedAppointments = await _context.Appointments.CountAsync(a => a.Status == AppointmentStatus.Confirmed);
        var cancelledAppointments = await _context.Appointments.CountAsync(a => a.Status == AppointmentStatus.Cancelled);

        var totalRevenue = await _context.Transactions
            .Where(t => t.Type == TransactionType.Income)
            .SumAsync(t => (decimal?)t.Amount) ?? 0;

        // Obtener barberos con sus datos
        var barbers = await _context.Barbers
            .OrderByDescending(b => b.CreatedAt)
            .Take(10)
            .ToListAsync();

        var recentBarbers = barbers.Select(b => new BarberSummaryDto
        {
            Id = b.Id,
            Name = b.Name ?? "Sin nombre",
            BusinessName = b.BusinessName ?? "",
            Phone = b.Phone ?? "",
            Slug = b.Slug ?? "",
            IsActive = b.IsActive,
            CreatedAt = b.CreatedAt,
            TotalAppointments = _context.Appointments.Count(a => a.BarberId == b.Id),
            TotalRevenue = _context.Transactions
                .Where(t => t.BarberId == b.Id && t.Type == TransactionType.Income)
                .Sum(t => (decimal?)t.Amount) ?? 0
        }).ToList();

        return new AdminDashboardDto
        {
            TotalBarbers = totalBarbers,
            ActiveBarbers = activeBarbers,
            InactiveBarbers = inactiveBarbers,
            TotalAppointments = totalAppointments,
            PendingAppointments = pendingAppointments,
            ConfirmedAppointments = confirmedAppointments,
            CancelledAppointments = cancelledAppointments,
            TotalRevenue = totalRevenue,
            RecentBarbers = recentBarbers
        };
    }
}

