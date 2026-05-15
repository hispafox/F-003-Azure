using AzureFunctions.Demo.Models;

namespace AzureFunctions.Demo.Services;

// Lógica pura del flujo async (slide 23):
//   1) Validar DTO de entrada
//   2) Construir Pedido + serializar el mensaje que va a Service Bus
//
// Extraerlo a un servicio permite testar la lógica sin runtime de
// Functions ni mocks de bindings (slide 26 estrategia 1).
public interface IPedidosOrquestador
{
    (IReadOnlyList<string> errores, Pedido? pedido, string? mensajeSerializado)
        ValidarYPreparar(CrearPedidoDto? dto);
}
