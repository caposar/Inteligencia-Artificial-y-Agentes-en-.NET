using Anthropic;
using BlazorIA.Components;
using BlazorIA.Datos;
using BlazorIA.RAG.Chatbots;
using BlazorIA.RAG.Servicios;
using BlazorIA.Servicios;
using BlazorIA.Servicios.Chatbots;
using BlazorIA.Utilidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel.Connectors.InMemory;
using OpenAI.Embeddings;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddDbContextFactory<ApplicationDbContext>(opciones =>
    opciones.UseSqlite("Data Source=midb.db"));

builder.Services.AddScoped<IServicioPersonas, ServicioPersonas>();

builder.Services.AddKeyedScoped<IChatbot, ChatbotReal>("chat");
builder.Services.AddKeyedScoped<IChatbot, ChatbotRag>("chat-rag");

builder.Services.AddSingleton<ServicioDocumentosEnMemoria>();
builder.Services.AddSingleton<IServicioRag, ServicioRAGAzureSearch>();
//builder.Services.AddSingleton<IServicioRag, ServicioRagMemoria>();
builder.Services.AddSingleton<InMemoryVectorStore>();

builder.Services.AddSingleton<ServicioIndiceRagAzureSearch>();
builder.Services.AddScoped<IVectorStore, VectorStoreClienteAzureSearch>();

builder.Services.AddTransient<IRepositorioMarkdown, RepositorioMarkdownLocal>();

builder.Services.AddSingleton<IEmbeddingGenerator<string, Embedding<float>>>(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var apiKey = configuration["OPENAI_LLAVE"]!;
    var modeloEmbeddings = configuration["MODELO_GENERA_EMBEDDINGS"];

    var cliente = new EmbeddingClient(modeloEmbeddings, apiKey);
    return cliente.AsIEmbeddingGenerator();
});


builder.Services.AddTransient<IServicioClima, ServicioClimaOpenWeather>();
builder.Services.AddTransient<ServicioEvaluaCondiciones>();
builder.Services.AddTransient<ServicioEnviarCorreoFalso>();
builder.Services.AddTransient<ServicioObtenerCorreoFalso>();
builder.Services.AddHttpClient();

builder.Services.AddTransient<IChatClientFactory, ChatClientFactory>();

builder.Services.AddTransient<ChatOptions>(sp => new ChatOptions
{
    Tools = [.. Tools.ObtenerTools(sp)],
    Temperature = 0.7f,
    MaxOutputTokens = 2000
});

var app = builder.Build();

// Configure the HTTP request pipeline.
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
