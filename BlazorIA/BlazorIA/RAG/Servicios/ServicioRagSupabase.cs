using BlazorIA.RAG.Modelos;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel.Connectors.PgVector;

namespace BlazorIA.RAG.Servicios
{
    /// <summary>
    /// Implementación de IServicioRag que busca contexto en Supabase/PostgreSQL
    /// usando la extensión pgvector. Es el "motor de búsqueda" del chat con RAG.
    /// </summary>
    public class ServicioRagSupabase(
        IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator,
        PostgresVectorStore vectorStore) : IServicioRag
    {
        // Nombre de la tabla/colección en Supabase. Debe coincidir con el usado
        // en VectorStoreClienteSupabase al subir los archivos.
        private const string NombreColeccion = "documentos_rag";

        public async Task<List<ResultadoBusquedaRAG>> BuscarContextoRelevante(
            string pregunta,
            int top = 3,
            float scoreMinimo = 0.6f,
            CancellationToken cancellationToken = default)
        {
            var collection = vectorStore.GetCollection<string, DocumentoRagSupabase>(NombreColeccion);

            // 1. Convertimos la pregunta del usuario en un vector de 1536 dimensiones,
            //    usando el mismo modelo de embeddings que se usó al subir los documentos.
            var embeddingPregunta = await embeddingGenerator.GenerateVectorAsync(pregunta, cancellationToken: cancellationToken);

            // 2. Búsqueda por similitud coseno en Postgres. "top" trae los N fragmentos
            //    más cercanos, sin importar qué tan relevantes sean realmente.
            var searchResult = collection.SearchAsync(
                embeddingPregunta,
                top: top,
                cancellationToken: cancellationToken);

            var resultados = new List<ResultadoBusquedaRAG>();

            // 3. Filtramos por score mínimo: descartamos fragmentos que, aunque sean
            //    "los más cercanos de los top", no son lo suficientemente relevantes
            //    como para responder con confianza (evita alucinaciones).
            await foreach (var item in searchResult)
            {
                if (item.Score >= scoreMinimo)
                {
                    resultados.Add(new ResultadoBusquedaRAG(item.Record.TituloDocumento, item.Record.Texto));
                }
            }

            return resultados;
        }

        /// <summary>
        /// No se requiere inicialización: la tabla "documentos_rag" se crea
        /// automáticamente (EnsureCollectionExistsAsync) al subir el primer archivo.
        /// </summary>
        public Task Inicializar(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}
