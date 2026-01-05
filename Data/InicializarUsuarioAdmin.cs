using BarberPro.Data;
using BarberPro.Models.Entities;
using BarberPro.Utils;
using Microsoft.EntityFrameworkCore;

namespace BarberPro.Data;

public static class InicializarUsuarioAdmin
{
    public static void CrearAdminSiNoExiste(ApplicationDbContext context, ILogger logger)
    {
        try
        {
            // Verificar si ya existe un usuario admin
            var existeAdmin = context.Usuarios
                .Any(u => u.NombreUsuario.ToLower() == "admin");

            if (existeAdmin)
            {
                logger.LogInformation("El usuario admin ya existe en la base de datos.");
                return;
            }

            // Crear usuario admin
            var admin = new Usuario
            {
                NombreUsuario = "admin",
                Contrasena = PasswordHelper.HashPassword("admin"),
                NombreCompleto = "Administrador del Sistema",
                Rol = "Administrador",
                Activo = true
            };

            context.Usuarios.Add(admin);
            context.SaveChanges();

            logger.LogInformation("Usuario admin creado exitosamente. Credenciales: admin/admin");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error al crear el usuario admin en la base de datos.");
        }
    }
}

