using Azure;
using Azure.Search.Documents;
using BlazorIA.RAG.Modelos;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.AI;

namespace BlazorIA.RAG.Servicios
{
    public class VectorStoreClienteAzureSearch : IVectorStore
    {
        private readonly IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator;
        private readonly ServicioIndiceRagAzureSearch servicioIndice;
        private SearchClient searchClient;

        public VectorStoreClienteAzureSearch(
            IConfiguration configuration,
            IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator,
            ServicioIndiceRagAzureSearch servicioIndice)
        {
            this.embeddingGenerator = embeddingGenerator;
            this.servicioIndice = servicioIndice;

            var endpoint = configuration["AzureSearch:Endpoint"]!;
            var apiKey = configuration["AzureSearch:ApiKey"]!;
            var indexName = configuration["AzureSearch:IndexName"]!;

            searchClient = new SearchClient(
                new Uri(endpoint),
                indexName,
                new AzureKeyCredential(apiKey));

        }

        public async Task SubirArchivos(List<IBrowserFile> archivos, CancellationToken cancellationToken = default)
        {
            if (archivos is null || archivos.Count == 0)
            {
                return;
            }

            await servicioIndice.CrearIndiceSiNoExiste(cancellationToken);

            var documentos = new List<DocumentoRag>();

            foreach (var archivo in archivos)
            {
                using var reader = new StreamReader(
                     archivo.OpenReadStream(maxAllowedSize: 10 * 1024 * 1024));

                var contenido = await reader.ReadToEndAsync(cancellationToken);

                var fragmentos = DividirEnFragmentos(contenido, 1200);

                for (int i = 0; i < fragmentos.Count; i++)
                {
                    var embedding = await embeddingGenerator.GenerateVectorAsync(fragmentos[i],
                                                    cancellationToken: cancellationToken);

                    var nombreValido = Path.GetFileNameWithoutExtension(archivo.Name).Replace(" ", "-");

                    documentos.Add(new DocumentoRag
                    {
                        Id = $"{nombreValido}-{i}-{Guid.NewGuid()}",
                        TituloDocumento = archivo.Name,
                        Texto = fragmentos[i],
                        NumeroFragmento = i,
                        Embedding = embedding.ToArray()
                    });

                }

            }

            if (documentos.Count > 0)
            {
                await searchClient.UploadDocumentsAsync(documentos);
            }
        }

        private static List<string> DividirEnFragmentos(string texto, int maxCaracteres)
        {
            var parrafos = texto
                .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            var resultado = new List<string>();
            var actual = string.Empty;

            foreach (var parrafo in parrafos)
            {
                var candidato = string.IsNullOrWhiteSpace(actual)
                                ? parrafo
                                : actual + "\n" + parrafo;

                if (candidato.Length > maxCaracteres)
                {
                    if (!string.IsNullOrWhiteSpace(actual))
                    {
                        resultado.Add(actual);
                    }

                    actual = parrafo;
                }
                else
                {
                    actual = candidato;
                }
            }

            if (!string.IsNullOrWhiteSpace(actual))
            {
                resultado.Add(actual);
            }

            return resultado;
        }
    }
}
