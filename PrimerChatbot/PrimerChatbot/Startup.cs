using Anthropic;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OllamaSharp;
using PrimerChatbot.Servicios;
using System;
using System.Collections.Generic;
using System.Text;

namespace PrimerChatbot
{
    internal static class Startup
    {
        public static void ConfigureServices(HostApplicationBuilder builder, string proveedor, string? modelo)
        {
            string llaveOpenAI = System.Environment.GetEnvironmentVariable("OPENAI_LLAVE")!;
            string llaveAnthropic = System.Environment.GetEnvironmentVariable("ANTHROPIC_LLAVE")!;
            string llaveGroq = System.Environment.GetEnvironmentVariable("GROQ_LLAVE")!;
            string llaveGemini = System.Environment.GetEnvironmentVariable("GEMINI_LLAVE")!;
            string llaveMistral = System.Environment.GetEnvironmentVariable("MISTRAL_LLAVE")!;
            string llaveDeepSeek = System.Environment.GetEnvironmentVariable("DEEPSEEK_LLAVE")!;
            string llaveOpenRouter = System.Environment.GetEnvironmentVariable("OPENROUTER_LLAVE") ?? "";
            string llaveGitHub = System.Environment.GetEnvironmentVariable("GITHUB_LLAVE") ?? "";

            builder.Services.AddTransient<IServicioClima, ServicioClimaOpenWeather>();
            builder.Services.AddTransient<ServicioEvaluaCondiciones>();
            builder.Logging.AddFilter("System.Net.Http.HttpClient", LogLevel.None);
            builder.Services.AddHttpClient();

            builder.Services.AddTransient<ServicioEnviarCorreoFalso>();
            builder.Services.AddTransient<ServicioObtenerCorreoFalso>();

            builder.Services.AddSingleton<IChatClient>(sp =>
            {
                // Ejemplos de uso:
                // dotnet run -- openai gpt-5.4-nano
                // dotnet run -- claude claude-haiku-4-5
                // dotnet run -- groq llama-3.3-70b-versatile
                // dotnet run -- gemini gemini-2.5-flash
                // dotnet run -- mistral mistral-small-latest
                // dotnet run -- deepseek deepseek-v4-flash
                var cliente = proveedor switch
                {
                    // OpenAI: https://platform.openai.com/docs/models
                    "openai" => new OpenAI.Chat.ChatClient(modelo ?? "gpt-5.4-nano", llaveOpenAI).AsIChatClient(),

                    // Anthropic (Claude): https://docs.anthropic.com/en/docs/about-claude/models
                    "claude" => new AnthropicClient()
                    {
                        ApiKey = llaveAnthropic
                    }.AsIChatClient().AsBuilder().ConfigureOptions(c => c.ModelId = modelo ?? "claude-haiku-4-5").Build(),

                    // Groq: compatible con OpenAI. Modelos: https://console.groq.com/docs/models
                    "groq" => new OpenAI.Chat.ChatClient(
                        modelo ?? "llama-3.3-70b-versatile",
                        new System.ClientModel.ApiKeyCredential(llaveGroq),
                        new OpenAI.OpenAIClientOptions { Endpoint = new Uri("https://api.groq.com/openai/v1/") })
                        .AsIChatClient(),

                    // Gemini: compatible con OpenAI. Modelos: https://ai.google.dev/gemini-api/docs/models
                    "gemini" => new OpenAI.Chat.ChatClient(
                        modelo ?? "gemini-2.5-flash",
                        new System.ClientModel.ApiKeyCredential(llaveGemini),
                        new OpenAI.OpenAIClientOptions { Endpoint = new Uri("https://generativelanguage.googleapis.com/v1beta/openai/") })
                        .AsIChatClient(),

                    // Mistral: compatible con OpenAI. Modelos: https://docs.mistral.ai/getting-started/models/models_overview/
                    "mistral" => new OpenAI.Chat.ChatClient(
                        modelo ?? "mistral-small-latest",
                        new System.ClientModel.ApiKeyCredential(llaveMistral),
                        new OpenAI.OpenAIClientOptions { Endpoint = new Uri("https://api.mistral.ai/v1/") })
                        .AsIChatClient(),

                    // DeepSeek: compatible con OpenAI. Modelos: https://api-docs.deepseek.com/
                    "deepseek" => new OpenAI.Chat.ChatClient(
                        modelo ?? "deepseek-v4-flash",
                        new System.ClientModel.ApiKeyCredential(llaveDeepSeek),
                        new OpenAI.OpenAIClientOptions { Endpoint = new Uri("https://api.deepseek.com/v1/") })
                        .AsIChatClient(),

                    // OpenRouter: decenas de modelos gratuitos con sufijo ":free"
                    // Registro: https://openrouter.ai
                    // Modelos gratuitos: https://openrouter.ai/models?q=free
                    "openrouter" => new OpenAI.Chat.ChatClient(
                        modelo ?? "openrouter/free",  // ← router automático de modelos gratuitos
                        new System.ClientModel.ApiKeyCredential(llaveOpenRouter),
                        new OpenAI.OpenAIClientOptions { Endpoint = new Uri("https://openrouter.ai/api/v1/") })
                        .AsIChatClient(),

                    // GitHub Models: acceso gratis a GPT-4o, Claude, Llama y más con cuenta de GitHub
                    // Registro: https://github.com/marketplace/models
                    "github" => new OpenAI.Chat.ChatClient(
                        modelo ?? "gpt-4o-mini",
                        new System.ClientModel.ApiKeyCredential(llaveGitHub),
                        new OpenAI.OpenAIClientOptions { Endpoint = new Uri("https://models.inference.ai.azure.com/") })
                        .AsIChatClient(),

                    // Ollama: Ejecución local en tu PC (requiere tener ollama ejecutándose)
                    "ollama" => new OllamaApiClient(new Uri("http://127.0.0.1:11434"), modelo ?? "qwen3.5:2b"),

                    _ => throw new ArgumentException($"Proveedor desconocido: {proveedor}. Opciones: openai, claude, groq, gemini, mistral, deepseek")
                };

                return cliente.AsBuilder()
                .ConfigureOptions(o =>
                {
                    o.MaxOutputTokens = 2000;
                    o.Temperature = 0.7f;

                    /* * Evita llamadas en paralelo forzando la ejecución secuencial.
                     * Soluciona el error al capturar aprobaciones sensibles con .FirstOrDefault()
                     * y evita que el SDK pida un permiso global por un lote de herramientas mezcladas.
                     * (comportamiento muy común en modelos como Llama 3.3 de Groq).
                     * 
                     * * TODO: Refactorizar el manejo de aprobaciones en Chatbot.cs para iterar y 
                     * procesar múltiples herramientas a la vez. Al implementar esa solución, 
                     * se debe eliminar la siguiente línea o cambiarla a 'true'.
                     */
                    o.AllowMultipleToolCalls = false;

                    o.Tools = [.. Tools.Tools.ObtenerTools(sp)];
                })
                //.Use(async (mensajes, opciones, next, cancellationToken) =>
                //{
                //    Console.WriteLine();
                //    Console.ForegroundColor = ConsoleColor.Green;
                //    Console.WriteLine("Antes de llamar al modelo...");
                //    Console.ResetColor();

                //    await next(mensajes, opciones, cancellationToken);

                //    Console.WriteLine();
                //    Console.ForegroundColor = ConsoleColor.Green;
                //    Console.WriteLine("Después de llamar al modelo...");
                //    Console.ResetColor();

                //})
                .UseFunctionInvocation(null, c =>
                {
                    c.IncludeDetailedErrors = true;
                })
                .Use(async (messages, options, next, cancellationToken) =>
                {
                    await next(messages, options, cancellationToken);
                })
                .Build(sp);
            });
        }
    }
}
