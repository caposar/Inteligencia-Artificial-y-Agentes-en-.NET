using Microsoft.AspNetCore.Identity;
using WebAPI_IA.DTOs;

namespace WebAPI_IA.Entidades
{
    public class ConversacionChat
    {
        public Guid Id { get; set; }
        public string Titulo { get; set; } = "Nuevo chat";
        public string? UsuarioId { get; set; }
        public IdentityUser? Usuario { get; set; }
        public DateTime FechaCreacionUtc { get; set; }
        public DateTime FechaActualizacionUtc { get; set; }
        public List<MensajeChat> Mensajes { get; set; } = [];
        public List<SolicitudAprobacionChat> SolicitudesAprobacion { get; set; } = [];
    }
}
