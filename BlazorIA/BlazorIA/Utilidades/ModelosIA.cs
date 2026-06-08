namespace BlazorIA.Utilidades
{
    public static class ModelosIA
    {
        // Claves estrictamente en minúscula para coincidir con ChatClientFactory
        private static readonly Dictionary<string, List<string>> ProveedoresYModelos = new()
        {
            ["openai"] = ["gpt-5.4-nano", "gpt-5.4"],
            ["claude"] = ["claude-haiku-4-5", "claude-sonnet-4-5"],
            ["groq"] = ["llama-3.3-70b-versatile", "llama-3.1-8b-instant"],
            ["gemini"] = ["models/gemini-2.5-flash", "models/gemini-2.5-flash-lite", "models/gemini-3.5-flash", "models/gemini-flash-latest"],
            ["mistral"] = ["mistral-large-latest", "mistral-small-latest"],
            ["deepseek"] = ["deepseek-v4-flash", "deepseek-chat", "deepseek-reasoner"],
            ["github"] = ["gpt-4o-mini", "gpt-4o"],
            ["openrouter"] = [
                "openrouter/free",
                "openrouter/owl-alpha",
                "openai/gpt-oss-120b:free",
                "openai/gpt-oss-20b:free",
                "google/gemma-4-26b-a4b-it:free",
                "google/gemma-4-31b-it:free",                
                "meta-llama/llama-3.3-70b-instruct:free",
                "meta-llama/llama-3.2-3b-instruct:free",
                "nvidia/nemotron-3.5-content-safety:free",
                "nvidia/nemotron-3-ultra-550b-a55b:free",
                "nvidia/nemotron-3-super-120b-a12b:free",
                "nvidia/nemotron-3-nano-omni-30b-a3b-reasoning:free",
                "nvidia/nemotron-3-nano-30b-a3b:free",
                "nvidia/nemotron-nano-12b-v2-vl:free",
                "nvidia/nemotron-nano-9b-v2:free",
                "poolside/laguna-xs.2:free",
                "poolside/laguna-m.1:free",
                "moonshotai/kimi-k2.6:free",
                "liquid/lfm-2.5-1.2b-thinking:free",
                "liquid/lfm-2.5-1.2b-instruct:free",
                "qwen/qwen3-next-80b-a3b-instruct:free",
                "qwen/qwen3-coder:free",
                "cognitivecomputations/dolphin-mistral-24b-venice-edition:free",
                "nousresearch/hermes-3-llama-3.1-405b:free"
            ]
        };

        public static string ObtenerProveedor(string modelo)
        {
            foreach (var kvp in ProveedoresYModelos)
            {
                if (kvp.Value.Contains(modelo, StringComparer.OrdinalIgnoreCase))
                {
                    return kvp.Key; // Ya retorna la minúscula exacta
                }
            }

            throw new ArgumentException($"Modelo no soportado: {modelo}");
        }

        public static IEnumerable<string> ObtenerProveedores() => ProveedoresYModelos.Keys;

        public static IEnumerable<string> ObtenerModelosPorProveedor(string proveedor) =>
            ProveedoresYModelos.TryGetValue(proveedor.ToLowerInvariant(), out var modelos) ? modelos : [];

        // Propiedades por defecto
        public static string ProveedorPorDefecto => "openrouter";
        public static string ModeloPorDefecto => "openrouter/free";

        // Helper visual para que el dropdown de Blazor se vea profesional
        public static string FormatearNombreProveedor(string proveedor) => proveedor switch
        {
            "openai" => "OpenAI",
            "claude" => "Anthropic (Claude)",
            "openrouter" => "OpenRouter",
            "github" => "GitHub Models",
            "deepseek" => "DeepSeek",
            "groq" => "Groq",
            "mistral" => "Mistral",
            _ => char.ToUpper(proveedor[0]) + proveedor[1..]
        };
    }
}
