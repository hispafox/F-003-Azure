using AppService.Practica.Api.Configuration;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddOptions<PracticaOptions>()
    .Bind(builder.Configuration.GetSection(PracticaOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

var app = builder.Build();

// Slide 7 — Endpoint principal: muestra version/novedad ("código") + entorno
// y nota_entorno ("settings sticky") + slot e instancia.
app.MapGet("/", (IOptions<PracticaOptions> options) =>
{
    var opts = options.Value;
    return Results.Json(new
    {
        app = "Curso AZ-204 — Práctica S2.P",
        version = opts.Version,
        novedad = opts.Novedad,
        entorno = app.Environment.EnvironmentName,
        nota_entorno = opts.NotaEntorno,
        slot = Environment.GetEnvironmentVariable("WEBSITE_SLOT_NAME") ?? "local",
        servidor = Environment.MachineName,
        hora_utc = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
    });
});

// Health check — App Service consulta este endpoint con healthCheckPath=/health
app.MapGet("/health", (IOptions<PracticaOptions> options) =>
    Results.Ok(new { status = "healthy", version = options.Value.Version }));

// Slide 9 — Endpoint para WEBSITE_SWAP_WARMUP_PING_PATH=/warmup. App Service
// lo llama antes de redirigir tráfico durante el swap. En una app real aquí
// precalentarías DB, cache, etc.
app.MapGet("/warmup", async () =>
{
    await Task.Delay(50);
    return Results.Ok(new { status = "warm", timestamp = DateTime.UtcNow });
});

app.Run();

public partial class Program;
