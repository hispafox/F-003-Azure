using AzureFunctions.Demo.Models;

namespace AzureFunctions.Demo.Services;

// Slide 10 — contrato del sink de notificaciones. El método clave es
// EnviarSiNoEnviada: encapsula el patrón "verificar y enviar" de forma
// atómica (ConcurrentDictionary.GetOrAdd) para que la función no tenga
// que orquestar ese check ella misma.
public interface INotificacionService
{
    // Devuelve true si la notificación se envió ahora, false si ya existía
    // por la clave (PedidoId, Estado).
    bool EnviarSiNoEnviada(string pedidoId, string clienteId, string estado, string mensaje);

    IReadOnlyCollection<Notificacion> ListarTodas();

    IReadOnlyCollection<Notificacion> ListarPorCliente(string clienteId);

    Notificacion? Buscar(string pedidoId, string estado);

    int Total { get; }
}
