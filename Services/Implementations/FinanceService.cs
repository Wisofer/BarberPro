using BarberPro.Data;
using BarberPro.Models.DTOs.Requests;
using BarberPro.Models.DTOs.Responses;
using BarberPro.Models.Entities;
using BarberPro.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace BarberPro.Services.Implementations;

/// <summary>
/// Servicio para gestión de finanzas
/// </summary>
public class FinanceService : IFinanceService
{
    private readonly ApplicationDbContext _context;

    public FinanceService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<FinanceSummaryDto> GetFinanceSummaryAsync(int barberId, DateTime? startDate = null, DateTime? endDate = null)
    {
        var now = DateTime.UtcNow;
        var startOfMonth = new DateTime(now.Year, now.Month, 1);
        var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

        var query = _context.Transactions.Where(t => t.BarberId == barberId);

        if (startDate.HasValue)
            query = query.Where(t => t.Date >= startDate.Value);
        if (endDate.HasValue)
            query = query.Where(t => t.Date <= endDate.Value);

        var allTransactions = await query.ToListAsync();
        var monthTransactions = await _context.Transactions
            .Where(t => t.BarberId == barberId && t.Date >= startOfMonth && t.Date <= endOfMonth)
            .ToListAsync();

        return new FinanceSummaryDto
        {
            TotalIncome = allTransactions.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount),
            TotalExpenses = allTransactions.Where(t => t.Type == TransactionType.Expense).Sum(t => t.Amount),
            NetProfit = allTransactions.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount) -
                       allTransactions.Where(t => t.Type == TransactionType.Expense).Sum(t => t.Amount),
            IncomeThisMonth = monthTransactions.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount),
            ExpensesThisMonth = monthTransactions.Where(t => t.Type == TransactionType.Expense).Sum(t => t.Amount),
            ProfitThisMonth = monthTransactions.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount) -
                             monthTransactions.Where(t => t.Type == TransactionType.Expense).Sum(t => t.Amount)
        };
    }

    public async Task<TransactionsResponse> GetIncomeAsync(int barberId, DateTime? startDate = null, DateTime? endDate = null, int page = 1, int pageSize = 50)
    {
        var query = _context.Transactions
            .Include(t => t.Employee)
            .Where(t => t.BarberId == barberId && t.Type == TransactionType.Income);

        if (startDate.HasValue)
            query = query.Where(t => t.Date >= startDate.Value);
        if (endDate.HasValue)
            query = query.Where(t => t.Date <= endDate.Value);

        var total = await query.SumAsync(t => t.Amount);
        var items = await query
            .OrderByDescending(t => t.Date)
            .ThenByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new TransactionDto
            {
                Id = t.Id,
                Type = t.Type.ToString(),
                Amount = t.Amount,
                Description = t.Description,
                Category = t.Category,
                Date = t.Date,
                AppointmentId = t.AppointmentId,
                EmployeeId = t.EmployeeId,
                EmployeeName = t.Employee != null ? t.Employee.Name : null
            })
            .ToListAsync();

        return new TransactionsResponse
        {
            Total = total,
            Items = items
        };
    }

    public async Task<TransactionsResponse> GetExpensesAsync(int barberId, DateTime? startDate = null, DateTime? endDate = null, int page = 1, int pageSize = 50)
    {
        var query = _context.Transactions
            .Include(t => t.Employee)
            .Where(t => t.BarberId == barberId && t.Type == TransactionType.Expense);

        if (startDate.HasValue)
            query = query.Where(t => t.Date >= startDate.Value);
        if (endDate.HasValue)
            query = query.Where(t => t.Date <= endDate.Value);

        var total = await query.SumAsync(t => t.Amount);
        var items = await query
            .OrderByDescending(t => t.Date)
            .ThenByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new TransactionDto
            {
                Id = t.Id,
                Type = t.Type.ToString(),
                Amount = t.Amount,
                Description = t.Description,
                Category = t.Category,
                Date = t.Date,
                AppointmentId = t.AppointmentId,
                EmployeeId = t.EmployeeId,
                EmployeeName = t.Employee != null ? t.Employee.Name : null
            })
            .ToListAsync();

        return new TransactionsResponse
        {
            Total = total,
            Items = items
        };
    }

    public async Task<TransactionDto> CreateExpenseAsync(int barberId, CreateExpenseRequest request, int? employeeId = null)
    {
        var transaction = new Transaction
        {
            BarberId = barberId,
            EmployeeId = employeeId,
            Type = TransactionType.Expense,
            Amount = request.Amount,
            Description = request.Description,
            Category = request.Category,
            Date = request.Date
        };

        _context.Transactions.Add(transaction);
        await _context.SaveChangesAsync();

        // Cargar Employee si existe
        if (employeeId.HasValue)
        {
            await _context.Entry(transaction)
                .Reference(t => t.Employee)
                .LoadAsync();
        }

        return new TransactionDto
        {
            Id = transaction.Id,
            Type = transaction.Type.ToString(),
            Amount = transaction.Amount,
            Description = transaction.Description,
            Category = transaction.Category,
            Date = transaction.Date,
            AppointmentId = transaction.AppointmentId,
            EmployeeId = transaction.EmployeeId,
            EmployeeName = transaction.Employee != null ? transaction.Employee.Name : null
        };
    }

    public async Task<TransactionDto> UpdateExpenseAsync(int barberId, int expenseId, UpdateExpenseRequest request)
    {
        var transaction = await _context.Transactions
            .FirstOrDefaultAsync(t => t.Id == expenseId && t.BarberId == barberId && t.Type == TransactionType.Expense);

        if (transaction == null)
            throw new KeyNotFoundException("Egreso no encontrado o no pertenece al barbero");

        transaction.Amount = request.Amount;
        transaction.Description = request.Description;
        transaction.Category = request.Category;
        transaction.Date = request.Date;

        _context.Transactions.Update(transaction);
        await _context.SaveChangesAsync();

        return new TransactionDto
        {
            Id = transaction.Id,
            Type = transaction.Type.ToString(),
            Amount = transaction.Amount,
            Description = transaction.Description,
            Category = transaction.Category,
            Date = transaction.Date,
            AppointmentId = transaction.AppointmentId
        };
    }

    public async Task<bool> DeleteExpenseAsync(int barberId, int expenseId)
    {
        var transaction = await _context.Transactions
            .FirstOrDefaultAsync(t => t.Id == expenseId && t.BarberId == barberId && t.Type == TransactionType.Expense);

        if (transaction == null)
            return false;

        _context.Transactions.Remove(transaction);
        await _context.SaveChangesAsync();
        return true;
    }

    public Task<List<string>> GetCategoriesAsync()
    {
        // Categorías predefinidas para ingresos y egresos
        var categories = new List<string>
        {
            "Alquiler",
            "Servicios Públicos",
            "Materiales",
            "Salarios",
            "Marketing",
            "Service",
            "Otros"
        };
        return Task.FromResult(categories);
    }

    public async Task<TransactionDto> CreateIncomeAsync(int barberId, CreateIncomeRequest request, int? employeeId = null)
    {
        var transaction = new Transaction
        {
            BarberId = barberId,
            EmployeeId = employeeId,
            Type = TransactionType.Income,
            Amount = request.Amount,
            Description = request.Description,
            Category = request.Category ?? "Service",
            Date = request.Date,
            AppointmentId = null // Ingreso manual, no viene de cita
        };

        _context.Transactions.Add(transaction);
        await _context.SaveChangesAsync();

        // Cargar Employee si existe
        if (employeeId.HasValue)
        {
            await _context.Entry(transaction)
                .Reference(t => t.Employee)
                .LoadAsync();
        }

        return new TransactionDto
        {
            Id = transaction.Id,
            Type = transaction.Type.ToString(),
            Amount = transaction.Amount,
            Description = transaction.Description,
            Category = transaction.Category,
            Date = transaction.Date,
            AppointmentId = transaction.AppointmentId,
            EmployeeId = transaction.EmployeeId,
            EmployeeName = transaction.Employee != null ? transaction.Employee.Name : null
        };
    }

    public async Task CreateIncomeFromAppointmentAsync(int barberId, int appointmentId, decimal amount, string description)
    {
        // Verificar que no exista ya una transacción para esta cita
        var exists = await _context.Transactions
            .AnyAsync(t => t.BarberId == barberId && t.AppointmentId == appointmentId);

        if (exists)
            return; // Ya existe, no crear duplicado

        var transaction = new Transaction
        {
            BarberId = barberId,
            Type = TransactionType.Income,
            Amount = amount,
            Description = description,
            Category = "Service",
            Date = DateTime.UtcNow,
            AppointmentId = appointmentId
        };

        _context.Transactions.Add(transaction);
        await _context.SaveChangesAsync();
    }

    public async Task CreateMultipleIncomesFromAppointmentAsync(int barberId, int appointmentId, List<(int ServiceId, string ServiceName, decimal Price)> services, string clientName)
    {
        if (services == null || services.Count == 0)
            return;

        // Verificar que no existan transacciones para esta cita con estos servicios
        var existingServiceIds = await _context.Transactions
            .Where(t => t.BarberId == barberId && t.AppointmentId == appointmentId)
            .Select(t => t.Description)
            .ToListAsync();

        // Crear una transacción por cada servicio
        foreach (var (serviceId, serviceName, price) in services)
        {
            var description = $"Cita - {serviceName} - {clientName}";
            
            // Verificar si ya existe una transacción con esta descripción para esta cita
            if (existingServiceIds.Contains(description))
                continue; // Ya existe, no crear duplicado

            var transaction = new Transaction
            {
                BarberId = barberId,
                Type = TransactionType.Income,
                Amount = price,
                Description = description,
                Category = "Service",
                Date = DateTime.UtcNow,
                AppointmentId = appointmentId
            };

            _context.Transactions.Add(transaction);
        }

        await _context.SaveChangesAsync();
    }
}

