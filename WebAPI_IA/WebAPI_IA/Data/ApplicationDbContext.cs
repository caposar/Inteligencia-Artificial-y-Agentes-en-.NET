using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WebAPI_IA.Entidades;

namespace WebAPI_IA.Data
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext(options)
    {
        public DbSet<ConversacionChat> Conversaciones { get; set; }
        public DbSet<MensajeChat> Mensajes { get; set; }
        public DbSet<SolicitudAprobacionChat> SolicitudesAprobacion { get; set; }
    }
}
