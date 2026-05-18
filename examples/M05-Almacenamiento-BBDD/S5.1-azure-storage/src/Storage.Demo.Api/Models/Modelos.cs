using Azure;
using Azure.Data.Tables;

namespace Storage.Demo.Api.Models;

// Resultado de listar un blob (slide 8).
public sealed record BlobItemDto(
    string Nombre,
    long Tamano,
    DateTimeOffset? UltimaModificacion,
    IReadOnlyDictionary<string, string> Metadata);

// Slide 12 — entidad de Table Storage. PartitionKey + RowKey = clave
// compuesta. Implementa ITableEntity (lo exige el SDK).
public sealed class AuditEntity : ITableEntity
{
    public string PartitionKey { get; set; } = "";   // p.ej. fecha "2026-05-15"
    public string RowKey { get; set; } = "";          // id único del evento
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    public string Usuario { get; set; } = "";
    public string Accion { get; set; } = "";
    public string Detalle { get; set; } = "";
}

public sealed record CrearAuditDto(string Particion, string Usuario, string Accion, string Detalle);

public sealed record MensajeColaDto(string Cuerpo);
