using ModelContextProtocol.Server;
using System.ComponentModel;

namespace ServidorMCP.Recursos
{
    [McpServerResourceType]
    public class DocumentosRecursos(IWebHostEnvironment env)
    {
        [McpServerResource(
        UriTemplate = "documentos://manual-politicas",
        MimeType = "text/markdown"),
        Description("Manual de políticas internas de la empresa en formato markdown.")]
        public string PoliticasInternas()
        {
            var ruta = Path.Combine(
                            env.ContentRootPath,
                            "Documentos",
                            "manual-de-politicas-internas.md");

            if (!File.Exists(ruta))
            {
                return "No se encontró el documento de políticas internas";
            }

            return File.ReadAllText(ruta);
        }
    }
}
