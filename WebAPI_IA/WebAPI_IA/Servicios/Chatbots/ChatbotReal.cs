using Microsoft.Extensions.AI;
using WebAPI_IA.DTOs;

namespace WebAPI_IA.Servicios.Chatbots
{
    public class ChatbotReal
    {
        private readonly IChatClientFactory chatClientFactory;
        private readonly ChatOptions chatOptions;
        private readonly List<ChatMessage> mensajes = [];

        public List<MensajeChatUI> Conversacion { get; } = [];
        public List<ToolApprovalRequestContent> SolicitudesAprobacionGeneradas { get; } = [];

        public ChatbotReal(
            IChatClientFactory chatClientFactory,
        ChatOptions chatOptions,
        IEnumerable<MensajeChatUI>? historial = null
            )
        {
            this.chatClientFactory = chatClientFactory;
            this.chatOptions = chatOptions;

            var systemPromptGeneral = """
        Eres un asistente que responde preguntas generales.
        Debes responder en español.
        Las respuestas deben ser en texto plano, no usar formatos como markdown.
        Las respuestas deben ser concisas a menos que te indiquen lo contrario.

        Si un tool falla, lee el mensaje de la excepción para ver si puedes arreglarlo haciendo algún ajuste. Comunícale al usuario cualquier ajuste que vayas a hacer.
        """;

            mensajes.Add(new ChatMessage(ChatRole.System, systemPromptGeneral));

            if (historial is null)
            {
                return;
            }

            Conversacion = historial.ToList();

            foreach (var mensaje in historial)
            {
                if (mensaje.Rol == RolMensaje.Usuario)
                {
                    mensajes.Add(new ChatMessage(ChatRole.User, mensaje.Texto));
                }
                else if (mensaje.Rol == RolMensaje.IA)
                {
                    mensajes.Add(new ChatMessage(ChatRole.Assistant, mensaje.Texto));
                }

            }
        }

        public async Task EnviarMensajeStreamAsync(string textoUsuario,
            Func<string, Task>? onChange,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(textoUsuario))
            {
                return;
            }

            try
            {
                Conversacion.Add(new MensajeChatUI
                {
                    Rol = RolMensaje.Usuario,
                    Texto = textoUsuario
                });

                mensajes.Add(new ChatMessage(ChatRole.User, textoUsuario));

                Conversacion.Add(new MensajeChatUI
                {
                    Rol = RolMensaje.IA,
                    Texto = string.Empty
                });

                await ProcesarRespuesta(cancellationToken, onChange);

            }
            catch (OperationCanceledException)
            {
                ManejarOperacionCancelada();
                throw;
            }
        }

        public async Task ResponderSolicitudesDeAprobacion(
                IEnumerable<ToolApprovalRequestContent> solicitudesAprobacion,
                IEnumerable<ToolApprovalResponseContent> respuestasAprobacion,
                CancellationToken cancellationToken = default
            )
        {
            var solicitudes = solicitudesAprobacion.Cast<AIContent>().ToList();
            var respuestas = respuestasAprobacion.Cast<AIContent>().ToList();

            try
            {
                mensajes.Add(new ChatMessage(ChatRole.Assistant, solicitudes));
                mensajes.Add(new ChatMessage(ChatRole.User, respuestas));

                Conversacion.Add(new MensajeChatUI
                {
                    Rol = RolMensaje.IA,
                    Texto = string.Empty
                });

                await ProcesarRespuesta(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                ManejarOperacionCancelada();
                throw;

            }
        }

        private async Task ProcesarRespuesta(CancellationToken cancellationToken,
                    Func<string, Task>? onDelta = null)
        {
            var updates = new List<ChatResponseUpdate>();
            var cliente = chatClientFactory.Crear();

            await foreach (var update in cliente.GetStreamingResponseAsync(mensajes, chatOptions,
                           cancellationToken: cancellationToken))
            {
                updates.Add(update);

                foreach (var content in update.Contents)
                {
                    if (content is TextContent textContent)
                    {
                        Conversacion[^1].Texto += textContent.Text;
                        if (onDelta is not null)
                        {
                            await onDelta(textContent.Text);
                        }
                    }
                }
            }

            var respuesta = updates.ToChatResponse();
            mensajes.AddMessages(respuesta);

            var solicitudesPendientes = respuesta.Messages
                                        .SelectMany(m => m.Contents)
                                        .OfType<ToolApprovalRequestContent>().ToList();

            SolicitudesAprobacionGeneradas.AddRange(solicitudesPendientes);

            if (string.IsNullOrWhiteSpace(Conversacion[^1].Texto))
            {
                Conversacion.RemoveAt(Conversacion.Count - 1);
            }
        }

        private void ManejarOperacionCancelada()
        {
            if (Conversacion.Count > 0 && Conversacion[^1].Rol == RolMensaje.IA)
            {
                if (string.IsNullOrWhiteSpace(Conversacion[^1].Texto))
                {
                    Conversacion[^1].Texto = "[Respuesta cancelada]";
                }
                else
                {
                    Conversacion[^1].Texto += " [cancelado]";
                }
            }
        }

        public static string ConvertirNombreDeFuncion(string nombre)
        {
            return nombre switch
            {
                "EnviarCorreo" => "Enviar correo",
                _ => nombre
            };
        }

    }
}
