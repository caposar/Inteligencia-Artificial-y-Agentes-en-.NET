using BlazorIA.RAG.Modelos;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel.Connectors.InMemory;

namespace BlazorIA.RAG.Servicios
{
    public class ServicioRagMemoria : IServicioRag
    {
        private readonly ServicioDocumentosEnMemoria servicioDocumentosEnMemoria;
        private readonly IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator;
        private readonly VectorStoreCollection<Guid, FragmentoDocumentoVector> collection;
        private bool inicializado;

        public ServicioRagMemoria(ServicioDocumentosEnMemoria servicioDocumentosEnMemoria,
            IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator,
            InMemoryVectorStore vectorStore)
        {
            this.servicioDocumentosEnMemoria = servicioDocumentosEnMemoria;
            this.embeddingGenerator = embeddingGenerator;

            collection = vectorStore.GetCollection<Guid, FragmentoDocumentoVector>("documentos");
        }

        public async Task<List<ResultadoBusquedaRAG>> BuscarContextoRelevante(string pregunta, int top = 3,
            float scoreMinimo = 0.6f,
                    CancellationToken cancellationToken = default)
        {
            await Inicializar(cancellationToken);

            var preguntaEmbedding = await embeddingGenerator.GenerateVectorAsync(pregunta, cancellationToken: cancellationToken);

            var resultados = new List<ResultadoBusquedaRAG>();

            await foreach (var resultado in collection.SearchAsync(preguntaEmbedding, top: top,
                cancellationToken: cancellationToken))
            {
                if (resultado.Score < scoreMinimo)
                    continue;

                resultados.Add(new ResultadoBusquedaRAG(resultado.Record.TituloDocumento, resultado.Record.Texto));
            }

            return resultados;
        }

        public async Task Inicializar(CancellationToken cancellationToken = default)
        {
            if (inicializado)
            {
                return;
            }

            await collection.EnsureCollectionExistsAsync(cancellationToken);

            var documentos = servicioDocumentosEnMemoria.ObtenerDocumentos();

            foreach (var documento in documentos)
            {
                var fragmentos = documento
                            .Contenido
                            .Split("\n", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

                foreach (var fragmento in fragmentos)
                {
                    var vector = await embeddingGenerator.GenerateVectorAsync(fragmento,
                                                        cancellationToken: cancellationToken);

                    var registro = new FragmentoDocumentoVector
                    {
                        Id = Guid.NewGuid(),
                        TituloDocumento = documento.Titulo,
                        Texto = fragmento,
                        Embedding = vector
                    };

                    await collection.UpsertAsync(registro, cancellationToken);
                }
            }

            inicializado = true;
        }
    }
}
