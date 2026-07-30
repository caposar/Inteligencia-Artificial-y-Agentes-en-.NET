using WebAPI_IA.DTOs;

namespace WebAPI_IA.Entidades
{
    public class MensajeChat
    {
        public Guid Id { get; set; }
        public Guid ConversacionChatId { get; set; }
        public ConversacionChat Conversacion { get; set; } = null!;
        public RolMensaje Rol { get; set; }
        public string Texto { get; set; } = string.Empty;
        public int Orden { get; set; }
    }
}
