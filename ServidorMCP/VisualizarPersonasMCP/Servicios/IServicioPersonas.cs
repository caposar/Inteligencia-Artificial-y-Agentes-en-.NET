using VisualizarPersonasMCP.Entidades;

namespace VisualizarPersonasMCP.Servicios
{
    public interface IServicioPersonas
    {
        Task<IEnumerable<Persona>> ObtenerTodas();
    }
}
