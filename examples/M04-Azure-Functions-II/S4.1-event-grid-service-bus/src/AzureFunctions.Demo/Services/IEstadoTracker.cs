namespace AzureFunctions.Demo.Services;

// Contadores in-memory para inspeccionar el efecto de los 4 triggers
// (SB queue, SB topic + 2 subs, Event Grid). En producción usarías
// Application Insights metrics; aquí basta con un singleton sencillo.
public interface IEstadoTracker
{
    void Encolado(string pedidoId);
    void ProcesadoCola(string pedidoId);
    void NotificadoPorTopic(string pedidoId, string subscripcion);
    void ClasificadoArchivo(string url, string clasificacion);
    void Abandonado(string pedidoId, string motivo);

    EstadoSnapshot Snapshot();
}

public sealed record EstadoSnapshot(
    int Encolados,
    int Procesados,
    int Notificaciones,
    int Clasificados,
    int Abandonados,
    IReadOnlyList<EntradaEstado> UltimasEntradas);

public sealed record EntradaEstado(
    string Tipo,
    string Detalle,
    DateTimeOffset En);
