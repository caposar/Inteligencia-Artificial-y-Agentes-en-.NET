namespace WebAPI_IA.Entidades
{
    public class SolicitudAprobacionChat
    {
        public Guid Id { get; set; }
        public Guid ConversacionChatId { get; set; }
        public ConversacionChat Conversacion { get; set; } = null!;
        public int Orden { get; set; }
        public EstadoSolicitudAprobacionChat Estado { get; set; }
        public string NombreTool { get; set; } = string.Empty;
        public string ArgumentosJson { get; set; } = "{}";
        public string SolicitudJson { get; set; } = string.Empty;
        public DateTime FechaCreacionUtc { get; set; }
        public DateTime? FechaResolucionUtc { get; set; }
    }

    public enum EstadoSolicitudAprobacionChat
    {
        Pendiente,
        Aprobada,
        Rechazada,
        Completada
    }
}
