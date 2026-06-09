using System.Text.Json.Serialization;

namespace BlazorIA.RAG.Modelos
{
    public class MetadataFuentes
    {
        [JsonPropertyName("fuentesUsadas")]
        public List<string> FuentesUsadas { get; set; } = [];
    }
}
