using BarberNic.Models.DTOs.Requests;
using BarberNic.Models.DTOs.Responses;

namespace BarberNic.Services.Interfaces;

/// <summary>
/// Interfaz para el servicio de finanzas
/// </summary>
public interface IFinanceService
{
    Task<FinanceSummaryDto> GetFinanceSummaryAsync(int barberId, DateTime? startDate = null, DateTime? endDate = null);
    Task<TransactionsResponse> GetIncomeAsync(int barberId, DateTime? startDate = null, DateTime? endDate = null, int page = 1, int pageSize = 50);
    Task<TransactionsResponse> GetExpensesAsync(int barberId, DateTime? startDate = null, DateTime? endDate = null, int page = 1, int pageSize = 50);
    Task<TransactionDto> CreateExpenseAsync(int barberId, CreateExpenseRequest request, int? employeeId = null);
    Task<TransactionDto> UpdateExpenseAsync(int barberId, int expenseId, UpdateExpenseRequest request);
    Task<bool> DeleteExpenseAsync(int barberId, int expenseId);
    Task<TransactionDto> CreateIncomeAsync(int barberId, CreateIncomeRequest request, int? employeeId = null);
    Task CreateIncomeFromAppointmentAsync(int barberId, int appointmentId, decimal amount, string description);
    Task CreateMultipleIncomesFromAppointmentAsync(int barberId, int appointmentId, List<(int ServiceId, string ServiceName, decimal Price)> services, string clientName);
    Task<List<string>> GetCategoriesAsync();
}

