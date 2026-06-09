using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using BlazorIA.RAG.Modelos;
using Microsoft.Extensions.AI;

namespace BlazorIA.RAG.Servicios
{
    public class ServicioRAGAzureSearch : IServicioRag
    {
        private readonly IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator;
        private SearchClient searchClient;

        public ServicioRAGAzureSearch(IConfiguration configuration,
                    IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator)
        {
            this.embeddingGenerator = embeddingGenerator;

            var endpoint = configuration["AzureSearch:Endpoint"]!;
            var apiKey = configuration["AzureSearch:ApiKey"]!;
            var indexName = configuration["AzureSearch:IndexName"]!;

            searchClient = new SearchClient(
                      new Uri(endpoint),
                    indexName,
                    new AzureKeyCredential(apiKey));

        }

        public async Task<List<ResultadoBusquedaRAG>> BuscarContextoRelevante(string pregunta, int top = 3,
            float scoreMinimo = 0.6F, CancellationToken cancellationToken = default)
        {
            var embeddingPregunta = await embeddingGenerator.GenerateVectorAsync(pregunta,
                                            cancellationToken: cancellationToken);

            var options = new SearchOptions
            {
                Size = top,
                Select = { nameof(DocumentoRag.Id),
                nameof(DocumentoRag.TituloDocumento),
                nameof(DocumentoRag.Texto),
                nameof(DocumentoRag.NumeroFragmento) }
            };

            options.VectorSearch = new()
            {
                Queries =
            {
                new VectorizedQuery(embeddingPregunta)
                {
                    KNearestNeighborsCount = top,
                    Fields = { nameof(DocumentoRag.Embedding) }
                }
            }
            };

            var response = await searchClient.SearchAsync<DocumentoRag>(null, options, cancellationToken);

            var resultados = new List<ResultadoBusquedaRAG>();

            await foreach (var item in response.Value.GetResultsAsync())
            {
                if (item.Score < scoreMinimo)
                    continue;

                resultados.Add(new ResultadoBusquedaRAG(item.Document.TituloDocumento, item.Document.Texto));
            }

            return resultados;

        }

        public Task Inicializar(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}
