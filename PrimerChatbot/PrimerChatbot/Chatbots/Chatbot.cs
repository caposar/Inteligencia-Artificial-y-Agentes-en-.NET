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

    Reglas para el uso de herramientas:
    1. Si un tool falla, lee el mensaje de la excepción para ver si puedes arreglarlo haciendo algún ajuste. Comunícale al usuario cualquier ajuste que vayas a hacer.
    2. Si el usuario RECHAZA una acción, cancela SOLO ese intento. Las nuevas peticiones son totalmente independientes: si el usuario repite una orden, DEBES invocar la herramienta nuevamente, ignorando los rechazos del pasado.
    """;

            //    var systemPromptCsharp = """
            //Eres un asistente experto en C# y .NET.
            //Debes responder en español y dando ejemplos.
            //Las respuestas deben ser en texto plano, no usar formatos como markdown.
            //""";

            mensajes.Add(new ChatMessage(role: ChatRole.System, systemPromptGeneral));

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

                mensajes.Add(new ChatMessage(role: ChatRole.User, entrada));

                Console.WriteLine();
                Console.Write($"IA: ");

                while (true)
                {
                    var updates = new List<ChatResponseUpdate>();

                    //await foreach (var responseUpdate in cliente.GetStreamingResponseAsync(mensajes))
                    //{
                    //    updates.Add(responseUpdate);

                    //    foreach (var contenido in responseUpdate.Contents)
                    //    {
                    //        if (contenido is TextContent contenidoTexto)
                    //        {
                    //            Console.Write(contenidoTexto);
                    //        }
                    //    }
                    //}

                    try
                    {
                        await foreach (var responseUpdate in cliente.GetStreamingResponseAsync(mensajes))
                        {
                            updates.Add(responseUpdate);

                            foreach (var contenido in responseUpdate.Contents)
                            {
                                if (contenido is TextContent contenidoTexto)
                                {
                                    Console.Write(contenidoTexto);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine();
                        Console.ForegroundColor = ConsoleColor.Red;
                        if (ex.Message.Contains("429"))
                        {
                            Console.WriteLine("[Aviso del Sistema]: Has alcanzado el límite de peticiones gratuitas. Espera unos 30 o 60 segundos antes de volver a preguntar.");
                        }
                        else
                        {
                            Console.WriteLine($"[Error de conexión]: {ex.Message}");
                        }
                        Console.ResetColor();

                        // Rompemos el bucle interno para que el programa no se cierre 
                        // y vuelva a pedirte un input en "Tú:"
                        break;
                    }

                    var respuesta = updates.ToChatResponse();
                    mensajes.AddMessages(respuesta);

                    var solicitudAprobacion = respuesta.Messages
                                            .SelectMany(m => m.Contents)
                                            .OfType<ToolApprovalRequestContent>()
                                            .FirstOrDefault();

                    if (solicitudAprobacion is not null)
                    {
                        Console.WriteLine();
                        Console.WriteLine();
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine("La IA desea ejecutar una acción sensible.");

                        if (solicitudAprobacion.ToolCall is FunctionCallContent functionCall)
                        {
                            Console.WriteLine($"Tool: {ConvertirNombreDeFuncion(functionCall.Name)}");

                            if (functionCall.Arguments is not null)
                            {
                                foreach (var argumento in functionCall.Arguments)
                                {
                                    Console.WriteLine($"{argumento.Key}: {argumento.Value}");
                                }
                            }
                        }

                        Console.ResetColor();
                        Console.Write("¿Deseas aprobar esta acción? (s/n): ");
                        var aprobada = Console.ReadLine()?.Trim().ToLower() == "s";
                        var respuestaAprobacion = solicitudAprobacion.CreateResponse(aprobada);

                        //mensajes.Add(new ChatMessage(ChatRole.User, [respuestaAprobacion]));
                        mensajes.Add(new ChatMessage(ChatRole.Tool, [respuestaAprobacion]));

                        Console.WriteLine();
                        Console.Write("IA: ");
                        continue;
                    }

                    Console.WriteLine();
                    Console.WriteLine();
                    break;
                }
            }
        }

        private static string ConvertirNombreDeFuncion(string nombre)
        {
            return nombre switch
            {
                "EnviarCorreo" => "Enviar correo",
                _ => nombre
            };
        }

    }
}
