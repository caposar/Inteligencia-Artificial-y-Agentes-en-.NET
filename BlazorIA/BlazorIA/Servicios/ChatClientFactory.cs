using Anthropic;
using BlazorIA.Utilidades;
using Microsoft.Extensions.AI;
using OllamaSharp;

namespace BlazorIA.Servicios
{
    public class ChatClientFactory(IConfiguration configuration, IServiceProvider sp) : IChatClientFactory
    {
        public IChatClient Crear(string modelo)
        {
            var llaveOpenAI = configuration.GetValue<string>("OPENAI_LLAVE");
            var llaveAnthropic = configuration.GetValue<string>("ANTHROPIC_LLAVE");
            var llaveGroq = configuration.GetValue<string>("GROQ_LLAVE");
            var llaveGemini = configuration.GetValue<string>("GEMINI_LLAVE");
            var llaveMistral = configuration.GetValue<string>("MISTRAL_LLAVE");
            var llaveDeepSeek = configuration.GetValue<string>("DEEPSEEK_LLAVE");
            var llaveOpenRouter = configuration.GetValue<string>("OPENROUTER_LLAVE");
            var llaveGitHub = configuration.GetValue<string>("GITHUB_LLAVE");
            var urlOllama = configuration.GetValue<string>("OLLAMA_ENDPOINT")!;

            var proveedor = ModelosIA.ObtenerProveedor(modelo);

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
                "ollama" => new OllamaApiClient(urlOllama, modelo ?? "qwen3.5:2b"),

                _ => throw new ArgumentException($"Proveedor desconocido: {proveedor}")
            };

            return cliente.AsBuilder()
            .UseFunctionInvocation(null, c =>
            {
                c.IncludeDetailedErrors = true;
            })
            .Build(sp);
        }
    }
}
