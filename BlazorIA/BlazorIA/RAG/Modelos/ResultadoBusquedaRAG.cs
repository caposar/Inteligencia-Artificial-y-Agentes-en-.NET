namespace BlazorIA.RAG.Modelos
{
    public record ResultadoBusquedaRAG(string TituloDocumento, string Texto)
    {
        public override string ToString()
        {
            return $"""
            Documento: {TituloDocumento},
            Contenido: {Texto}
            """;
        }
    }
}
