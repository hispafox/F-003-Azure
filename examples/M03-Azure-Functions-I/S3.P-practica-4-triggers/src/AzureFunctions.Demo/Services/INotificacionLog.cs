using AzureFunctions.Demo.Models;

namespace AzureFunctions.Demo.Services;

// Log in-memory de los cambios que el Cosmos Change Feed trigger ha
// procesado. Sirve para inspección desde un endpoint HTTP (verificar que
// el trigger reaccionó a los inserts).
public interface INotificacionLog
{
    void Anotar(Pedido pedido);
    IReadOnlyList<EntradaLog> Listar();
    int Total { get; }
}

public sealed record EntradaLog(
    string PedidoId,
    string ClienteId,
    string Estado,
    decimal Total,
    DateTimeOffset RegistradoEn);
