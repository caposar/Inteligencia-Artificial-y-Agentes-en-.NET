using Microsoft.Extensions.AI;
using System;
using System.Collections.Generic;
using System.Text;

namespace PrimerChatbot.Chatbots
{
    internal class Chatbot
    {
        internal static async Task Correr(IChatClient cliente)
        {
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
                Console.Write($"IA: ");

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
