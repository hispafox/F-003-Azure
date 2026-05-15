using System.Text.Json;
using AzureFunctions.Demo.Services;
using Azure.Messaging.EventGrid;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace AzureFunctions.Demo.Functions;

// Slide 19 — Patrón Event Grid → fan-out a Service Bus:
//   Blob creado → BlobCreated event → ClasificarArchivoFunction →
//     ├─ si es .pdf  → queue "facturas"
//     ├─ si es .csv  → queue "imports"
//     └─ otros       → ignorado (no encolamos basura)
//
// Multi-output (slide 6): cada propiedad con atributo se materializa al
// destino. Si una es null, el binding no se ejecuta — así no encolamos
// mensajes vacíos cuando el archivo no nos interesa.
public sealed class ClasificarArchivoFunction
{
    private readonly IEstadoTracker _tracker;
    private readonly ILogger<ClasificarArchivoFunction> _logger;

    public ClasificarArchivoFunction(
        IEstadoTracker tracker,
        ILogger<ClasificarArchivoFunction> logger)
    {
        _tracker = tracker;
        _logger = logger;
    }

    [Function(nameof(ClasificarArchivo))]
    public ClasificarArchivoResult ClasificarArchivo(
        [EventGridTrigger] EventGridEvent evento)
    {
        return Clasificar(evento);
    }

    // Handler puro para tests.
    internal ClasificarArchivoResult Clasificar(EventGridEvent evento)
    {
        if (evento.EventType != "Microsoft.Storage.BlobCreated")
        {
            _logger.LogDebug("Evento ignorado: {Tipo}", evento.EventType);
            return new ClasificarArchivoResult();
        }

        // El payload de BlobCreated trae la URL del blob recién subido.
        var data = evento.Data.ToObjectFromJson<JsonElement>();
        var url = data.TryGetProperty("url", out var u) ? u.GetString() : null;
        if (string.IsNullOrEmpty(url))
        {
            _logger.LogWarning("BlobCreated sin URL: {Evento}", evento.Subject);
            return new ClasificarArchivoResult();
        }

        var clasificacion = ClasificarUrl(url);
        if (clasificacion is null)
        {
            _logger.LogInformation("Archivo no relevante, no se encola: {Url}", url);
            return new ClasificarArchivoResult();
        }

        _tracker.ClasificadoArchivo(url, clasificacion);
        _logger.LogInformation("Clasificado {Url} → {Clasificacion}", url, clasificacion);

        var mensaje = JsonSerializer.Serialize(new
        {
            url,
            tipo = clasificacion,
            detectadoEn = DateTimeOffset.UtcNow,
            subject = evento.Subject,
        });

        return clasificacion switch
        {
            "factura" => new ClasificarArchivoResult { MensajeFacturas = mensaje },
            "import" => new ClasificarArchivoResult { MensajeImports = mensaje },
            _ => new ClasificarArchivoResult(),
        };
    }

    internal static string? ClasificarUrl(string url)
    {
        if (url.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase)) return "factura";
        if (url.EndsWith(".csv", StringComparison.OrdinalIgnoreCase)) return "import";
        return null;
    }
}

public sealed class ClasificarArchivoResult
{
    [ServiceBusOutput("facturas-procesar", Connection = "ServiceBusConnection")]
    public string? MensajeFacturas { get; set; }

    [ServiceBusOutput("imports-procesar", Connection = "ServiceBusConnection")]
    public string? MensajeImports { get; set; }
}
