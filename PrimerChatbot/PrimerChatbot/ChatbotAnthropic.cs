using Anthropic;
using Anthropic.Models.Messages;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace PrimerChatbot
{
    /// <summary>
    /// Chatbot usando la API de Anthropic (Claude).
    /// Registro y API Key (paga, desde $5): https://console.anthropic.com
    /// Modelos disponibles: https://docs.anthropic.com/en/docs/about-claude/models
    /// </summary>
    internal class ChatbotAnthropic
    {
        internal static async Task Correr()
        {
            string llave = Environment.GetEnvironmentVariable("ANTHROPIC_LLAVE")!;
            var cliente = new AnthropicClient
            {
                ApiKey = llave
            };

            var modelo = "claude-haiku-4-5";

            Console.WriteLine("IA: ¡Hola! Puedes escribir tus preguntas o presionar Enter para salir");
            Console.WriteLine();

            var mensajes = new List<MessageParam>();

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

                mensajes.Add(new MessageParam
                {
                    Role = Role.User,
                    Content = entrada
                });

                Console.WriteLine();
                Console.Write("IA: ");

                var parametros = new MessageCreateParams
                {
                    Model = modelo,
                    MaxTokens = 1024,
                    System = systemPromptCsharp,
                    Messages = mensajes
                };

                await foreach (var actualizacion in cliente.Messages.CreateStreaming(parametros))
                {
                    var texto = ExtraerTextoDelta(actualizacion);

                    if (!string.IsNullOrEmpty(texto))
                    {
                        sb.Append(texto);
                        Console.Write(texto);
                    }
                }

                mensajes.Add(new MessageParam
                {
                    Role = Role.Assistant,
                    Content = sb.ToString()
                });

                Console.WriteLine();
                Console.WriteLine();
            }

        }

        private static string? ExtraerTextoDelta(object? actualizacion)
        {
            var json = actualizacion?.ToString();

            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (!root.TryGetProperty("type", out var typeProp) ||
                    typeProp.GetString() != "content_block_delta")
                {
                    return null;
                }

                if (!root.TryGetProperty("delta", out var deltaProp))
                {
                    return null;
                }

                if (!deltaProp.TryGetProperty("type", out var deltaTypeProp) ||
                    deltaTypeProp.GetString() != "text_delta")
                {
                    return null;
                }

                if (!deltaProp.TryGetProperty("text", out var textProp))
                {
                    return null;
                }

                return textProp.GetString();
            }
            catch (JsonException)
            {
                return null;
            }
        }

    }
}
