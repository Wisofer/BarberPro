using BarberPro.Models.DTOs.Requests;
using BarberPro.Models.DTOs.Responses;

namespace BarberPro.Services.Interfaces;

/// <summary>
/// Interfaz para el servicio de trabajadores/empleados
/// </summary>
public interface IEmployeeService
{
    Task<List<EmployeeDto>> GetEmployeesAsync(int ownerBarberId);
    Task<EmployeeDto?> GetEmployeeByIdAsync(int employeeId, int ownerBarberId);
    Task<EmployeeDto> CreateEmployeeAsync(int ownerBarberId, CreateEmployeeRequest request);
    Task<EmployeeDto?> UpdateEmployeeAsync(int employeeId, int ownerBarberId, UpdateEmployeeRequest request);
    Task<bool> DeleteEmployeeAsync(int employeeId, int ownerBarberId);
}

