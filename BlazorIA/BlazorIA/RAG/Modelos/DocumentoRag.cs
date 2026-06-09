using Azure.Search.Documents.Indexes;

namespace BlazorIA.RAG.Modelos
{
    public class DocumentoRag
    {
        [SimpleField(IsKey = true, IsFilterable = true)]
        public string Id { get; set; } = null!;

        [SearchableField(IsFilterable = true)]
        public string TituloDocumento { get; set; } = null!;

        [SearchableField]
        public string Texto { get; set; } = null!;

        [SimpleField(IsFilterable = true)]
        public int NumeroFragmento { get; set; }

        [VectorSearchField(VectorSearchDimensions = 1536, VectorSearchProfileName = "perfil-vector")]
        public float[] Embedding { get; set; } = null!;
    }
}
