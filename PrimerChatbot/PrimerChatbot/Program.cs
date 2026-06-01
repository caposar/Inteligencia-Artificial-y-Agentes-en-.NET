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

var proveedoresPorDefecto = new Dictionary<string, string>
{
    ["openai"] = "gpt-5.4-nano",
    ["claude"] = "claude-haiku-4-5",
    ["groq"] = "llama-3.3-70b-versatile",
    ["gemini"] = "models/gemini-2.5-flash",
    ["mistral"] = "mistral-small-latest",
    ["deepseek"] = "deepseek-v4-flash"
};

//var proveedor = args.Length > 0 ? args[0].ToLowerInvariant() : "openai";
//var modeloPorDefecto = proveedor == "openai" ? "gpt-5.4-nano" : "claude-haiku-4-5";
//var modelo = args.Length > 1 ? args[1] : modeloPorDefecto;

//Console.WriteLine($"{proveedor}: {modelo}");

var proveedor = args.Length > 0 ? args[0].ToLowerInvariant() : "groq";
var modeloPorDefecto = proveedoresPorDefecto.TryGetValue(proveedor, out var m) ? m : "llama-3.3-70b-versatile";
var modelo = args.Length > 1 ? args[1] : modeloPorDefecto;

Console.WriteLine($"Proveedor: {proveedor} | Modelo: {modelo}");


var builder = Host.CreateApplicationBuilder(args);
Startup.ConfigureServices(builder, proveedor, modelo);
var host = builder.Build();

var chatClient = host.Services.GetRequiredService<IChatClient>();
await Chatbot.Correr(chatClient);
