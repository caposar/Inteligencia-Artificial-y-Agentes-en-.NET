using OpenAI.Chat;
using System;
using System.Collections.Generic;
using System.Text;

namespace PrimerChatbot
{
    /// <summary>
    /// Chatbot usando la API de DeepSeek (compatible con el SDK de OpenAI).
    /// Registro y API Key: https://platform.deepseek.com (5M tokens gratis al registrarse)
    /// Modelos disponibles: https://api-docs.deepseek.com/
    /// </summary>
    internal class ChatbotDeepSeek
    {
        internal static async Task Correr()
        {
            var llave = Environment.GetEnvironmentVariable("DEEPSEEK_LLAVE");

            // DeepSeek es compatible con el SDK de OpenAI, solo cambia la URL base
            var credencial = new System.ClientModel.ApiKeyCredential(llave!);
            var opciones = new OpenAI.OpenAIClientOptions
            {
                Endpoint = new Uri("https://api.deepseek.com/v1")
            };

            // Modelos disponibles:
            // "deepseek-v4-flash"  ← recomendado, rápido y económico
            // "deepseek-v4-pro"    ← más potente, ideal para razonamiento complejo
            // NOTA: "deepseek-chat" y "deepseek-reasoner" se deprecan el 24/07/2026
            var modelo = "deepseek-v4-flash";

            var cliente = new ChatClient(modelo, credencial, opciones);

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

            mensajes.Add(new SystemChatMessage(systemPromptCsharp));

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

                mensajes.Add(new UserChatMessage(entrada));

                Console.WriteLine();
                Console.Write("IA: ");

                var stream = cliente.CompleteChatStreamingAsync(mensajes);

                await foreach (var actualizacion in stream)
                {
                    foreach (var contenido in actualizacion.ContentUpdate)
                    {
                        sb.Append(contenido.Text);
                        Console.Write(contenido.Text);
                    }
                }

                mensajes.Add(new AssistantChatMessage(sb.ToString()));

                Console.WriteLine();
                Console.WriteLine();
            }
        }
    }

}
