using BlazorIA.DTOs;
using BlazorIA.RAG.Modelos;
using BlazorIA.RAG.Servicios;
using BlazorIA.Servicios;
using BlazorIA.Servicios.Chatbots;
using BlazorIA.Utilidades;
using Microsoft.Extensions.AI;
using System.Text;
using System.Text.Json;

namespace BlazorIA.RAG.Chatbots
{
    public class ChatbotRag : IChatbot
    {
        private string modelo;
        private readonly IChatClientFactory chatClientFactory;
        private readonly ChatOptions chatOptions;
        private readonly IServicioRag servicioRag;
        private readonly List<ChatMessage> mensajes = [];
        private readonly Queue<ToolApprovalRequestContent> aprobacionesPendientes = new();
        private CancellationTokenSource? _ctsActual;

        public List<MensajeChatUI> Conversacion { get; } = [];
        public bool EstaProcesando { get; private set; }
        public event Action? OnChange;
        public SolicitudAprobacionUI? AprobacionPendiente { get; private set; }

        public ChatbotRag(IChatClientFactory chatClientFactory, ChatOptions chatOptions, IServicioRag servicioRag)
        {
            modelo = ModelosIA.ObtenerModeloPorDefecto;
            this.chatClientFactory = chatClientFactory;
            this.chatOptions = chatOptions;
            this.servicioRag = servicioRag;
            var systemPromptGeneral = """
            Eres un asistente especializado exclusivamente en responder preguntas usando el contexto recuperado de documentos internos.

            Debes responder en español.
            Las respuestas deben ser en texto plano, sin markdown.

            Reglas obligatorias:
            - Responde únicamente con información contenida en el contexto recuperado.
            - Si la respuesta no está explícitamente en el contexto, debes responder: "No tengo información suficiente en los documentos para responder esa pregunta."
            - No uses conocimiento general del modelo.
            - No inventes información.
            - No respondas preguntas de programación, cultura general, matemáticas u otros temas si no aparecen en el contexto recuperado.
            - Si la pregunta no está relacionada con los documentos, recházala de forma breve.
            """;

            mensajes.Add(new ChatMessage(ChatRole.System, systemPromptGeneral));
        }

        public void CancelarRespuestaActual()
        {
            if (EstaProcesando)
            {
                _ctsActual?.Cancel();
            }
        }

        public async Task EnviarMensajeAsync(string textoUsuario, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(textoUsuario))
            {
                return;
            }

            if (EstaProcesando || AprobacionPendiente is not null)
            {
                return;
            }

            try
            {
                EstaProcesando = true;
                _ctsActual = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

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

                NotificarCambio();
                await ProcesarRespuesta(textoUsuario, _ctsActual.Token);
            }
            catch (OperationCanceledException)
            {
                ManejarOperacionCancelada();
            }
            finally
            {
                ManejarFinally();
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

        private void ManejarFinally()
        {
            _ctsActual?.Dispose();
            _ctsActual = null;
            EstaProcesando = false;
            NotificarCambio();
        }


        private async Task ProcesarRespuesta(string textoUsuario, CancellationToken cancellationToken)
        {
            var contexto = await servicioRag.BuscarContextoRelevante(textoUsuario, top: 3, scoreMinimo: 0.35f, cancellationToken);

            if (!contexto.Any())
            {
                Conversacion[^1].Texto = "No tengo información suficiente en los documentos para responder esa pregunta.";
                NotificarCambio();
                return;
            }

            var delimitadorFuentes = "|";

            /*
             Contexto recuperado de la base documental:

            Documento: documento 1
            Contenido: el contenido

            -------

            Documento: documento 2
            Contenido: el contenido del doc 2
             */
            var mensajeContexto = new ChatMessage(ChatRole.System,
                $$"""
                Contexto recuperado de la base documental:
                {{string.Join("\n\n---\n\n", contexto)}}

                Pregunta del usuario:
                {{textoUsuario}}

                Instrucción:
                - Responde solo si la respuesta está explícitamente respaldada por el contexto recuperado.
                - Si no lo está, responde exactamente:
                    "No tengo información suficiente en los documentos para responder esa pregunta."
                - Primero escribe solamente la respuesta para el usuario, en texto plano.
                - Luego escribe en una nueva línea exactamente:
                    {{delimitadorFuentes}}
                - Después del delimitador, escribe un JSON válido con este formato:
                    {"fuentesUsadas":["Documento-1", "Documento-2"]}
                - Por ejemplo: El nombre del documento se encuentra así "manual-de-politicas-internas.md" donde manual-de-politicas-internas.md sería el título que debes colocar en fuentesUsadas.
                - En "fuentesUsadas" incluye solamente los títulos de documento de las fuentes realmente utilizadas.
                - No incluyas fuentes irrelevantes.

                """);

            var mensajesParaEnviar = new List<ChatMessage>();
            mensajesParaEnviar.AddRange(mensajes);
            mensajesParaEnviar.Insert(mensajes.Count - 1, mensajeContexto);

            var updates = new List<ChatResponseUpdate>();

            var cliente = chatClientFactory.Crear(modelo);
            var sbFuentes = new StringBuilder();
            var delimitadorEncontrado = false;

            await foreach (var update in cliente.GetStreamingResponseAsync(mensajesParaEnviar, chatOptions,
                                            cancellationToken: cancellationToken))
            {
                updates.Add(update);

                foreach (var content in update.Contents)
                {
                    if (content is TextContent textContent)
                    {
                        if (textContent.Text.Contains(delimitadorFuentes) || delimitadorEncontrado)
                        {
                            sbFuentes.Append(textContent.Text);
                            delimitadorEncontrado = true;
                            continue;
                        }
                        else
                        {
                            Conversacion[^1].Texto += textContent.Text;
                            NotificarCambio();
                        }
                    }
                }
            }

            var contenidoFuentes = sbFuentes.ToString().Trim().Replace(delimitadorFuentes, "")
                                    .Replace("\r\n", "")
                                    .Replace("\n", "")
                                    .Replace("\r", "");

            var metadata = JsonSerializer.Deserialize<MetadataFuentes>(contenidoFuentes)!;


            Conversacion[^1].ArchivosCitados = metadata.FuentesUsadas.Select(nombreArchivo =>
            new ArchivoCitado
            {
                NombreArchivo = nombreArchivo
            }).ToList();

            var respuesta = updates.ToChatResponse();
            mensajes.AddMessages(respuesta);

            var solicitudesAprobacion = respuesta.Messages
            .SelectMany(m => m.Contents)
            .OfType<ToolApprovalRequestContent>()
            .ToList();

            if (solicitudesAprobacion.Count > 0)
            {
                foreach (var solicitud in solicitudesAprobacion)
                {
                    aprobacionesPendientes.Enqueue(solicitud);
                }

                // Removemos el mensaje vacío de la IA.
                if (string.IsNullOrWhiteSpace(Conversacion[^1].Texto))
                {
                    Conversacion.RemoveAt(Conversacion.Count - 1);
                }

                MostrarSiguienteAprobacionPendiente();
                NotificarCambio();
                return;
            }
        }

        private void MostrarSiguienteAprobacionPendiente()
        {
            if (aprobacionesPendientes.Count == 0)
            {
                AprobacionPendiente = null;
                return;
            }

            var solicitudAprobacion = aprobacionesPendientes.Dequeue();

            if (solicitudAprobacion.ToolCall is FunctionCallContent functionCall)
            {
                AprobacionPendiente = new SolicitudAprobacionUI
                {
                    SolicitudAprobacion = solicitudAprobacion,
                    NombreTool = ConvertirNombreDeFuncion(functionCall.Name),
                    Argumentos = functionCall.Arguments?.ToDictionary(x => x.Key, x => x.Value) ?? []
                };
            }

        }

        private void NotificarCambio() => OnChange?.Invoke();

        public Task ResolverAprobacionAsync(bool aprobada, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        private static string ConvertirNombreDeFuncion(string nombre)
        {
            return nombre switch
            {
                "EnviarCorreo" => "Enviar correo",
                _ => nombre
            };
        }

        public void SetearModelo(string modelo)
        {
            this.modelo = modelo;
        }
    }
}
