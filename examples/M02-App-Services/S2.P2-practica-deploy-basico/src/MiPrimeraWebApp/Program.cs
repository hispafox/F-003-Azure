using MiPrimeraWebApp.Configuration;
using MiPrimeraWebApp.Models;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddOptions<SaludoOptions>()
    .Bind(builder.Configuration.GetSection(SaludoOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

var app = builder.Build();

// Slide 5 — Endpoint raíz: información de la app
app.MapGet("/", () => Results.Ok(new
{
    aplicacion = "Mi Primera Web App",
    version = "1.0",
    entorno = app.Environment.EnvironmentName,
    hora_servidor = DateTime.UtcNow,
    mensaje = "Hola desde Azure"
}));

// Slide 5 — Health check (también lo usa App Service via healthCheckPath=/health)
app.MapGet("/health", () => Results.Ok(new
{
    status = "healthy",
    timestamp = DateTime.UtcNow
}));

// Slide 5, 13 y 14 — Saludo con validación, logging estructurado y settings
app.MapGet("/saludo/{nombre}", (
    string nombre,
    IOptions<SaludoOptions> options,
    ILogger<Program> logger) =>
{
    var opts = options.Value;

    if (string.IsNullOrWhiteSpace(nombre) || nombre.Length > opts.MaxLength)
    {
        logger.LogWarning("Nombre invalido recibido: longitud {Length}", nombre?.Length ?? 0);
        return Results.BadRequest(new
        {
            error = $"Nombre invalido (max {opts.MaxLength} caracteres)"
        });
    }

    logger.LogInformation("Saludando a {Nombre}", nombre);
    return Results.Ok(new
    {
        mensaje = $"{opts.Base} {nombre}",
        hora = DateTime.UtcNow
    });
});

// Slide 21, reto 1 — POST /usuarios con validación de email
app.MapPost("/usuarios", (Usuario user, ILogger<Program> logger) =>
{
    if (string.IsNullOrEmpty(user.Email) || !user.Email.Contains('@'))
    {
        logger.LogWarning("Email invalido recibido");
        return Results.BadRequest(new { error = "Email invalido" });
    }

    var id = Guid.NewGuid();
    logger.LogInformation("Usuario creado con id {UserId}", id);
    return Results.Created($"/usuarios/{id}", new
    {
        id,
        nombre = user.Nombre,
        email = user.Email
    });
});

app.Run();

public partial class Program;
