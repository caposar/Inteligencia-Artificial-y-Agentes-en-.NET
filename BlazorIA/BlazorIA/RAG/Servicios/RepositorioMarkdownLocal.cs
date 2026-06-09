namespace BlazorIA.RAG.Servicios
{
    public class RepositorioMarkdownLocal(IWebHostEnvironment env) : IRepositorioMarkdown
    {
        public async Task<string?> ObtenerContenidoPorNombreArchivo(string nombreArchivo)
        {
            var directorioArchivos = Path.Combine(env.ContentRootPath, "Archivos-Markdown");
            var rutaCompleta = Path.Combine(directorioArchivos, nombreArchivo);

            if (!File.Exists(rutaCompleta))
            {
                return null;
            }

            return await File.ReadAllTextAsync(rutaCompleta);
        }
    }
}
