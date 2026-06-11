using Microsoft.Extensions.VectorData;

namespace BlazorIA.RAG.Modelos
{
    public class DocumentoRagSupabase
    {
        [VectorStoreKey]
        public string Id { get; set; } = null!;

        [VectorStoreData(IsIndexed = true)]
        public string TituloDocumento { get; set; } = string.Empty;

        [VectorStoreData(IsFullTextIndexed = true)]
        public string Texto { get; set; } = string.Empty;

        [VectorStoreData]
        public int NumeroFragmento { get; set; }

        // Mantenemos 1536 dimensiones para text-embedding-3-small de OpenAI/GitHub Models.
        // Si en el futuro usas qwen3-embedding local, recuerda cambiar este número a sus dimensiones correspondientes (ej. 768 o 1024).
        [VectorStoreVector(Dimensions: 1536, DistanceFunction = DistanceFunction.CosineSimilarity)]
        public ReadOnlyMemory<float> Embedding { get; set; }
    }
}
