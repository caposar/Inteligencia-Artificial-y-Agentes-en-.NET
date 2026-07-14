using Microsoft.AspNetCore.HttpOverrides;
using ServidorMCP.Servicios;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services
    .AddMcpServer()
     .WithHttpTransport(options =>
     {
         options.Stateless = true;
     })
     .WithToolsFromAssembly()
     .WithPromptsFromAssembly()
     .WithResourcesFromAssembly();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services.AddSingleton<IRepositorioPersonas, RepositorioPersonasMemoria>();

// ── AGREGADO: Health check para monitoreo (cron-job.org, Sección 8) ────
builder.Services.AddHealthChecks();

// ── AGREGADO: Forwarded Headers — recomendado detrás de Nginx ──────────
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    // Necesario en Docker: la red vps-network no está en la lista de
    // redes conocidas por defecto (igual que en Lusso Store).
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

var app = builder.Build();

// ── AGREGADO: debe ir ANTES de UseCors/MapControllers ──────────────────
app.UseForwardedHeaders();

app.UseCors();

app.MapMcp("/mcp");

app.MapControllers();

// ── AGREGADO: endpoint que va a monitorear cron-job.org ────────────────
app.MapHealthChecks("/api/health");

app.Run();