using BlazorIA.RAG.Modelos;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel.Connectors.PgVector;

namespace BlazorIA.RAG.Servicios
{
    public class ServicioRagSupabase(
        IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator,
        PostgresVectorStore vectorStore) : IServicioRag
    {
        public async Task<List<ResultadoBusquedaRAG>> BuscarContextoRelevante(
            string pregunta,
            int top = 3,
            float scoreMinimo = 0.6f,
            CancellationToken cancellationToken = default)
        {
            var collection = vectorStore.GetCollection<string, DocumentoRagSupabase>("documentos_rag");

            var embeddingPregunta = await embeddingGenerator.GenerateVectorAsync(pregunta, cancellationToken: cancellationToken);

            // ✅ CAMBIO 1 y 2: Sin 'await' inicial y 'top' pasa como parámetro directo
            var searchResult = collection.SearchAsync(
                embeddingPregunta,
                top: top,
                cancellationToken: cancellationToken);

            var resultados = new List<ResultadoBusquedaRAG>();

            Console.WriteLine($"[RAG DEBUG] Pregunta: {pregunta}");

            // ✅ CAMBIO 3: Iteramos directamente sobre 'searchResult' sin usar '.Results'
            await foreach (var item in searchResult)
            {
                Console.WriteLine($"[RAG DEBUG] Score: {item.Score} | Doc: {item.Record.TituloDocumento}");

                if (item.Score >= scoreMinimo)
                {
                    resultados.Add(new ResultadoBusquedaRAG(item.Record.TituloDocumento, item.Record.Texto));
                }
            }

            return resultados;
        }

        public Task Inicializar(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}
