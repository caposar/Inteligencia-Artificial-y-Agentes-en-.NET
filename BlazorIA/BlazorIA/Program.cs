using BlazorIA.Components;
using BlazorIA.Datos;
using BlazorIA.RAG.Chatbots;
using BlazorIA.RAG.Servicios;
using BlazorIA.Servicios;
using BlazorIA.Servicios.Chatbots;
using BlazorIA.Utilidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
// Necesario solo si se activa la "Opción A" (RAG en memoria) más abajo.
using Microsoft.SemanticKernel.Connectors.InMemory;
using Npgsql;
using OpenAI.Embeddings;

var builder = WebApplication.CreateBuilder(args);

// ===================== INFRAESTRUCTURA BASE =====================

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Base de datos local (SQLite) para entidades propias de la app (ej. Persona).
// No tiene relación con el almacenamiento vectorial de Supabase.
builder.Services.AddDbContextFactory<ApplicationDbContext>(opciones =>
    opciones.UseSqlite("Data Source=midb.db"));

builder.Services.AddScoped<IServicioPersonas, ServicioPersonas>();

// ===================== CHATBOTS =====================

// Dos chatbots registrados con claves distintas ("chat" y "chat-rag")
// para que la UI pueda elegir cuál instanciar según la página.
// - ChatbotReal: chat general, usa herramientas (Tools.cs) y sin contexto de documentos.
// - ChatbotRag: chat que responde solo en base al contexto recuperado de Supabase.
builder.Services.AddKeyedScoped<IChatbot, ChatbotReal>("chat");
builder.Services.AddKeyedScoped<IChatbot, ChatbotRag>("chat-rag");

// ===================== MOTOR DE RAG: ELEGIR UNA IMPLEMENTACIÓN =====================
// La app soporta dos "motores" intercambiables para IServicioRag e IVectorStore:
//
//   Opción A (este bloque, comentado): RAG EN MEMORIA
//     - No requiere Supabase ni conexión a internet a la base de datos.
//     - Los documentos se cargan y vectorizan en memoria al iniciar la app.
//     - Útil para demos rápidas, desarrollo offline o pruebas.
//
//   Opción B (más abajo, sección "SUPABASE / PGVECTOR"): RAG CON SUPABASE
//     - Persiste los embeddings en una base Postgres real (pgvector).
//     - Es la opción activa por defecto / la usada en producción.
//
// Para cambiar de A a B (o viceversa): comentar/descomentar este bloque
// Y la registración de IServicioRag/IVectorStore en la sección de Supabase.
// Solo una de las dos debe estar activa a la vez.

// --- Opción A: RAG en memoria (para pruebas sin Supabase) ---
//builder.Services.AddSingleton<ServicioDocumentosEnMemoria>();
//builder.Services.AddSingleton<IServicioRag, ServicioRagMemoria>();
//builder.Services.AddSingleton<InMemoryVectorStore>();

builder.Services.AddTransient<IRepositorioMarkdown, RepositorioMarkdownLocal>();

// ===================== SUPABASE / PGVECTOR (motor de RAG) =====================

// 1. Conexión a Postgres (Supabase). UseVector() es OBLIGATORIO:
//    sin esto, Npgsql no sabe serializar/deserializar el tipo "vector" de pgvector
//    y las consultas fallan en tiempo de ejecución.
builder.Services.AddSingleton<NpgsqlDataSource>(sp =>
{
    var connectionString = builder.Configuration["SupabaseConnection"]!;
    var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
    dataSourceBuilder.UseVector();
    return dataSourceBuilder.Build();
});

// 2. Vector Store de Postgres (PostgresVectorStore). Es la implementación concreta
//    de las abstracciones de Microsoft.Extensions.VectorData para PgVector.
//    AddPostgresVectorStore() toma automáticamente el NpgsqlDataSource registrado arriba.
builder.Services.AddPostgresVectorStore();

// 3. Servicios propios de BlazorIA que consumen el Vector Store:
//    - VectorStoreClienteSupabase: sube/fragmenta/embebe los .md y los guarda en Supabase.
//    - ServicioRagSupabase: dado un texto de pregunta, busca los fragmentos más relevantes.
//    (Esta es la "Opción B" del RAG — ver comentario junto a los chatbots para la Opción A)
builder.Services.AddScoped<BlazorIA.RAG.Servicios.IVectorStore, VectorStoreClienteSupabase>();
builder.Services.AddSingleton<IServicioRag, ServicioRagSupabase>();

// ===================== EMBEDDINGS =====================

// Generador de embeddings (texto -> vector de 1536 dimensiones).
// Usa GitHub Models como proveedor gratuito, compatible con la API de OpenAI.
// Si en el futuro se cambia de proveedor (OpenAI, Ollama, etc.), solo se modifica este bloque;
// el resto de la app sigue funcionando igual gracias a IEmbeddingGenerator.
builder.Services.AddSingleton<IEmbeddingGenerator<string, Embedding<float>>>(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var apiKey = configuration["GITHUB_LLAVE"]!;
    var modeloEmbeddings = configuration["MODELO_GENERA_EMBEDDINGS"];

    var cliente = new EmbeddingClient(
        modeloEmbeddings,
        new System.ClientModel.ApiKeyCredential(apiKey),
        new OpenAI.OpenAIClientOptions
        {
            Endpoint = new Uri("https://models.inference.ai.azure.com")
        }
    );
    return cliente.AsIEmbeddingGenerator();
});

// ===================== HERRAMIENTAS / TOOLS DEL CHAT GENERAL =====================

builder.Services.AddTransient<IServicioClima, ServicioClimaOpenWeather>();
builder.Services.AddTransient<ServicioEvaluaCondiciones>();
builder.Services.AddTransient<ServicioEnviarCorreoFalso>();
builder.Services.AddTransient<ServicioObtenerCorreoFalso>();
builder.Services.AddHttpClient();

// Fábrica que crea el IChatClient correcto según el modelo elegido
// (OpenAI, Claude, Gemini, GitHub Models, Ollama, etc.). Ver ChatClientFactory.cs.
builder.Services.AddTransient<IChatClientFactory, ChatClientFactory>();

// Opciones de chat compartidas: tools disponibles, temperatura y límite de tokens.
builder.Services.AddTransient<ChatOptions>(sp => new ChatOptions
{
    Tools = [.. Tools.ObtenerTools(sp)],
    Temperature = 0.7f,
    MaxOutputTokens = 2000
});

var app = builder.Build();

// ===================== PIPELINE HTTP =====================

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
