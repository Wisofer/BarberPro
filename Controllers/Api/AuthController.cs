using BarberNic.Models.DTOs.Requests;
using BarberNic.Models.DTOs.Responses;
using BarberNic.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BarberNic.Controllers.Api;

/// <summary>
/// Controlador de autenticación
/// </summary>
[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    /// <summary>
    /// Login de Admin o Barbero
    /// </summary>
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var result = await _authService.LoginWithResultAsync(request);
            
            if (result.Success && result.Response != null)
            {
                return Ok(result.Response);
            }
            
            // Retornar mensaje de error específico
            return Unauthorized(new { message = result.ErrorMessage ?? "Credenciales inválidas" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en login");
            return StatusCode(500, new { message = "Error interno del servidor" });
        }
    }
}

