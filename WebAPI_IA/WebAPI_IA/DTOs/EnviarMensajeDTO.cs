namespace WebAPI_IA.DTOs
{
    public class EnviarMensajeDTO
    {
        public Guid? ConversacionId { get; set; }
        public string Texto { get; set; } = string.Empty;
    }
}
