namespace AzureFunctions.Demo.Services;

// Contadores in-memory para inspeccionar el flujo de resiliencia desde
// /api/estado: cuántos se procesaron OK, cuántos saltaron por idempotencia,
// cuántos fueron a DLQ y qué hizo el poison-processor con ellos.
public interface IEstadoTracker
{
    void Procesado(string pedidoId);
    void DuplicadoSaltado(string pedidoId);
    void EnviadoADeadLetter(string pedidoId, string motivo);
    void PoisonProcesado(string pedidoId, string accion);

    EstadoSnapshot Snapshot();
}

public sealed record EstadoSnapshot(
    int Procesados,
    int DuplicadosSaltados,
    int EnviadosADeadLetter,
    int PoisonProcesados,
    IReadOnlyList<EntradaEstado> UltimasEntradas);

public sealed record EntradaEstado(
    string Tipo,
    string Detalle,
    DateTimeOffset En);
