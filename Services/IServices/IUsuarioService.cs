using BarberNic.Models.Entities;

namespace BarberNic.Services.IServices;

public interface IUsuarioService
{
    List<Usuario> ObtenerTodos();
    Usuario? ObtenerPorId(int id);
    Usuario? ObtenerPorNombreUsuario(string nombreUsuario);
    bool Crear(Usuario usuario);
    bool Actualizar(Usuario usuario);
    bool Eliminar(int id);
    bool ExisteNombreUsuario(string nombreUsuario, int? idExcluir = null);
}

