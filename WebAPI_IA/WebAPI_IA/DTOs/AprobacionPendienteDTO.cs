namespace WebAPI_IA.DTOs
{
    public class AprobacionPendienteDTO
    {
        public string NombreTool { get; set; } = string.Empty;
        public Dictionary<string, object?> Argumentos { get; set; } = [];
    }
}
