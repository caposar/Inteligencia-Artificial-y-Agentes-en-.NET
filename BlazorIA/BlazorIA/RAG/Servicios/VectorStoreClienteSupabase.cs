using BlazorIA.RAG.Modelos;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel.Connectors.PgVector;

namespace BlazorIA.RAG.Servicios
{
    public class VectorStoreClienteSupabase(
        IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator,
        PostgresVectorStore vectorStore) : IVectorStore
    {
        public async Task SubirArchivos(List<IBrowserFile> archivos, CancellationToken cancellationToken = default)
        {
            if (archivos is null || archivos.Count == 0) return;

            var collection = vectorStore.GetCollection<string, DocumentoRagSupabase>("documentos_rag");

            // ✅ CORRECCIÓN: El método oficial en .NET 10 para crear colecciones
            await collection.EnsureCollectionExistsAsync(cancellationToken);

            foreach (var archivo in archivos)
            {
                using var reader = new StreamReader(archivo.OpenReadStream(maxAllowedSize: 10 * 1024 * 1024));
                var contenido = await reader.ReadToEndAsync(cancellationToken);

                var fragmentos = DividirEnFragmentos(contenido, 1200);

                for (int i = 0; i < fragmentos.Count; i++)
                {
                    // ✅ Mantenemos GenerateVectorAsync que ya te funciona perfecto
                    var embedding = await embeddingGenerator.GenerateVectorAsync(fragmentos[i], cancellationToken: cancellationToken);
                    var nombreValido = Path.GetFileNameWithoutExtension(archivo.Name).Replace(" ", "-");

                    var doc = new DocumentoRagSupabase
                    {
                        Id = $"{nombreValido}-{i}-{Guid.NewGuid()}",
                        TituloDocumento = archivo.Name,
                        Texto = fragmentos[i],
                        NumeroFragmento = i,
                        Embedding = embedding
                    };

                    await collection.UpsertAsync(doc, cancellationToken: cancellationToken);
                }
            }
        }

        private static List<string> DividirEnFragmentos(string texto, int maxCaracteres)
        {
            var parrafos = texto.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var resultado = new List<string>();
            var actual = string.Empty;

            foreach (var parrafo in parrafos)
            {
                var candidato = string.IsNullOrWhiteSpace(actual) ? parrafo : actual + "\n" + parrafo;
                if (candidato.Length > maxCaracteres)
                {
                    if (!string.IsNullOrWhiteSpace(actual)) resultado.Add(actual);
                    actual = parrafo;
                }
                else
                {
                    actual = candidato;
                }
            }
            if (!string.IsNullOrWhiteSpace(actual)) resultado.Add(actual);

            return resultado;
        }
    }
}
