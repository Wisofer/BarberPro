namespace BarberPro.Models.DTOs.Responses;

/// <summary>
/// DTO de resumen financiero
/// </summary>
public class FinanceSummaryDto
{
    public decimal TotalIncome { get; set; }
    public decimal TotalExpenses { get; set; }
    public decimal NetProfit { get; set; }
    public decimal IncomeThisMonth { get; set; }
    public decimal ExpensesThisMonth { get; set; }
    public decimal ProfitThisMonth { get; set; }
}

/// <summary>
/// DTO de transacción
/// </summary>
public class TransactionDto
{
    public int Id { get; set; }
    public string Type { get; set; } = string.Empty; // Income o Expense
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? Category { get; set; }
    public DateTime Date { get; set; }
    public int? AppointmentId { get; set; }
}

/// <summary>
/// DTO de respuesta de transacciones
/// </summary>
public class TransactionsResponse
{
    public decimal Total { get; set; }
    public List<TransactionDto> Items { get; set; } = new();
}

