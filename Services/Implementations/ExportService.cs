using BarberNic.Models.Entities;
using BarberNic.Services.Interfaces;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Text;
using System.Text.Json;

namespace BarberNic.Services.Implementations;

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
        var barber = await _context.Barbers
            .FirstOrDefaultAsync(b => b.Id == barberId);

        if (barber == null)
            throw new KeyNotFoundException("Barbero no encontrado");

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
            "pdf" => await GeneratePdfAppointmentsAsync(appointments, barber, startDate, endDate),
            _ => throw new ArgumentException($"Formato no soportado: {format}")
        };
    }

    public async Task<byte[]> ExportFinancesAsync(int barberId, DateOnly? startDate, DateOnly? endDate, string format)
    {
        var barber = await _context.Barbers
            .FirstOrDefaultAsync(b => b.Id == barberId);

        if (barber == null)
            throw new KeyNotFoundException("Barbero no encontrado");

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
            "pdf" => await GeneratePdfFinancesAsync(transactions, barber, startDate, endDate),
            _ => throw new ArgumentException($"Formato no soportado: {format}")
        };
    }

    public async Task<byte[]> ExportClientsAsync(int barberId, string format)
    {
        var barber = await _context.Barbers
            .FirstOrDefaultAsync(b => b.Id == barberId);

        if (barber == null)
            throw new KeyNotFoundException("Barbero no encontrado");

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
            "pdf" => await GeneratePdfClientsAsync(clients, barber),
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

    private Task<byte[]> GeneratePdfAppointmentsAsync(List<Appointment> appointments, Barber barber, DateOnly? startDate, DateOnly? endDate)
    {
        var totalAppointments = appointments.Count;
        var confirmedAppointments = appointments.Count(a => a.Status == AppointmentStatus.Confirmed);
        var totalRevenue = appointments
            .Where(a => a.Status == AppointmentStatus.Confirmed)
            .Sum(a => a.Service.Price);
        var averagePrice = confirmedAppointments > 0 ? totalRevenue / confirmedAppointments : 0;
        var currentDate = DateTime.Now.ToString("dd/MM/yyyy");
        var periodText = startDate.HasValue || endDate.HasValue
            ? $"{startDate?.ToString("dd/MM/yyyy") ?? "Inicio"} - {endDate?.ToString("dd/MM/yyyy") ?? "Fin"}"
            : "Todos los registros";

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);

                // Header con título del sistema
                page.Header().Column(column =>
                {
                    column.Item().Text("BARBERNIC - REPORTE DE CITAS")
                        .FontSize(24)
                        .Bold()
                        .FontColor(Colors.Grey.Darken3);
                    
                    column.Item().PaddingTop(10);
                    
                    column.Item().Row(row =>
                    {
                        row.RelativeItem().Text($"Barbero: {barber.BusinessName ?? barber.Name}")
                            .FontSize(12)
                            .FontColor(Colors.Grey.Darken1);
                        row.RelativeItem().AlignRight().Text($"Fecha: {currentDate}")
                            .FontSize(12)
                            .FontColor(Colors.Grey.Darken1);
                    });
                });

                page.Content().Column(column =>
                {
                    column.Spacing(15);

                    // Período
                    column.Item().Text($"Período: {periodText}")
                        .FontSize(11)
                        .FontColor(Colors.Grey.Darken2);

                    // Estadísticas
                    column.Item().PaddingTop(5).PaddingBottom(10).BorderBottom(1).BorderColor(Colors.Grey.Lighten2);
                    
                    column.Item().PaddingBottom(8).Text("ESTADÍSTICAS")
                        .FontSize(14)
                        .Bold()
                        .FontColor(Colors.Grey.Darken3);

                    column.Item().Row(row =>
                    {
                        row.RelativeItem().Text($"Citas registradas: {totalAppointments}")
                            .FontSize(11);
                        row.RelativeItem().Text($"Citas confirmadas: {confirmedAppointments}")
                            .FontSize(11);
                    });

                    column.Item().Row(row =>
                    {
                        row.RelativeItem().Text($"Ingresos totales: ${totalRevenue:F2}")
                            .FontSize(11)
                            .Bold();
                        row.RelativeItem().Text($"Precio promedio: ${averagePrice:F2}")
                            .FontSize(11);
                    });

                    column.Item().PaddingTop(10).PaddingBottom(5);

                    // Tabla de citas
                    column.Item().PaddingBottom(8).Text("HISTORIAL DE CITAS")
                        .FontSize(14)
                        .Bold()
                        .FontColor(Colors.Grey.Darken3);

                    column.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(1.5f);
                            columns.RelativeColumn(2.5f);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(1.5f);
                        });

                        // Header de la tabla
                        table.Header(header =>
                        {
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(8).Text("Fecha").Bold().FontSize(10);
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(8).Text("Hora").Bold().FontSize(10);
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(8).Text("Cliente").Bold().FontSize(10);
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(8).Text("Servicio").Bold().FontSize(10);
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(8).Text("Precio").Bold().FontSize(10);
                        });

                        // Filas de datos
                        foreach (var apt in appointments)
                        {
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(6).Text(apt.Date.ToString("dd/MM/yyyy")).FontSize(9);
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(6).Text(apt.Time.ToString("HH:mm")).FontSize(9);
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(6).Text(apt.ClientName).FontSize(9);
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(6).Text(apt.Service.Name).FontSize(9);
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(6).Text($"${apt.Service.Price:F2}").FontSize(9);
                        }
                    });

                    // Nota legal
                    column.Item().PaddingTop(20).BorderTop(1).BorderColor(Colors.Grey.Lighten2).PaddingTop(10);
                    column.Item().PaddingBottom(5).Text("NOTA LEGAL")
                        .FontSize(10)
                        .Bold()
                        .FontColor(Colors.Grey.Darken2);
                    column.Item().Text("Este reporte es generado automáticamente por BarberNic. La información presentada es de carácter informativo y refleja los datos registrados en el sistema hasta la fecha de generación del reporte.")
                        .FontSize(8)
                        .FontColor(Colors.Grey.Darken1)
                        .LineHeight(1.4f);
                });

                // Footer
                page.Footer().AlignCenter().Text($"© {DateTime.Now.Year} BarberNic - Sistema de Gestión de Barberías")
                    .FontSize(8)
                    .FontColor(Colors.Grey.Medium);
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

    private Task<byte[]> GeneratePdfFinancesAsync(List<Transaction> transactions, Barber barber, DateOnly? startDate, DateOnly? endDate)
    {
        var income = transactions.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount);
        var expenses = transactions.Where(t => t.Type == TransactionType.Expense).Sum(t => t.Amount);
        var profit = income - expenses;
        var totalTransactions = transactions.Count;
        var incomeCount = transactions.Count(t => t.Type == TransactionType.Income);
        var expenseCount = transactions.Count(t => t.Type == TransactionType.Expense);
        var currentDate = DateTime.Now.ToString("dd/MM/yyyy");
        var periodText = startDate.HasValue || endDate.HasValue
            ? $"{startDate?.ToString("dd/MM/yyyy") ?? "Inicio"} - {endDate?.ToString("dd/MM/yyyy") ?? "Fin"}"
            : "Todos los registros";

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);

                // Header con título del sistema
                page.Header().Column(column =>
                {
                    column.Item().Text("BARBERNIC - REPORTE FINANCIERO")
                        .FontSize(24)
                        .Bold()
                        .FontColor(Colors.Grey.Darken3);
                    
                    column.Item().PaddingTop(10);
                    
                    column.Item().Row(row =>
                    {
                        row.RelativeItem().Text($"Barbero: {barber.BusinessName ?? barber.Name}")
                            .FontSize(12)
                            .FontColor(Colors.Grey.Darken1);
                        row.RelativeItem().AlignRight().Text($"Fecha: {currentDate}")
                            .FontSize(12)
                            .FontColor(Colors.Grey.Darken1);
                    });
                });

                page.Content().Column(column =>
                {
                    column.Spacing(15);

                    // Período
                    column.Item().Text($"Período: {periodText}")
                        .FontSize(11)
                        .FontColor(Colors.Grey.Darken2);

                    // Estadísticas
                    column.Item().PaddingTop(5).PaddingBottom(10).BorderBottom(1).BorderColor(Colors.Grey.Lighten2);
                    
                    column.Item().PaddingBottom(8).Text("ESTADÍSTICAS")
                        .FontSize(14)
                        .Bold()
                        .FontColor(Colors.Grey.Darken3);

                    column.Item().Row(row =>
                    {
                        row.RelativeItem().Text($"Transacciones registradas: {totalTransactions}")
                            .FontSize(11);
                        row.RelativeItem().Text($"Ingresos: {incomeCount}")
                            .FontSize(11);
                    });

                    column.Item().Row(row =>
                    {
                        row.RelativeItem().Text($"Egresos: {expenseCount}")
                            .FontSize(11);
                        row.RelativeItem().Text($"Ganancia neta: ${profit:F2}")
                            .FontSize(11)
                            .Bold()
                            .FontColor(profit >= 0 ? Colors.Green.Darken2 : Colors.Red.Darken2);
                    });

                    column.Item().PaddingTop(10).PaddingBottom(5);

                    // Resumen financiero
                    column.Item().PaddingBottom(8).Text("RESUMEN FINANCIERO")
                        .FontSize(14)
                        .Bold()
                        .FontColor(Colors.Grey.Darken3);

                    column.Item().Row(row =>
                    {
                        row.RelativeItem().Text($"Ingresos totales: ${income:F2}")
                            .FontSize(12)
                            .Bold()
                            .FontColor(Colors.Green.Darken2);
                        row.RelativeItem().Text($"Egresos totales: ${expenses:F2}")
                            .FontSize(12)
                            .Bold()
                            .FontColor(Colors.Red.Darken2);
                    });

                    column.Item().PaddingTop(10).PaddingBottom(5);

                    // Tabla de transacciones
                    column.Item().PaddingBottom(8).Text("HISTORIAL DE TRANSACCIONES")
                        .FontSize(14)
                        .Bold()
                        .FontColor(Colors.Grey.Darken3);

                    column.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(1.5f);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(3);
                        });

                        // Header de la tabla
                        table.Header(header =>
                        {
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(8).Text("Fecha").Bold().FontSize(10);
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(8).Text("Tipo").Bold().FontSize(10);
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(8).Text("Monto").Bold().FontSize(10);
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(8).Text("Descripción").Bold().FontSize(10);
                        });

                        // Filas de datos
                        foreach (var t in transactions)
                        {
                            var amountColor = t.Type == TransactionType.Income ? Colors.Green.Darken2 : Colors.Red.Darken2;
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(6).Text(t.Date.ToString("dd/MM/yyyy")).FontSize(9);
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(6).Text(t.Type == TransactionType.Income ? "Ingreso" : "Egreso").FontSize(9);
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(6).Text($"${t.Amount:F2}").FontSize(9).FontColor(amountColor);
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(6).Text(t.Description ?? "-").FontSize(9);
                        }
                    });

                    // Nota legal
                    column.Item().PaddingTop(20).BorderTop(1).BorderColor(Colors.Grey.Lighten2).PaddingTop(10);
                    column.Item().PaddingBottom(5).Text("NOTA LEGAL")
                        .FontSize(10)
                        .Bold()
                        .FontColor(Colors.Grey.Darken2);
                    column.Item().Text("Este reporte es generado automáticamente por BarberNic. La información financiera presentada es de carácter informativo y refleja los datos registrados en el sistema hasta la fecha de generación del reporte.")
                        .FontSize(8)
                        .FontColor(Colors.Grey.Darken1)
                        .LineHeight(1.4f);
                });

                // Footer
                page.Footer().AlignCenter().Text($"© {DateTime.Now.Year} BarberNic - Sistema de Gestión de Barberías")
                    .FontSize(8)
                    .FontColor(Colors.Grey.Medium);
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

    private Task<byte[]> GeneratePdfClientsAsync(List<ClientExportDto> clients, Barber barber)
    {
        var totalClients = clients.Count;
        var totalAppointments = clients.Sum(c => c.TotalAppointments);
        var totalRevenue = clients.Sum(c => c.TotalSpent);
        var averageAppointments = totalClients > 0 ? (double)totalAppointments / totalClients : 0;
        var currentDate = DateTime.Now.ToString("dd/MM/yyyy");

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);

                // Header con título del sistema
                page.Header().Column(column =>
                {
                    column.Item().Text("BARBERNIC - REPORTE DE CLIENTES")
                        .FontSize(24)
                        .Bold()
                        .FontColor(Colors.Grey.Darken3);
                    
                    column.Item().PaddingTop(10);
                    
                    column.Item().Row(row =>
                    {
                        row.RelativeItem().Text($"Barbero: {barber.BusinessName ?? barber.Name}")
                            .FontSize(12)
                            .FontColor(Colors.Grey.Darken1);
                        row.RelativeItem().AlignRight().Text($"Fecha: {currentDate}")
                            .FontSize(12)
                            .FontColor(Colors.Grey.Darken1);
                    });
                });

                page.Content().Column(column =>
                {
                    column.Spacing(15);

                    // Estadísticas
                    column.Item().PaddingTop(5).PaddingBottom(10).BorderBottom(1).BorderColor(Colors.Grey.Lighten2);
                    
                    column.Item().PaddingBottom(8).Text("ESTADÍSTICAS")
                        .FontSize(14)
                        .Bold()
                        .FontColor(Colors.Grey.Darken3);

                    column.Item().Row(row =>
                    {
                        row.RelativeItem().Text($"Clientes registrados: {totalClients}")
                            .FontSize(11);
                        row.RelativeItem().Text($"Total de citas: {totalAppointments}")
                            .FontSize(11);
                    });

                    column.Item().Row(row =>
                    {
                        row.RelativeItem().Text($"Ingresos totales: ${totalRevenue:F2}")
                            .FontSize(11)
                            .Bold();
                        row.RelativeItem().Text($"Promedio de citas por cliente: {averageAppointments:F1}")
                            .FontSize(11);
                    });

                    column.Item().PaddingTop(10).PaddingBottom(5);

                    // Tabla de clientes
                    column.Item().PaddingBottom(8).Text("HISTORIAL DE CLIENTES")
                        .FontSize(14)
                        .Bold()
                        .FontColor(Colors.Grey.Darken3);

                    column.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(2.5f);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(1.5f);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(2);
                        });

                        // Header de la tabla
                        table.Header(header =>
                        {
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(8).Text("Cliente").Bold().FontSize(10);
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(8).Text("Teléfono").Bold().FontSize(10);
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(8).Text("Total Citas").Bold().FontSize(10);
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(8).Text("Última Cita").Bold().FontSize(10);
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(8).Text("Total Gastado").Bold().FontSize(10);
                        });

                        // Filas de datos
                        foreach (var client in clients)
                        {
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(6).Text(client.ClientName).FontSize(9);
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(6).Text(client.ClientPhone).FontSize(9);
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(6).Text(client.TotalAppointments.ToString()).FontSize(9);
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(6).Text(client.LastAppointment.ToString("dd/MM/yyyy")).FontSize(9);
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(6).Text($"${client.TotalSpent:F2}").FontSize(9);
                        }
                    });

                    // Nota legal
                    column.Item().PaddingTop(20).BorderTop(1).BorderColor(Colors.Grey.Lighten2).PaddingTop(10);
                    column.Item().PaddingBottom(5).Text("NOTA LEGAL")
                        .FontSize(10)
                        .Bold()
                        .FontColor(Colors.Grey.Darken2);
                    column.Item().Text("Este reporte es generado automáticamente por BarberNic. La información de clientes presentada es de carácter informativo y refleja los datos registrados en el sistema hasta la fecha de generación del reporte.")
                        .FontSize(8)
                        .FontColor(Colors.Grey.Darken1)
                        .LineHeight(1.4f);
                });

                // Footer
                page.Footer().AlignCenter().Text($"© {DateTime.Now.Year} BarberNic - Sistema de Gestión de Barberías")
                    .FontSize(8)
                    .FontColor(Colors.Grey.Medium);
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

