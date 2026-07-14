using ServidorMCP.Entidades;

namespace ServidorMCP.Servicios
{
    public class RepositorioPersonasMemoria : IRepositorioPersonas
    {
        private List<Persona> _personas;

        public RepositorioPersonasMemoria()
        {
            _personas = new List<Persona>
            {
                new Persona
                {
                    Id = 1,
                    Nombre = "Felipe Gavilán",
                    Email = "Felipe.Gavilan@email.com",
                    Salario = 50000,
                    Activo = true
                },
                new Persona
                {
                    Id = 2,
                    Nombre = "Claudia Rodríguez",
                    Email = "claudia.rodriguez@email.com",
                    Salario = 65000,
                    Activo = true
                },
                new Persona
                {
                    Id = 3,
                    Nombre = "Carlos Rodríguez",
                    Email = "carlos.rodriguez@email.com",
                    Salario = 45000,
                    Activo = false
                }
            };

        }

        public bool ActualizarActivo(int id, bool activo)
        {
            var persona = _personas.FirstOrDefault(p => p.Id == id);

            if (persona is null)
            {
                return false;
            }

            persona.Activo = activo;
            return true;
        }

        public Persona? ObtenerPorId(int id)
        {
            return _personas.FirstOrDefault(p => p.Id == id);
        }

        public List<Persona> ObtenerTodas()
        {
            return _personas;
        }
    }
}
