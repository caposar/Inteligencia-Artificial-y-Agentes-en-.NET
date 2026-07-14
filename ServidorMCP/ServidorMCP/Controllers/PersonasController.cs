using Microsoft.AspNetCore.Mvc;
using ServidorMCP.Entidades;
using ServidorMCP.Servicios;

namespace ServidorMCP.Controllers
{
    [ApiController]
    [Route("api/personas")]
    public class PersonasController(IRepositorioPersonas repositorioPersonas)
    {
        [HttpGet]
        public List<Persona> Obtener()
        {
            return repositorioPersonas.ObtenerTodas();
        }
    }
}
