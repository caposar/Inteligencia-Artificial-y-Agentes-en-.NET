using Microsoft.AspNetCore.Components.Forms;

namespace BlazorIA.RAG.Servicios
{
    public interface IVectorStore
    {
        Task SubirArchivos(List<IBrowserFile> archivos, CancellationToken cancellationToken = default);
    }
}
