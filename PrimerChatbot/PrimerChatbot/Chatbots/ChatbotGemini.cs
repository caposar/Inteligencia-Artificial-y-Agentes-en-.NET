using Google.GenAI;
using Google.GenAI.Types;
using System;
using System.Collections.Generic;
using System.Text;
using Environment = System.Environment;

namespace PrimerChatbot.Chatbots
{
    /// <summary>
    /// Chatbot usando la API de Google Gemini.
    /// Registro y API Key gratuita: https://aistudio.google.com
    /// Modelos disponibles: https://ai.google.dev/gemini-api/docs/models
    /// </summary>
    internal class ChatbotGemini
    {
        internal static async Task Correr()
        {
            string llave = Environment.GetEnvironmentVariable("GEMINI_LLAVE")!;

            var cliente = new Client(apiKey: llave);
            var modelo = "models/gemini-2.5-flash";        // ← Recomendado, el mejor gratis
            // var modelo = "models/gemini-2.5-flash-lite"; // ← Más rápido, más liviano
            // var modelo = "models/gemini-2.5-pro";        // ← Limitado: solo 50 req/día gratis

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

            // Historial de conversación
            var historial = new List<Content>();

            Console.WriteLine("IA: ¡Hola! Puedes escribir tus preguntas o presionar Enter para salir");
            Console.WriteLine();

            while (true)
            {
                Console.ForegroundColor = ConsoleColor.Blue;
                Console.Write("Tú: ");
                var entrada = Console.ReadLine();
                Console.ResetColor();

                if (string.IsNullOrWhiteSpace(entrada))
                {
                    break;
                }

                // Agregar mensaje del usuario al historial
                historial.Add(new Content
                {
                    Role = "user",
                    Parts = new List<Part> { new Part { Text = entrada } }
                });

                Console.WriteLine();
                Console.Write("IA: ");

                var sb = new StringBuilder();

                // Streaming con historial y system prompt
                await foreach (var chunk in cliente.Models.GenerateContentStreamAsync(
                    model: modelo,
                    contents: historial,
                    config: new GenerateContentConfig
                    {
                        SystemInstruction = new Content
                        {
                            Parts = new List<Part> { new Part { Text = systemPromptCsharp } }
                        }
                    }))
                {
                    var texto = chunk.Candidates?[0].Content?.Parts?[0].Text;
                    if (!string.IsNullOrEmpty(texto))
                    {
                        sb.Append(texto);
                        Console.Write(texto);
                    }
                }

                // Agregar respuesta de la IA al historial
                historial.Add(new Content
                {
                    Role = "model",
                    Parts = new List<Part> { new Part { Text = sb.ToString() } }
                });

                Console.WriteLine();
                Console.WriteLine();
            }
        }
    }

}
