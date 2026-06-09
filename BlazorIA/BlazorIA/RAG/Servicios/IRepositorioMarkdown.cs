namespace BlazorIA.RAG.Servicios
{
    public interface IRepositorioMarkdown
    {
        Task<string?> ObtenerContenidoPorNombreArchivo(string nombreArchivo);
    }
}
