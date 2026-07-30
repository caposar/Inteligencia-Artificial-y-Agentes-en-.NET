namespace WebAPI_IA.DTOs
{
    public class ConversacionDTO
    {
        public Guid Id { get; set; }
        public List<MensajeChatUI> Mensajes { get; set; } = [];
        public AprobacionPendienteDTO? AprobacionPendiente { get; set; }
    }
}
