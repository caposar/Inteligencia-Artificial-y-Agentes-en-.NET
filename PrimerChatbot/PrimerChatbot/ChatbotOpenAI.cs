using OpenAI.Chat;
using System;
using System.Collections.Generic;
using System.Text;

namespace PrimerChatbot
{
    /// <summary>
    /// Chatbot usando la API de OpenAI (GPT).
    /// Registro y API Key (paga, desde $5): https://platform.openai.com
    /// Modelos disponibles: https://platform.openai.com/docs/models
    /// </summary>
    internal class ChatbotOpenAI
    {
        internal static async Task Correr()
        {
            var modelo = "gpt-5.4-nano";
            var llave = Environment.GetEnvironmentVariable("OPENAI_LLAVE");
            var cliente = new ChatClient(modelo, llave);

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

            mensajes.Add(new SystemChatMessage(systemPromptGeneral));

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
                Console.Write($"IA: ");

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
