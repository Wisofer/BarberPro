using BarberPro.Models.Entities;
using BarberPro.Services.Interfaces;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Text;
using System.Text.Json;

namespace BarberPro.Services.Implementations;

/// <summary>
/// Servicio para exportar datos
/// </summary>
public class ExportService : IExportService
{
    private readonly Data.ApplicationDbContext _context;
    private readonly ILogger<ExportService> _logger;

    public ExportService(
        Data.ApplicationDbContext context,
        ILogger<ExportService> logger)
    {
        _context = context;
        _logger = logger;
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public async Task<byte[]> ExportAppointmentsAsync(int barberId, DateOnly? startDate, DateOnly? endDate, string format)
    {
        var query = _context.Appointments
            .Include(a => a.Service)
            .Where(a => a.BarberId == barberId);

        if (startDate.HasValue)
            query = query.Where(a => a.Date >= startDate.Value);
        if (endDate.HasValue)
            query = query.Where(a => a.Date <= endDate.Value);

        var appointments = await query
            .OrderBy(a => a.Date)
            .ThenBy(a => a.Time)
            .ToListAsync();

        return format.ToLower() switch
        {
            "csv" => GenerateCsvAppointments(appointments),
            "excel" => await GenerateExcelAppointmentsAsync(appointments),
            "pdf" => await GeneratePdfAppointmentsAsync(appointments, startDate, endDate),
            _ => throw new ArgumentException($"Formato no soportado: {format}")
        };
    }

    public async Task<byte[]> ExportFinancesAsync(int barberId, DateOnly? startDate, DateOnly? endDate, string format)
    {
        var query = _context.Transactions
            .Where(t => t.BarberId == barberId);

        if (startDate.HasValue)
            query = query.Where(t => t.Date >= startDate.Value.ToDateTime(TimeOnly.MinValue));
        if (endDate.HasValue)
            query = query.Where(t => t.Date <= endDate.Value.ToDateTime(TimeOnly.MaxValue));

        var transactions = await query
            .OrderBy(t => t.Date)
            .ToListAsync();

        return format.ToLower() switch
        {
            "csv" => GenerateCsvFinances(transactions),
            "excel" => await GenerateExcelFinancesAsync(transactions),
            "pdf" => await GeneratePdfFinancesAsync(transactions, startDate, endDate),
            _ => throw new ArgumentException($"Formato no soportado: {format}")
        };
    }

    public async Task<byte[]> ExportClientsAsync(int barberId, string format)
    {
        var clients = await _context.Appointments
            .Include(a => a.Service)
            .Where(a => a.BarberId == barberId)
            .GroupBy(a => new { a.ClientName, a.ClientPhone })
            .Select(g => new ClientExportDto
            {
                ClientName = g.Key.ClientName,
                ClientPhone = g.Key.ClientPhone,
                TotalAppointments = g.Count(),
                LastAppointment = g.Max(a => a.Date),
                TotalSpent = g.Where(a => a.Status == AppointmentStatus.Confirmed)
                    .Sum(a => a.Service.Price)
            })
            .OrderByDescending(c => c.TotalAppointments)
            .ToListAsync();

        return format.ToLower() switch
        {
            "csv" => GenerateCsvClients(clients),
            "excel" => await GenerateExcelClientsAsync(clients),
            "pdf" => await GeneratePdfClientsAsync(clients),
            _ => throw new ArgumentException($"Formato no soportado: {format}")
        };
    }

    public async Task<byte[]> ExportBackupAsync(int barberId)
    {
        var barber = await _context.Barbers
            .Include(b => b.Services)
            .Include(b => b.WorkingHours)
            .FirstOrDefaultAsync(b => b.Id == barberId);

        if (barber == null)
            throw new KeyNotFoundException("Barbero no encontrado");

        var appointments = await _context.Appointments
            .Include(a => a.Service)
            .Where(a => a.BarberId == barberId)
            .ToListAsync();

        var transactions = await _context.Transactions
            .Where(t => t.BarberId == barberId)
            .ToListAsync();

        var backup = new
        {
            Barber = new
            {
                barber.Id,
                barber.Name,
                barber.BusinessName,
                barber.Phone,
                barber.Slug,
                barber.IsActive,
                barber.CreatedAt
            },
            Services = barber.Services.Select(s => new
            {
                s.Id,
                s.Name,
                s.Price,
                s.DurationMinutes,
                s.IsActive
            }),
            WorkingHours = barber.WorkingHours.Select(wh => new
            {
                wh.DayOfWeek,
                wh.StartTime,
                wh.EndTime,
                wh.IsActive
            }),
            Appointments = appointments.Select(a => new
            {
                a.Id,
                a.ClientName,
                a.ClientPhone,
                a.Date,
                a.Time,
                a.Status,
                ServiceName = a.Service.Name,
                ServicePrice = a.Service.Price,
                a.CreatedAt
            }),
            Transactions = transactions.Select(t => new
            {
                t.Id,
                t.Type,
                t.Amount,
                t.Description,
                t.Date
            })
        };

        var json = JsonSerializer.Serialize(backup, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        return Encoding.UTF8.GetBytes(json);
    }

    // Métodos privados para generar archivos
    private byte[] GenerateCsvAppointments(List<Appointment> appointments)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Fecha,Hora,Cliente,Teléfono,Servicio,Precio,Estado");

        foreach (var apt in appointments)
        {
            sb.AppendLine($"{apt.Date:yyyy-MM-dd},{apt.Time:HH:mm},{apt.ClientName},{apt.ClientPhone},{apt.Service.Name},{apt.Service.Price:F2},{apt.Status}");
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    private Task<byte[]> GenerateExcelAppointmentsAsync(List<Appointment> appointments)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Citas");

        worksheet.Cell(1, 1).Value = "Fecha";
        worksheet.Cell(1, 2).Value = "Hora";
        worksheet.Cell(1, 3).Value = "Cliente";
        worksheet.Cell(1, 4).Value = "Teléfono";
        worksheet.Cell(1, 5).Value = "Servicio";
        worksheet.Cell(1, 6).Value = "Precio";
        worksheet.Cell(1, 7).Value = "Estado";

        var row = 2;
        foreach (var apt in appointments)
        {
            worksheet.Cell(row, 1).Value = apt.Date.ToString("yyyy-MM-dd");
            worksheet.Cell(row, 2).Value = apt.Time.ToString("HH:mm");
            worksheet.Cell(row, 3).Value = apt.ClientName;
            worksheet.Cell(row, 4).Value = apt.ClientPhone;
            worksheet.Cell(row, 5).Value = apt.Service.Name;
            worksheet.Cell(row, 6).Value = apt.Service.Price;
            worksheet.Cell(row, 7).Value = apt.Status.ToString();
            row++;
        }

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return Task.FromResult(stream.ToArray());
    }

    private Task<byte[]> GeneratePdfAppointmentsAsync(List<Appointment> appointments, DateOnly? startDate, DateOnly? endDate)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);

                page.Header().Text("Reporte de Citas").FontSize(20).Bold();
                page.Content().Column(column =>
                {
                    if (startDate.HasValue || endDate.HasValue)
                    {
                        column.Item().Text($"Período: {startDate?.ToString("dd/MM/yyyy") ?? "Inicio"} - {endDate?.ToString("dd/MM/yyyy") ?? "Fin"}");
                    }

                    column.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                        });

                        table.Header(header =>
                        {
                            header.Cell().Text("Fecha").Bold();
                            header.Cell().Text("Hora").Bold();
                            header.Cell().Text("Cliente").Bold();
                            header.Cell().Text("Servicio").Bold();
                            header.Cell().Text("Precio").Bold();
                        });

                        foreach (var apt in appointments)
                        {
                            table.Cell().Text(apt.Date.ToString("dd/MM/yyyy"));
                            table.Cell().Text(apt.Time.ToString("HH:mm"));
                            table.Cell().Text(apt.ClientName);
                            table.Cell().Text(apt.Service.Name);
                            table.Cell().Text($"${apt.Service.Price:F2}");
                        }
                    });
                });
            });
        });

        return Task.FromResult(document.GeneratePdf());
    }

    private byte[] GenerateCsvFinances(List<Transaction> transactions)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Fecha,Tipo,Monto,Descripción");

        foreach (var t in transactions)
        {
            sb.AppendLine($"{t.Date:yyyy-MM-dd},{t.Type},{t.Amount:F2},{t.Description}");
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    private Task<byte[]> GenerateExcelFinancesAsync(List<Transaction> transactions)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Finanzas");

        worksheet.Cell(1, 1).Value = "Fecha";
        worksheet.Cell(1, 2).Value = "Tipo";
        worksheet.Cell(1, 3).Value = "Monto";
        worksheet.Cell(1, 4).Value = "Descripción";

        var row = 2;
        foreach (var t in transactions)
        {
            worksheet.Cell(row, 1).Value = t.Date.ToString("yyyy-MM-dd");
            worksheet.Cell(row, 2).Value = t.Type.ToString();
            worksheet.Cell(row, 3).Value = t.Amount;
            worksheet.Cell(row, 4).Value = t.Description;
            row++;
        }

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return Task.FromResult(stream.ToArray());
    }

    private Task<byte[]> GeneratePdfFinancesAsync(List<Transaction> transactions, DateOnly? startDate, DateOnly? endDate)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);

                page.Header().Text("Reporte Financiero").FontSize(20).Bold();
                page.Content().Column(column =>
                {
                    if (startDate.HasValue || endDate.HasValue)
                    {
                        column.Item().Text($"Período: {startDate?.ToString("dd/MM/yyyy") ?? "Inicio"} - {endDate?.ToString("dd/MM/yyyy") ?? "Fin"}");
                    }

                    var income = transactions.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount);
                    var expenses = transactions.Where(t => t.Type == TransactionType.Expense).Sum(t => t.Amount);
                    column.Item().Text($"Ingresos: ${income:F2}");
                    column.Item().Text($"Egresos: ${expenses:F2}");
                    column.Item().Text($"Ganancia: ${income - expenses:F2}").Bold();

                    column.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                        });

                        table.Header(header =>
                        {
                            header.Cell().Text("Fecha").Bold();
                            header.Cell().Text("Tipo").Bold();
                            header.Cell().Text("Monto").Bold();
                            header.Cell().Text("Descripción").Bold();
                        });

                        foreach (var t in transactions)
                        {
                            table.Cell().Text(t.Date.ToString("dd/MM/yyyy"));
                            table.Cell().Text(t.Type.ToString());
                            table.Cell().Text($"${t.Amount:F2}");
                            table.Cell().Text(t.Description);
                        }
                    });
                });
            });
        });

        return Task.FromResult(document.GeneratePdf());
    }

    private byte[] GenerateCsvClients(List<ClientExportDto> clients)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Cliente,Teléfono,Total Citas,Última Cita,Total Gastado");

        foreach (var client in clients)
        {
            sb.AppendLine($"{client.ClientName},{client.ClientPhone},{client.TotalAppointments},{client.LastAppointment:yyyy-MM-dd},{client.TotalSpent:F2}");
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    private Task<byte[]> GenerateExcelClientsAsync(List<ClientExportDto> clients)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Clientes");

        worksheet.Cell(1, 1).Value = "Cliente";
        worksheet.Cell(1, 2).Value = "Teléfono";
        worksheet.Cell(1, 3).Value = "Total Citas";
        worksheet.Cell(1, 4).Value = "Última Cita";
        worksheet.Cell(1, 5).Value = "Total Gastado";

        var row = 2;
        foreach (var client in clients)
        {
            worksheet.Cell(row, 1).Value = client.ClientName;
            worksheet.Cell(row, 2).Value = client.ClientPhone;
            worksheet.Cell(row, 3).Value = client.TotalAppointments;
            worksheet.Cell(row, 4).Value = client.LastAppointment.ToString("yyyy-MM-dd");
            worksheet.Cell(row, 5).Value = client.TotalSpent;
            row++;
        }

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return Task.FromResult(stream.ToArray());
    }

    private Task<byte[]> GeneratePdfClientsAsync(List<ClientExportDto> clients)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);

                page.Header().Text("Reporte de Clientes").FontSize(20).Bold();
                page.Content().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                    });

                    table.Header(header =>
                    {
                        header.Cell().Text("Cliente").Bold();
                        header.Cell().Text("Teléfono").Bold();
                        header.Cell().Text("Total Citas").Bold();
                        header.Cell().Text("Última Cita").Bold();
                        header.Cell().Text("Total Gastado").Bold();
                    });

                    foreach (var client in clients)
                    {
                        table.Cell().Text(client.ClientName);
                        table.Cell().Text(client.ClientPhone);
                        table.Cell().Text(client.TotalAppointments.ToString());
                        table.Cell().Text(client.LastAppointment.ToString("dd/MM/yyyy"));
                        table.Cell().Text($"${client.TotalSpent:F2}");
                    }
                });
            });
        });

        return Task.FromResult(document.GeneratePdf());
    }

    private class ClientExportDto
    {
        public string ClientName { get; set; } = string.Empty;
        public string ClientPhone { get; set; } = string.Empty;
        public int TotalAppointments { get; set; }
        public DateOnly LastAppointment { get; set; }
        public decimal TotalSpent { get; set; }
    }
}

