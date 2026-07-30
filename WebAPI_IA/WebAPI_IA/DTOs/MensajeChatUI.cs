namespace WebAPI_IA.DTOs
{
    public class MensajeChatUI
    {
        public RolMensaje Rol { get; set; }
        public string Texto { get; set; } = string.Empty;
    }

    public enum RolMensaje
    {
        Usuario, IA, Sistema
    }
}
