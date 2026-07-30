using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using System.Security.Claims;
using System.Text.Json;
using WebAPI_IA.Data;
using WebAPI_IA.DTOs;
using WebAPI_IA.Entidades;
using WebAPI_IA.Servicios.Chatbots;
using WebAPI_IA.Utilidades;

namespace WebAPI_IA.Controllers
{
    [ApiController]
    [Route("api/chat")]
    [Authorize]
    public class ChatController(IChatClientFactory chatClientFactory,
        ChatOptions chatOptions, ApplicationDbContext context) : ControllerBase
    {

        [HttpGet("ObtenerConversaciones")]
        public async Task<ActionResult<List<ConversacionResumenDTO>>> ObtenerConversaciones()
        {
            var usuarioId = ObtenerUsuarioId();

            if (usuarioId is null)
            {
                return Unauthorized();
            }

            var conversaciones = await context.Conversaciones
            .Where(x => x.UsuarioId == usuarioId)
            .OrderByDescending(x => x.FechaActualizacionUtc)
            .Select(x => new ConversacionResumenDTO
            {
                Id = x.Id,
                Titulo = x.Titulo
            })
            .ToListAsync();

            return conversaciones;

        }

        [HttpGet("ObtenerConversacion")]
        public async Task<ActionResult<ConversacionDTO>> ObtenerConversacion(Guid id)
        {
            var usuarioId = ObtenerUsuarioId();

            if (usuarioId is null)
            {
                return Forbid();
            }

            var conversacionDB = await context.Conversaciones
            .Include(x => x.Mensajes.OrderBy(x => x.Orden))
            .FirstOrDefaultAsync(x => x.Id == id && x.UsuarioId == usuarioId);

            if (conversacionDB is null)
            {
                return NotFound();
            }

            var aprobacionPendiente = await ObtenerAprobacionPendiente(id, usuarioId);

            var conversacion = new ConversacionDTO
            {
                Id = id,
                Mensajes = conversacionDB.Mensajes
                .Select(x => new MensajeChatUI { Rol = x.Rol, Texto = x.Texto })
                .ToList(),
                AprobacionPendiente = aprobacionPendiente?.ADTO()
            };

            return conversacion;

        }

        [HttpPost("NuevoChat")]
        public async Task<IActionResult> NuevoChat()
        {
            var usuarioId = ObtenerUsuarioId();

            if (usuarioId is null)
            {
                return Unauthorized();
            }

            var id = Guid.NewGuid();

            var ahora = DateTime.UtcNow;

            context.Conversaciones.Add(new ConversacionChat
            {
                Id = id,
                Titulo = "Nuevo chat",
                UsuarioId = usuarioId,
                FechaCreacionUtc = ahora,
                FechaActualizacionUtc = ahora
            });

            await context.SaveChangesAsync();

            return Ok(new { id });

        }

        [HttpPost("Enviar")]
        public async Task Enviar([FromBody] EnviarMensajeDTO dto)
        {
            var usuarioId = ObtenerUsuarioId();

            if (usuarioId is null)
            {
                Response.StatusCode = StatusCodes.Status401Unauthorized;
                await EscribirEventoStream(EventoStream.error, new { mensaje = "Debes iniciar sesion." });
                return;
            }

            if (dto.ConversacionId is null)
            {
                Response.StatusCode = StatusCodes.Status401Unauthorized;
                await EscribirEventoStream(EventoStream.error, new { mensaje = "Debe enviar el Id de la conversación" });
                return;
            }

            var id = dto.ConversacionId.Value;

            var conversacionDBExiste = await context.Conversaciones
                                    .AnyAsync(x => x.Id == id && x.UsuarioId == usuarioId);

            if (!conversacionDBExiste)
            {
                Response.StatusCode = StatusCodes.Status404NotFound;
                await EscribirEventoStream(EventoStream.error, new { mensaje = "La conversacion no existe." });
                return;
            }

            Response.ContentType = "application/x-ndjson";
            Response.Headers.CacheControl = "no-cache";

            var chatbot = await CrearChatbot(id, usuarioId);

            await EscribirEventoStream(EventoStream.iniciar_conversacion, new { id });

            try
            {
                await chatbot.EnviarMensajeStreamAsync(dto.Texto, async delta =>
                {
                    await EscribirEventoStream(EventoStream.delta, new { texto = delta });
                }, HttpContext.RequestAborted);

                await GuardarMensajes(id, usuarioId, chatbot.Conversacion);

                if (chatbot.SolicitudesAprobacionGeneradas.Any())
                {
                    await PersistirSolicitudesAprobacion(id, chatbot.SolicitudesAprobacionGeneradas);
                    var aprobacionPendiente = await ObtenerAprobacionPendiente(id, usuarioId);
                    var aprobacionPendienteDTO = aprobacionPendiente!.ADTO();

                    await EscribirEventoStream(EventoStream.aprobacion_requerida, new
                    {
                        nombreTool = aprobacionPendienteDTO.NombreTool,
                        argumentos = aprobacionPendienteDTO.Argumentos
                    });
                }
            }
            catch (OperationCanceledException)
            {
                await GuardarMensajes(id, usuarioId, chatbot.Conversacion);
            }
        }

        [HttpPost("ResolverAprobacion")]
        public async Task<IActionResult> ResolverAprobacion([FromBody] ResolverAprobacionDTO dto)
        {
            var usuarioId = ObtenerUsuarioId();

            if (usuarioId is null)
            {
                return Unauthorized();
            }

            var conversacionDB = await context.Conversaciones
                .Include(x => x.Mensajes.OrderBy(x => x.Orden))
                .FirstOrDefaultAsync(x => x.Id == dto.ConversacionId && x.UsuarioId == usuarioId);

            if (conversacionDB is null)
            {
                return BadRequest("La conversación no existe");
            }

            var solicitud = await ObtenerAprobacionPendiente(dto.ConversacionId, usuarioId);

            if (solicitud is null)
            {
                return BadRequest("La conversación no tiene una aprobacion pendiente activa.");
            }

            solicitud.Estado = dto.Aprobado
                            ? EstadoSolicitudAprobacionChat.Aprobada
                            : EstadoSolicitudAprobacionChat.Rechazada;

            solicitud.FechaResolucionUtc = DateTime.UtcNow;
            await context.SaveChangesAsync();

            var mensajesChatUI = conversacionDB.Mensajes
                .Select(x => new MensajeChatUI { Rol = x.Rol, Texto = x.Texto })
                .ToList();

            mensajesChatUI.Add(new MensajeChatUI
            {
                Rol = RolMensaje.Sistema,
                Texto = dto.Aprobado ? "Acción aprobada por el usuario" : "Acción rechazada por el usuario"
            });

            await GuardarMensajes(dto.ConversacionId, usuarioId, mensajesChatUI);

            var solicitudSiguiente = await ObtenerAprobacionPendiente(dto.ConversacionId, usuarioId);

            if (solicitudSiguiente is not null)
            {
                return Ok();
            }

            var loteResuelto = await context.SolicitudesAprobacion
                            .Where(x =>
                                x.ConversacionChatId == dto.ConversacionId &&
                                x.Conversacion.UsuarioId == usuarioId &&
                                (x.Estado == EstadoSolicitudAprobacionChat.Aprobada ||
                                    x.Estado == EstadoSolicitudAprobacionChat.Rechazada))
                            .OrderBy(x => x.Orden)
                            .ToListAsync();

            var solicitudes = loteResuelto.Select(x =>
            JsonSerializer
            .Deserialize<ToolApprovalRequestContent>(x.SolicitudJson, AIJsonUtilities.DefaultOptions))
            .ToList();

            var respuestas = new List<ToolApprovalResponseContent>();

            for (int i = 0; i < solicitudes.Count; i++)
            {
                var solicitudActual = solicitudes[i];
                var loteResueltoActual = loteResuelto[i];
                var respuesta = solicitudActual!
                    .CreateResponse(loteResueltoActual.Estado == EstadoSolicitudAprobacionChat.Aprobada,
                        null);
                respuestas.Add(respuesta);
            }

            var chatbot = await CrearChatbot(dto.ConversacionId, usuarioId);
            await chatbot.ResponderSolicitudesDeAprobacion(solicitudes!, respuestas, HttpContext.RequestAborted);

            foreach (var item in loteResuelto)
            {
                item.Estado = EstadoSolicitudAprobacionChat.Completada;
            }

            await context.SaveChangesAsync();
            await GuardarMensajes(dto.ConversacionId, usuarioId, chatbot.Conversacion);
            await PersistirSolicitudesAprobacion(dto.ConversacionId, chatbot.SolicitudesAprobacionGeneradas);

            return Ok();
        }

        //[HttpPost("BorrarConversacion")]
        //public async Task<IActionResult> BorrarConversacion([FromBody] Guid id)
        //{
        //    var usuarioId = ObtenerUsuarioId();

        //    if (usuarioId is null)
        //    {
        //        return Unauthorized();
        //    }

        //    var conversacionesBorradas = await context.Conversaciones
        //                            .Where(x => x.Id == id).ExecuteDeleteAsync();

        //    if (conversacionesBorradas == 0)
        //    {
        //        return NotFound();
        //    }

        //    return NoContent();
        //}

        [HttpDelete("BorrarConversacion/{id}")]
        public async Task<IActionResult> BorrarConversacion(Guid id)
        {
            var usuarioId = ObtenerUsuarioId();

            if (usuarioId is null)
            {
                return Unauthorized();
            }

            // Nota: Se agrega la validación del usuario
            // para evitar que alguien borre conversaciones de otros.
            var conversacionesBorradas = await context.Conversaciones
                                    .Where(x => x.Id == id && x.UsuarioId == usuarioId).ExecuteDeleteAsync();

            if (conversacionesBorradas == 0)
            {
                return NotFound();
            }

            return NoContent();
        }

        private async Task GuardarMensajes(Guid conversacionId, string usuarioId,
            List<MensajeChatUI> mensajes)
        {
            var conversacion = await context.Conversaciones
                .Include(x => x.Mensajes)
                .FirstOrDefaultAsync(x => x.Id == conversacionId && x.UsuarioId == usuarioId);

            if (conversacion is null)
            {
                return;
            }

            var mensajesPersistidos = conversacion.Mensajes
                .OrderBy(x => x.Orden)
                .ToList();

            for (var indice = mensajesPersistidos.Count; indice < mensajes.Count; indice++)
            {
                var mensaje = mensajes[indice];

                context.Mensajes.Add(new MensajeChat
                {
                    Id = Guid.NewGuid(),
                    ConversacionChatId = conversacionId,
                    Rol = mensaje.Rol,
                    Texto = mensaje.Texto,
                    Orden = indice,
                });
            }

            conversacion.FechaActualizacionUtc = DateTime.UtcNow;
            conversacion.Titulo = ObtenerTitulo(conversacion.Titulo, mensajes);
            await context.SaveChangesAsync();

        }

        private async Task<SolicitudAprobacionChat?> ObtenerAprobacionPendiente(Guid conversacionId,
                                                                    string usuarioId)
        {
            return await context.SolicitudesAprobacion
                    .Where(x =>
                        x.ConversacionChatId == conversacionId &&
                        x.Conversacion.UsuarioId == usuarioId &&
                        x.Estado == EstadoSolicitudAprobacionChat.Pendiente)
                    .OrderBy(x => x.Orden)
                    .FirstOrDefaultAsync();
        }

        private async Task PersistirSolicitudesAprobacion(
        Guid conversacionId,
        List<ToolApprovalRequestContent> solicitudes)
        {
            var orden = -1;

            var existenAprobaciones = await context
                        .SolicitudesAprobacion.AnyAsync(x => x.ConversacionChatId == conversacionId);

            if (existenAprobaciones)
            {
                orden = await context.SolicitudesAprobacion
                        .Where(x => x.ConversacionChatId == conversacionId)
                        .Select(x => x.Orden)
                        .MaxAsync();
            }

            var ahora = DateTime.UtcNow;

            foreach (var solicitud in solicitudes)
            {
                orden++;

                if (solicitud.ToolCall is not FunctionCallContent functionCall)
                {
                    continue;
                }

                context.SolicitudesAprobacion.Add(new SolicitudAprobacionChat
                {
                    Id = Guid.NewGuid(),
                    ConversacionChatId = conversacionId,
                    Orden = orden,
                    Estado = EstadoSolicitudAprobacionChat.Pendiente,
                    NombreTool = ChatbotReal.ConvertirNombreDeFuncion(functionCall.Name),
                    ArgumentosJson = JsonSerializer.Serialize(functionCall.Arguments, AIJsonUtilities.DefaultOptions),
                    SolicitudJson = JsonSerializer.Serialize(solicitud, AIJsonUtilities.DefaultOptions),
                    FechaCreacionUtc = ahora
                });

            }

            await context.SaveChangesAsync();
        }


        private async Task EscribirEventoStream(EventoStream tipo, object datos)
        {
            var evento = JsonSerializer.Serialize(new
            {
                tipo = tipo.ToString(),
                datos
            });

            await Response.WriteAsync(evento + "\n");
            await Response.Body.FlushAsync();
        }

        private async Task<ChatbotReal> CrearChatbot(Guid conversacionId, string usuarioId)
        {
            var mensajes = await context.Mensajes
               .Where(x => x.ConversacionChatId == conversacionId && x.Conversacion.UsuarioId == usuarioId)
               .OrderBy(x => x.Orden)
               .ToListAsync();

            var mensajesConversacionUI = mensajes
                        .Select(x => new MensajeChatUI { Rol = x.Rol, Texto = x.Texto });

            return new ChatbotReal(chatClientFactory, chatOptions, mensajesConversacionUI);
        }

        private string? ObtenerUsuarioId()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier);
        }

        private static string ObtenerTitulo(string tituloActual, IEnumerable<MensajeChatUI> mensajes)
        {
            if (tituloActual != "Nuevo chat")
            {
                return tituloActual;
            }

            var primerMensajeUsuario = mensajes.FirstOrDefault(x => x.Rol == RolMensaje.Usuario)?.Texto.Trim();


            if (string.IsNullOrWhiteSpace(primerMensajeUsuario))
            {
                return tituloActual;
            }

            return primerMensajeUsuario.Length <= 60
                        ? primerMensajeUsuario
                        : primerMensajeUsuario[..60] + "...";
        }

    }
}
