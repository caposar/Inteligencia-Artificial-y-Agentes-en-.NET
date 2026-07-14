using ModelContextProtocol.Server;
using ServidorMCP.DTOs;
using ServidorMCP.Entidades;
using ServidorMCP.Servicios;
using System.ComponentModel;

namespace ServidorMCP.Tools
{
    [McpServerToolType]
    public class PersonasTools(IRepositorioPersonas repositorioPersonas)
    {
        [McpServerTool, Description("Obtiene el listado de todas las personas registradas.")]
        public List<Persona> ObtenerTodas()
        {
            var personas = repositorioPersonas.ObtenerTodas();
            return personas;
        }

        [McpServerTool, Description("Obtiene una persona por su identificador.")]
        public Persona? ObtenerPorId(
        [Description("Identificador único de la persona.")] int id)
        {
            var persona = repositorioPersonas.ObtenerPorId(id);
            return persona;
        }

        [McpServerTool, Description("Activa o desactiva una persona según su identificador.")]
        public ResultadoOperacionDTO ActualizarActivo(
        [Description("Identificador de la persona.")] int id,
        [Description("Indica si la persona estará activa (true) o inactiva (false).")] bool activo)
        {
            var actualizado = repositorioPersonas.ActualizarActivo(id, activo);

            if (!actualizado)
            {
                return new ResultadoOperacionDTO(false, $"No se pudo actualizar la persona con id {id}. Verifique que exista.");
            }

            return new ResultadoOperacionDTO(true, "La actualización fue realizada exitosamente");
        }
    }
}
