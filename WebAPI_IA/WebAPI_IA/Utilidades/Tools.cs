using Microsoft.Extensions.AI;
using WebAPI_IA.Servicios;

namespace WebAPI_IA.Utilidades
{
    internal static class Tools
    {
        internal static IEnumerable<AITool> ObtenerTools(this IServiceProvider sp)
        {
            var servicioClima = sp.GetRequiredService<IServicioClima>();

            yield return AIFunctionFactory.Create(
                servicioClima.ObtenerClima,
                new AIFunctionFactoryOptions
                {
                    Name = "obtener_clima",
                    Description = "Obtiene el clima actual de la ciudad indicada"
                });

            var servicioObtenerCorreo = sp.GetRequiredService<ServicioObtenerCorreoFalso>();
            yield return AIFunctionFactory.Create(servicioObtenerCorreo.ObtenerCorreo);

            var servicioCorreos = sp.GetRequiredService<ServicioEnviarCorreoFalso>();
            var functionEnviarCorreos = AIFunctionFactory.Create(servicioCorreos.EnviarCorreo);
            yield return new ApprovalRequiredAIFunction(functionEnviarCorreos);
        }
    }
}
