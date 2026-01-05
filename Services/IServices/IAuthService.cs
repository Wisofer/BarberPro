using BarberPro.Models.Entities;

namespace BarberPro.Services.IServices;

public interface IAuthService
{
    Usuario? ValidarUsuario(string nombreUsuario, string contrasena);
    bool EsAdministrador(Usuario usuario);
    bool EsUsuarioNormal(Usuario usuario);
}

