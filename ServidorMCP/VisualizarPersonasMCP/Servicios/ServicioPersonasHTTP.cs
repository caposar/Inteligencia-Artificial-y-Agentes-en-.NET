using System.Net.Http.Json;
using VisualizarPersonasMCP.Entidades;

namespace VisualizarPersonasMCP.Servicios
{
    public class ServicioPersonasHTTP(HttpClient httpClient) : IServicioPersonas
    {
        public async Task<IEnumerable<Persona>> ObtenerTodas()
        {
            var baseURL = "https://servidormcp20260423155102-erhmbda7csfehagw.eastus-01.azurewebsites.net";
            var url = $"{baseURL}/api/personas";
            var resultado = await httpClient.GetFromJsonAsync<IEnumerable<Persona>>(url);
            return resultado!;
        }
    }
}
