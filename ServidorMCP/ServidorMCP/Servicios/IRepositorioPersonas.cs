using ServidorMCP.Entidades;

namespace ServidorMCP.Servicios
{
    public interface IRepositorioPersonas
    {
        bool ActualizarActivo(int id, bool activo);
        Persona? ObtenerPorId(int id);
        List<Persona> ObtenerTodas();
    }
}
