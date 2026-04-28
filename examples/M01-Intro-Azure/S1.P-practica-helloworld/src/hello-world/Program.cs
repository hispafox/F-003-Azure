using System.Reflection;
using System.Runtime.InteropServices;

var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

// Slide 27 — Endpoint raíz: la "tarjeta de presentación" de la app.
// Los campos `entorno`, `servidor`, `hora_utc`, `runtime` y `os` cambian al
// desplegar a Azure y permiten verificar que el deploy funcionó.
// `asistente` se lee de configuración para que cada alumno ponga su nombre.
app.MapGet("/", (IConfiguration config) => Results.Json(new
{
    mensaje = "Hello Azure — Curso AZ-204",
    asistente = config["Asistente"] ?? "<TU-NOMBRE-AQUÍ>",
    entorno = app.Environment.EnvironmentName,
    servidor = Environment.MachineName,
    hora_utc = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
    runtime = RuntimeInformation.FrameworkDescription,
    os = RuntimeInformation.OSDescription
}));

// Slide 50 — Health check. App Service consulta este endpoint si configuras
// `healthCheckPath=/health` y los load balancers externos lo usan para
// availability probes.
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

// ─────────────────────────────────────────────────────────────────────────
// Retos opcionales (slides 69-72). Implementados desde el principio para
// que el alumno los pueda probar sin escribir código adicional.
// ─────────────────────────────────────────────────────────────────────────

// Reto 1 (slide 69) — App Settings vía variables de entorno.
// CURSO_MODULO, CURSO_SESION, CURSO_FECHA se configuran en App Service
// y se leen aquí sin redesplegar.
app.MapGet("/api/info", (IConfiguration config) => Results.Ok(new
{
    modulo = config["CURSO_MODULO"] ?? "no definido",
    sesion = config["CURSO_SESION"] ?? "no definida",
    fecha = config["CURSO_FECHA"] ?? "sin fecha",
    asistente = config["Asistente"] ?? "<TU-NOMBRE-AQUÍ>"
}));

// Reto 2 (slide 70) — Echo con query parameter y validación.
app.MapGet("/api/echo", (string? msg) =>
{
    if (string.IsNullOrWhiteSpace(msg))
    {
        return Results.BadRequest(new
        {
            error = "El parámetro 'msg' es obligatorio",
            ejemplo = "/api/echo?msg=hola"
        });
    }

    return Results.Ok(new
    {
        recibido = msg,
        longitud = msg.Length,
        hora = DateTime.UtcNow.ToString("HH:mm:ss"),
        eco = $"Has dicho: {msg}"
    });
});

// Reto 3 (slide 71) — Información de versión de la app desplegada.
// Útil en pipelines: verificas qué build está corriendo.
app.MapGet("/api/version", () =>
{
    var assembly = Assembly.GetExecutingAssembly();
    var name = assembly.GetName();
    var location = assembly.Location;
    var buildTime = string.IsNullOrEmpty(location) || !File.Exists(location)
        ? DateTime.UtcNow
        : File.GetLastWriteTimeUtc(location);

    return Results.Ok(new
    {
        version = name.Version?.ToString() ?? "unknown",
        assembly = name.Name,
        framework = RuntimeInformation.FrameworkDescription,
        buildTime = buildTime.ToString("yyyy-MM-dd HH:mm:ss")
    });
});

app.Run();

public partial class Program;
