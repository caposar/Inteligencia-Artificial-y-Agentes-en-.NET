using Microsoft.Extensions.AI;
using System;
using System.Collections.Generic;
using System.Text;

namespace PrimerChatbot.Chatbots
{
    /// <summary>
    /// Chatbot usando la API de Mistral AI (compatible con el SDK de OpenAI).
    /// Registro y API Key gratuita: https://console.mistral.ai (requiere verificar teléfono, sin tarjeta)
    /// Modelos disponibles: https://docs.mistral.ai/getting-started/models/models_overview/
    /// </summary>
    internal class ChatbotMistral
    {
        internal static async Task Correr()
        {
            var llave = Environment.GetEnvironmentVariable("MISTRAL_LLAVE");

            // Modelos disponibles en el free tier:
            // "mistral-small-latest"  ← recomendado, equilibrio entre velocidad y capacidad
            // "open-mistral-nemo"     ← más liviano, 128K contexto, ideal para tareas simples
            // "codestral-latest"      ← especializado en código
            var modelo = "mistral-small-latest";

            // Mistral es compatible con el SDK de OpenAI, solo cambia la URL base
            var credencial = new System.ClientModel.ApiKeyCredential(llave!);
            var opciones = new OpenAI.OpenAIClientOptions
            {
                Endpoint = new Uri("https://api.mistral.ai/v1")
            };

            var cliente = new OpenAI.Chat.ChatClient(modelo, credencial, opciones).AsIChatClient();

            Console.WriteLine("IA: ¡Hola! Puedes escribir tus preguntas o presionar Enter para salir");
            Console.WriteLine();

            var mensajes = new List<ChatMessage>();

            var systemPromptGeneral = """
    Eres un asistente que responde preguntas generales.
    Debes responder en español.
    Las respuestas deben ser en texto plano, no usar formatos como markdown.
    """;

            var systemPromptCsharp = """
    Eres un asistente experto en C# y .NET.
    Debes responder en español y dando ejemplos.
    Las respuestas deben ser en texto plano, no usar formatos como markdown.
    """;

            var systemPromptPython = """
    Eres un asistente experto en Python.
    Debes responder en español y dando ejemplos.
    Las respuestas deben ser en texto plano, no usar formatos como markdown.
    """;

            mensajes.Add(new ChatMessage(role: ChatRole.System, systemPromptCsharp));

            while (true)
            {
                var sb = new StringBuilder();
                Console.ForegroundColor = ConsoleColor.Blue;
                Console.Write("Tú: ");
                var entrada = Console.ReadLine();
                Console.ResetColor();

                if (string.IsNullOrWhiteSpace(entrada))
                {
                    break;
                }

                mensajes.Add(new ChatMessage(role: ChatRole.User, entrada));

                Console.WriteLine();
                Console.Write("IA: ");

                await foreach (var fragmento in cliente.GetStreamingResponseAsync(mensajes))
                {
                    sb.Append(fragmento);
                    Console.Write(fragmento);
                }

                mensajes.Add(new ChatMessage(role: ChatRole.Assistant, sb.ToString()));

                Console.WriteLine();
                Console.WriteLine();
            }
        }
    }

}
