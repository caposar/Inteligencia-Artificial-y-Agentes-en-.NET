using BlazorIA.RAG.Modelos;

namespace BlazorIA.RAG.Servicios
{
    public interface IServicioRag
    {
        Task Inicializar(CancellationToken cancellationToken = default);
        Task<List<ResultadoBusquedaRAG>> BuscarContextoRelevante(string pregunta, int top = 3, float scoreMinimo = 0.6f, CancellationToken cancellationToken = default);
    }
}
