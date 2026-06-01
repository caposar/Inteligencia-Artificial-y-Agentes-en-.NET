using Microsoft.Extensions.AI;
using System;
using System.Collections.Generic;
using System.Text;

namespace PrimerChatbot.Chatbots
{
    /// <summary>
    /// Chatbot usando la API de Groq (compatible con el SDK de OpenAI).
    /// Registro y API Key gratuita: https://console.groq.com
    /// Modelos disponibles: https://console.groq.com/docs/models
    /// </summary>
    internal class ChatbotGroq
    {
        internal static async Task Correr()
        {
            var llave = Environment.GetEnvironmentVariable("GROQ_LLAVE");

            var modelo = "llama-3.3-70b-versatile"; // ✅ RECOMENDADO - el más capaz y gratuito

            // Alternativas gratuitas:
            // var modelo = "llama-3.1-8b-instant";    // más rápido, menos capaz, 14.400 req/día
            // var modelo = "openai/gpt-oss-120b";     // muy potente, reemplazó a Llama 4 Maverick

            // ❌ OBSOLETOS - no usar:
            // "gemma2-9b-it"               → deprecado en agosto 2025
            // "deepseek-r1-distill-llama-70b" → deprecado en septiembre 2025
            // "meta-llama/llama-4-maverick-17b-128e-instruct" → deprecado en febrero 2026

            // Groq es compatible con el SDK de OpenAI, solo cambia la URL base
            var credencial = new System.ClientModel.ApiKeyCredential(llave!);
            var opciones = new OpenAI.OpenAIClientOptions
            {
                Endpoint = new Uri("https://api.groq.com/openai/v1")
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
