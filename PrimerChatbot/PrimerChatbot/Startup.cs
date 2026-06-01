using Anthropic;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
                        new OpenAI.OpenAIClientOptions { Endpoint = new Uri("https://api.groq.com/openai/v1") })
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
                        new OpenAI.OpenAIClientOptions { Endpoint = new Uri("https://api.mistral.ai/v1") })
                        .AsIChatClient(),

                    // DeepSeek: compatible con OpenAI. Modelos: https://api-docs.deepseek.com/
                    "deepseek" => new OpenAI.Chat.ChatClient(
                        modelo ?? "deepseek-v4-flash",
                        new System.ClientModel.ApiKeyCredential(llaveDeepSeek),
                        new OpenAI.OpenAIClientOptions { Endpoint = new Uri("https://api.deepseek.com/v1") })
                        .AsIChatClient(),

                    _ => throw new ArgumentException($"Proveedor desconocido: {proveedor}. Opciones: openai, claude, groq, gemini, mistral, deepseek")
                };

                return cliente.AsBuilder()
                .ConfigureOptions(o =>
                {
                    o.MaxOutputTokens = 2000;
                    o.Temperature = 0.7f;
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
                .Build(sp);
            });
        }
    }
}
