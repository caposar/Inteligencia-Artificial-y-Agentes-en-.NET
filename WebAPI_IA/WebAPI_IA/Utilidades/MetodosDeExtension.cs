using Microsoft.Extensions.AI;
using System.Text.Json;
using WebAPI_IA.DTOs;
using WebAPI_IA.Entidades;

namespace WebAPI_IA.Utilidades
{
    public static class MetodosDeExtension
    {
        public static AprobacionPendienteDTO ADTO(this SolicitudAprobacionChat solicitud)
        {
            return new AprobacionPendienteDTO
            {
                NombreTool = solicitud.NombreTool,
                Argumentos = JsonSerializer.Deserialize<Dictionary<string, object?>>(
                solicitud.ArgumentosJson,
                AIJsonUtilities.DefaultOptions) ?? []
            };
        }
    }
}
