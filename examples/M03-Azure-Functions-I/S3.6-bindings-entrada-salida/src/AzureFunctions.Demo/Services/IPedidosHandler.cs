using AzureFunctions.Demo.Models;

namespace AzureFunctions.Demo.Services;

// Slide 26 — Estrategia 1: separar la lógica del trigger.
// El handler se testea sin runtime de Functions ni mocks de bindings.
public interface IPedidosHandler
{
    // Devuelve (errores, pedido). Si hay errores, pedido es null y los
    // bindings de output NO se ejecutan (slide 24 — validar antes de
    // escribir en Cosmos / encolar).
    (IReadOnlyList<ValidationError> errores, Pedido? pedido) ValidarYConstruir(CrearPedidoDto? dto);
}

public sealed record ValidationError(string Campo, string Mensaje);
