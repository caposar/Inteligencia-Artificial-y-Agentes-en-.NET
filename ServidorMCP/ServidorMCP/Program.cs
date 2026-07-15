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

// Expone /api/health para que herramientas de monitoreo (cron-job.org,
// Docker, Kubernetes, etc.) puedan verificar automáticamente si la
// aplicación sigue viva.
builder.Services.AddHealthChecks();

// La aplicación corre detrás de un reverse proxy (Nginx). Sin este ajuste,
// ASP.NET Core ve todas las conexiones como si vinieran del proxy mismo
// (127.0.0.1) y no del cliente real, y no sabe distinguir si la petición
// original llegó por HTTP o HTTPS. Esto afecta la IP que se registra en
// logs, la lógica de autenticación y cualquier redirección basada en el
// esquema de la conexión.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

    // Docker asigna una IP dinámica a Nginx dentro de la red vps-network,
    // así que no puede fijarse de antemano en KnownProxies/KnownNetworks.
    // Al limpiar ambas listas, se confía en cualquier IP dentro de la red
    // interna del contenedor — algo seguro acá porque esa red no es
    // accesible desde internet, solo desde el propio servidor.
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

var app = builder.Build();

// Debe ir antes de UseCors/MapControllers: necesita corregir la información
// de la petición (IP real, esquema HTTP/HTTPS) antes de que el resto del
// pipeline la use.
app.UseForwardedHeaders();

app.UseCors();

app.MapMcp("/mcp");

app.MapControllers();

app.MapHealthChecks("/api/health");

app.Run();