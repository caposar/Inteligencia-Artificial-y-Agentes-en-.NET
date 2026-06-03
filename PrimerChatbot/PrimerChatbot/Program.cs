using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PrimerChatbot;
using PrimerChatbot.Chatbots;

Utilidades.CargarVariablesDeAmbiente();

// Ejemplos:
// dotnet run -- openai gpt-5.4-nano
// dotnet run -- claude claude-haiku-4-5
// dotnet run -- groq llama-3.3-70b-versatile
// dotnet run -- gemini models/gemini-2.5-flash
// dotnet run -- mistral mistral-small-latest
// dotnet run -- deepseek deepseek-v4-flash
// NUEVOS PROVEEDORES GRATUITOS:
// dotnet run -- openrouter openrouter/free
// dotnet run -- github gpt-4o-mini
// dotnet run -- ollama llama3.2

var proveedoresPorDefecto = new Dictionary<string, string>
{
    ["openai"] = "gpt-5.4-nano",
    ["claude"] = "claude-haiku-4-5",
    ["groq"] = "llama-3.3-70b-versatile",
    ["gemini"] = "models/gemini-2.5-flash",
    ["mistral"] = "mistral-small-latest",
    ["deepseek"] = "deepseek-v4-flash",
    ["github"] = "gpt-4o-mini",
    //["ollama"] = "llama3.2", // o "qwen2.5", "phi3"

    // Opciones a través de OPENROUTER (Soportan Tools)
    ["openrouter"] = "openrouter/free",                 // IMPORTANTE: El default si solo pones "openrouter"
    //["openrouter"] = "openrouter/owl-alpha",            // Router especializado en agentes y herramientas
    //["openrouter"] = "openai/gpt-oss-120b:free",        // El gigante y más capaz
    //["openrouter"] = "openai/gpt-oss-20b:free",         // La versión más rápida y ligera
    //["openrouter"] = "google/gemma-4-31b-it:free",      // Gran contexto
    //["openrouter"] = "google/gemma-4-26b-a4b-it:free",  // Arquitectura eficiente (MoE)

    //["openrouter"] = "nvidia/nemotron-3-super-120b-a12b:free", // El modelo de NVIDIA
    //["openrouter"] = "poolside/laguna-m.1:free",        // Especializado en código
    //["openrouter"] = "moonshotai/kimi-k2.6:free"        // Multimodal y multi-agente
};

var proveedor = args.Length > 0 ? args[0].ToLowerInvariant() : "openrouter";
var modeloPorDefecto = proveedoresPorDefecto.TryGetValue(proveedor, out var m) ? m : "openrouter/free";
var modelo = args.Length > 1 ? args[1] : modeloPorDefecto;

Console.WriteLine($"Proveedor: {proveedor} | Modelo: {modelo}");

var builder = Host.CreateApplicationBuilder(args);
Startup.ConfigureServices(builder, proveedor, modelo);
var host = builder.Build();

var chatClient = host.Services.GetRequiredService<IChatClient>();
await Chatbot.Correr(chatClient);
