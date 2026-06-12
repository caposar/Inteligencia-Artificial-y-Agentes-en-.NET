using BlazorIA.RAG.Modelos;

namespace BlazorIA.RAG.Servicios
{
    /// <summary>
    /// Abstracción del motor de RAG. Cualquier implementación (Supabase, Azure Search,
    /// memoria, etc.) debe poder responder a esta única operación: dado un texto de pregunta,
    /// devolver los fragmentos de documentos más relevantes para usar como contexto del LLM.
    /// </summary>
    public interface IServicioRag
    {
        /// <summary>
        /// Inicialización opcional del motor (crear índices/tablas si hace falta).
        /// En la implementación de Supabase no hace nada, ya que la tabla
        /// se crea automáticamente al subir el primer archivo.
        /// </summary>
        Task Inicializar(CancellationToken cancellationToken = default);

        /// <summary>
        /// Busca los fragmentos de documentos semánticamente más cercanos a <paramref name="pregunta"/>.
        /// </summary>
        /// <param name="pregunta">Texto del usuario, se convierte internamente a embedding.</param>
        /// <param name="top">Cantidad máxima de fragmentos a recuperar de la base vectorial,
        /// antes de aplicar el filtro de <paramref name="scoreMinimo"/>.</param>
        /// <param name="scoreMinimo">Umbral mínimo de similitud coseno (0 a 1) para considerar
        /// un fragmento relevante. Valores típicos con text-embedding-3-small en español
        /// rondan 0.35-0.6 para contenido relacionado.</param>
        Task<List<ResultadoBusquedaRAG>> BuscarContextoRelevante(
            string pregunta, 
            int top = 3, 
            float scoreMinimo = 0.35f, 
            CancellationToken cancellationToken = default);
    }
}
