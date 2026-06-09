using Microsoft.Extensions.VectorData;

namespace BlazorIA.RAG.Modelos
{
    public class FragmentoDocumentoVector
    {
        [VectorStoreKey]
        public Guid Id { get; set; }

        [VectorStoreData(IsIndexed = true)]
        public string TituloDocumento { get; set; } = string.Empty;

        [VectorStoreData(IsFullTextIndexed = true)]
        public string Texto { get; set; } = string.Empty;

        [VectorStoreVector(
            Dimensions: 1536,
            DistanceFunction = DistanceFunction.CosineSimilarity)]
        public ReadOnlyMemory<float> Embedding { get; set; }

    }
}
