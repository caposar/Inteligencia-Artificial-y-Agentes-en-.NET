using BlazorIA.RAG.Modelos;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel.Connectors.PgVector;

namespace BlazorIA.RAG.Servicios
{
    /// <summary>
    /// Encargado de procesar los archivos .md subidos desde la página "Subir archivos":
    /// los fragmenta, genera sus embeddings y los guarda en Supabase (tabla "documentos_rag").
    /// </summary>
    public class VectorStoreClienteSupabase(
        IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator,
        PostgresVectorStore vectorStore) : IVectorStore
    {
        private const string NombreColeccion = "documentos_rag";

        // Tamaño máximo (en caracteres) de cada fragmento de texto que se vectoriza.
        // Fragmentos más chicos = búsquedas más precisas pero más registros en la DB.
        private const int TamanioMaximoFragmento = 1200;

        public async Task SubirArchivos(List<IBrowserFile> archivos, CancellationToken cancellationToken = default)
        {
            if (archivos is null || archivos.Count == 0) return;

            var collection = vectorStore.GetCollection<string, DocumentoRagSupabase>(NombreColeccion);

            // Crea la tabla "documentos_rag" en Supabase la primera vez que se ejecuta.
            // En ejecuciones siguientes no hace nada si la tabla ya existe.
            await collection.EnsureCollectionExistsAsync(cancellationToken);

            foreach (var archivo in archivos)
            {
                using var reader = new StreamReader(archivo.OpenReadStream(maxAllowedSize: 10 * 1024 * 1024));
                var contenido = await reader.ReadToEndAsync(cancellationToken);

                var fragmentos = DividirEnFragmentos(contenido, TamanioMaximoFragmento);

                for (int i = 0; i < fragmentos.Count; i++)
                {
                    // Genera el vector de 1536 dimensiones para este fragmento de texto.
                    var embedding = await embeddingGenerator.GenerateVectorAsync(fragmentos[i], cancellationToken: cancellationToken);

                    var nombreValido = Path.GetFileNameWithoutExtension(archivo.Name).Replace(" ", "-");

                    var doc = new DocumentoRagSupabase
                    {
                        // Id único por fragmento. Incluye el nombre del archivo y el número
                        // de fragmento para que sea legible al inspeccionar la tabla en Supabase.
                        Id = $"{nombreValido}-{i}-{Guid.NewGuid()}",
                        TituloDocumento = archivo.Name,
                        Texto = fragmentos[i],
                        NumeroFragmento = i,
                        Embedding = embedding
                    };

                    // Inserta o actualiza el fragmento en la tabla.
                    await collection.UpsertAsync(doc, cancellationToken: cancellationToken);
                }
            }
        }

        /// <summary>
        /// Divide un texto largo en fragmentos de hasta <paramref name="maxCaracteres"/>,
        /// respetando los saltos de línea para no cortar párrafos a la mitad.
        /// </summary>
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
